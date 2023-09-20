// Copyright (c) 2023 Mick Krippendorf <m.krippendorf@freenet.de>
//
// A Monadic Combinator framework for Logic Programming, allowing users to
// express and solve logical problems in programmatic form.
//
//
// Overview:
//
//
// The Triple-Barrelled Continuation Monad:
//
//     “The continuation that obeys only obvious stack semantics,
//     O grasshopper, is not the true continuation.” — Guy L. Steele.
//
// We use this Monad to drive the resolution process. It is responsible for
// handling resolution, backtracking, and branch pruning.
//
//
// Simplified Terminology:
//
// To make logic programming more accessible and user-friendly, we've
// introduced simplified terminology. Instead of using abstract jargon like
// "Monad" and "Continuation," we've defined two main types:
//
// 1. 'Step': Represents a monadic computation step. It can succeed, invoke
// backtracking, or esacpe, that is, jump back to the previous choice point,
// thus pruning the search space.
//
// 2. 'Goal': Represents a logical statement or query that we want to resolve.
// Goals take a substitution environment and produce 'Steps.'
//
//
// Basic Combinators:
//
// We provide a set of essential combinators that you'd expect in a logic
// programming library:
//
// - 'Unit': Succeeds once and represents success.
//
// - 'Fail': Never succeeds and represents failure.
//
// - 'Cut': Succeeds once and then curtails backtracking at the previous
// choice point.
//
// - 'And': Represents conjunction of goals, meaning all goals must succeed
// for it to succeed.
//
// - 'Or': Represents adjunction of goals, meaning it succeeds if any one of
// the goals succeeds.
//
// - 'Not': Represents negation as failure, i.e., it succeeds only when the
// given goal fails and vice versa.
//
// - 'Unify*': A set of unification combinators for matching objects with each
// otehr and binding variables to objects and other variables.
//
//
// Resolution Process:
//
// A resolution process is started by calling 'Resolve' on a goal. It returns
// an enumerable collection of substitution environments (proxy mappings) of
// variables to their bindings. These are the solutions to a logical query.
//
//
// Algebraic Structure:
//
// Under the hood, we make use of the algebraic structure of monadic
// combinators. Specifically:
//
// - 'Unit' and 'Then' form a Monoid over monadic combinator functions.
//
// - 'Fail' and 'Choice' also form a Monoid.
//
// These structures allow us to fold a sequence of combinators into a single
// one. Additionally, they form a Distributive Lattice where 'Then' is the
// meet (infimum) and 'Choice' the join (supremum) operator, and 'Unit' and
// 'Fail' their respective identity elements. Although not explicitly used in
// the code, these properties reflect the inherent structure of the
// combinators. Users of this library, on the other hand, might make use
// these distributive properties of the Lattice.
//
// It's important to note that due to the sequential nature of the employed
// resolution algorithm combined with the 'Cut' combinator, neither the
// lattice nor the monoids are commutative.
//
//
// Tail Call Elimination:
//
// C# lacks proper Tail Call Elimination, which can lead to stack overflows in
// recursive logic programming. To mitigate this somewhat, we use a technique
// known as Trampolining with Thunking. It allows us to prevent stack overflow
// issues by, instead of returning just a solution to a query, also returning
// the thunk (a parameterless function) to be executed next.


using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;

namespace Yogic;

// Miscellaneous type aliases.
using Seq = IReadOnlyCollection<object>;
using Pair = ValueTuple<object, object>;

// The type of the Substitution Environment.
// ImmutableDictionary brings everything we need for Trailing.
using Subst = ImmutableDictionary<Variable, object>;

// A Tuple of this type is returned for each successful resolution step.
// This enables Tail Call ELimination through Thunking and Trampolining.
using Result = ValueTuple<ImmutableDictionary<Variable, object>, Next>;

// A function type that represents a backtracking operation.
public delegate Result? Next();

// A function type that represents a successful resolution.
public delegate Result? Emit(Subst subst, Next next);

// The monadic computation type.
// 'succeed' wraps the current continuation and 'backtrack' wraps the
// continuation for normal backtracking. 'escape' wraps the continuation
// that a subsequent 'cut' invokes to curtail backtracking at the previous
// choice point.
public delegate Result? Step(Emit succeed, Next backtrack, Next escape);

