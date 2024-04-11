// Copyright (c) 2023 Mick Krippendorf <m.krippendorf@freenet.de>
//
// A Monadic Combinator framework for Logic Programming, allowing users to
// express and solve logical problems in programmatic form.
//
//
// Overview:
//
//     “The continuation that obeys only obvious stack semantics,
//     O grasshopper, is not the true continuation.” — Guy L. Steele.
//
// We use the Triple-Barrelled Continuation Monad to drive the resolution
// process. It allows for backtracking and branch pruning.
//
//
// To make logic programming more accessible and user-friendly, we've
// introduced simplified terminology. Instead of using abstract jargon like
// "Monad" and "Continuation," we've defined two main types:
//
// - 'Step': Represents a monadic computation step. It either succeeds, or
//   invokes backtracking, or esacpes back to the previous choice point, thus
//   pruning the search space.
//
// - 'Goal': Represents a logical statement or query that we want to resolve.
//   Goals take a substitution environment and produce 'Steps.'
//
//
// We provide a set of fundamental combinators that you'd expect in a logic
// programming library:
//
// - 'Unit': Succeeds once and represents success.
//
// - 'Fail': Never succeeds and represents failure.
//
// - 'Cut': Succeeds once and then curtails backtracking at the previous
//   choice point.
//
// - 'And': Represents conjunction of goals, meaning all goals must succeed
//   for it to succeed.
//
// - 'Or': Represents adjunction of goals, meaning it succeeds if at least one
//   of the goals succeeds.
//
// - 'Not': Represents negation. Succeeds if the given goal fails and vice
//   versa.
//
// - 'Unify*': A set of unification combinators for matching objects with each
//   other and binding variables to objects and other variables.
//
//
// A resolution process is started by calling 'Resolve' on a goal. It returns
// an enumerable collection of substitution environments (proxy mappings) of
// variables to their bindings, each representing a solution.
//
//
// Under the hood, we make use of the algebraic structure of the monadic
// combinators. Specifically:
//
// - 'Unit' and 'Then' form a Monoid over monadic combinator functions.
//
// - 'Fail' and 'Choice' also form a Monoid.
//
// These structures allow us to fold a sequence of combinators into a single
// one in the 'And' and 'Or' combinators.
//
// Additionally, they form a Distributive Lattice where 'Then' is the meet
// (infimum) and 'Choice' the join (supremum) operator, and 'Unit' and 'Fail'
// their respective identity elements. Although not used in our code, these
// properties reflect the inherent structure of the combinators. Users of this
// library might make use the distributive properties of the Lattice.
//
// It's important to note that due to the sequential nature of the employed
// resolution algorithm combined with the 'Cut' combinator, neither the
// lattice nor the monoids are commutative.
//
//
// C# lacks proper Tail Call Elimination, which can lead to stack overflows.
// To mitigate this, we use a technique known as Trampolining with Thunking.
// Instead of returning only the solution, we also return a parameterless
// function (called a thunk) to be executed next. The 'Resolve' function acts
// as a driver that calls the thunks in a loop until no more solutions can be
// found.


using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

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
// that a subsequent 'Cut' invokes to curtail backtracking at the previous
// choice point.
public delegate Result? Step(Emit succeed, Next backtrack, Next escape);

// The monadic continuation type.
public delegate Step Goal(Subst subst);

public record Variable(string name);

public class SubstProxy(Subst subst)
{
    // deref'ing here is the whole reason we need this class:
    public object this[Variable v] => subst.deref(v);
}

public static class Combinators
{
    internal static object deref(this Subst subst, object obj)
    {
        // Chase down Variable bindings:
        while (obj is Variable variable && subst.ContainsKey(variable))
        {
            obj = subst[variable];
        }
        return obj;
    }

    public static Step Bind(Step step, Goal goal)
    {
        // Make 'goal' the continuation of 'step':
        return (succeed, backtrack, escape) =>
            tailcall(
                () => step((subst, next) => goal(subst)(succeed, next, escape), backtrack, escape)
            );
    }

    public static Step Unit(Subst subst)
    {
        return (succeed, backtrack, escape) => tailcall(() => succeed(subst, backtrack));
    }

    public static Step Cut(Subst subst)
    {
        return (succeed, backtrack, escape) => tailcall(() => succeed(subst, escape));
    }

    public static Step Fail(Subst subst)
    {
        return (succeed, backtrack, escape) => tailcall(backtrack);
    }

    public static Goal Then(Goal goal1, Goal goal2)
    {
        // Sequencing is the default behavior of 'Bind':
        return subst => Bind(goal1(subst), goal2);
    }

    public static Goal And(IEnumerable<Goal> goals)
    {
        // 'Unit' and 'Then' form a monoid, so we can just fold:
        return goals.Aggregate<Goal, Goal>(Unit, Then);
    }

    public static Goal And(params Goal[] goals)
    {
        return And(goals);
    }

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

    public static Goal Or(IEnumerable<Goal> goals)
    {
        // 'Fail' and 'Choice' form a monoid, so we can just fold:
        var choices = goals.Aggregate<Goal, Goal>(Fail, Choice);
        // We inject 'backtrack' as the new escape path, so we can
        // curtail backtracking here and immediately continue at the
        // previous choice point instead:
        return subst =>
            (succeed, backtrack, escape) =>
                tailcall(() => choices(subst)(succeed, backtrack, backtrack));
    }

    public static Goal Or(params Goal[] goals)
    {
        return Or(goals);
    }

    public static Goal Not(Goal goal)
    {
        // Negation as failure:
        return Or(And(goal, Cut, Fail), Unit);
    }

    public static Goal Unify(object o1, object o2)
    {
        // Using an 'ImmutableDictionary' makes trailing easy:
        return subst =>
            (subst.deref(o1), subst.deref(o2)) switch
            {
                (var x1, var x2) when x1 == x2 => Unit(subst),
                (Seq s1, Seq s2) when s1.Count == s2.Count => UnifyAll(s1.Zip(s2))(subst),
                (Variable v, var o) => Unit(subst.Add(v, o)),
                (var o, Variable v) => Unit(subst.Add(v, o)),
                _ => Fail(subst)
            };
    }

    public static Goal UnifyAny(Variable v, IEnumerable<object> objects)
    {
        return Or(from o in objects select Unify(v, o));
    }

    public static Goal UnifyAny(Variable variable, params object[] objects)
    {
        return UnifyAny(variable, objects);
    }

    public static Goal UnifyAll(IEnumerable<Pair> pairs)
    {
        return And(from pair in pairs select Unify(pair.Item1, pair.Item2));
    }

    public static Goal UnifyAll(params Pair[] pairs)
    {
        return UnifyAll(pairs);
    }

    private static Result? quit() => null;

    private static Result? emit(Subst subst, Next next) => (subst, next);

    private static Result tailcall(Next next) => (null, next);

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