// The monadic continuation type.
public delegate Step Goal(Subst subst);

public class Variable
{
    private readonly string name;

    public Variable(string name)
    {
        this.name = name;
    }

    public override string ToString() => $"Variable({name})";
}

public class SubstProxy
{
    private readonly Subst subst;

    internal SubstProxy(Subst subst)
    {
        this.subst = subst;
    }

    // deref'ing here is the whole reason we need this class:
    public object this[Variable v] => Combinators.deref(subst, v);
}

public static class Combinators
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object deref(Subst subst, object obj)
    {
        // Chase down Variable bindings:
        while (obj is Variable variable && subst.ContainsKey(variable))
        {
            obj = subst[variable];
        }
        return obj;
    }

    private static Result? quit() => null;

    private static Result? emit(Subst subst, Next next) => (subst, next);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Result tailcall(Next next) => (null, next);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Step Bind(Step step, Goal goal)
    {
        // Make 'goal' the continuation of 'step':
        return (succeed, backtrack, escape) =>
            tailcall(
                () => step((subst, next) => goal(subst)(succeed, next, escape), backtrack, escape)
            );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Step Unit(Subst subst)
    {
        return (succeed, backtrack, escape) => tailcall(() => succeed(subst, backtrack));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Step Cut(Subst subst)
    {
        return (succeed, backtrack, escape) => tailcall(() => succeed(subst, escape));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Step Fail(Subst subst)
    {
        return (succeed, backtrack, escape) => tailcall(backtrack);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal Then(Goal goal1, Goal goal2)
    {
        // Sequencing is the default behavior of 'bind':
        return subst => Bind(goal1(subst), goal2);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal And(IEnumerable<Goal> goals)
    {
        // 'unit' and 'then' form a monoid, so we can just fold:
        return goals.Aggregate<Goal, Goal>(Unit, Then);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal And(params Goal[] goals)
    {
        return And(goals);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal Choice(Goal goal1, Goal goal2)
    {
        // We make 'goal2' the new backtracking path of 'goal1':
        return subst =>
            (succeed, backtrack, escape) =>
                tailcall(
                    () =>
                        goal1(subst)(
                            succeed,
                            () => goal2(subst)(succeed, backtrack, escape),
                            escape
                        )
                );
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal Or(IEnumerable<Goal> goals)
    {
        // 'fail' and 'choice' form a monoid, so we can just fold:
        var choices = goals.Aggregate<Goal, Goal>(Fail, Choice);
        // we inject 'backtrack' as the new escape path, so we can
        // curtail backtracking here and immediately continue at the
        // previous choice point instead:
        return subst =>
            (succeed, backtrack, escape) =>
                tailcall(() => choices(subst)(succeed, backtrack, backtrack));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal Or(params Goal[] goals)
    {
        return Or(goals);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal Not(Goal goal)
    {
        // Negation as failure:
        return Or(And(goal, Cut, Fail), Unit);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal Unify(object o1, object o2)
    {
        // Using an 'ImmutableDictionary' makes trailing easy:
        return subst =>
            (deref(subst, o1), deref(subst, o2)) switch
            {
                (var x1, var x2) when x1.Equals(x2) => Unit(subst),
                (Seq s1, Seq s2) when s1.Count == s2.Count => UnifyAll(s1.Zip(s2))(subst),
                (Variable v, var o) => Unit(subst.Add(v, o)),
                (var o, Variable v) => Unit(subst.Add(v, o)),
                _ => Fail(subst)
            };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal UnifyAny(Variable v, IEnumerable<object> objects)
    {
        return Or(from o in objects select Unify(v, o));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal UnifyAny(Variable variable, params object[] objects)
    {
        return UnifyAny(variable, objects);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal UnifyAll(IEnumerable<Pair> pairs)
    {
        return And(from pair in pairs select Unify(pair.Item1, pair.Item2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal UnifyAll(params Pair[] pairs)
    {
        return UnifyAll(pairs);
    }

    public static IEnumerable<SubstProxy> Resolve(Goal goal)
    {
        Result? result = goal(Subst.Empty)(emit, quit, quit);
        // We have to implement Tail Call Elimination ourself:
        while (result is (var subst, var next))
        {
            if (subst is not null)
            {
                yield return new SubstProxy(subst);
            }
            result = next();
        }
    }
}
