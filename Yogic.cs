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
//   Goals take a substitution environment and produce a 'Step'.
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
// an enumerable sequence of substitution environments (proxy mappings) of
// variables to their bindings, each representing a distinct solution.
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
// C# lacks proper Tail Call Elimination, which can lead to stack overflows in
// heavily recursive code like this. To mitigate this, we use a technique known
// as Trampolining with Thunking. Instead of returning only the solution, we
// also return a parameterless function (called a thunk) to be executed next.
// The 'Resolve' function acts as a driver that calls the thunks in a loop until
// no more solutions can be found.

using System;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Yogic;

// The type of the Substitution Environment.
// ImmutableDictionary brings everything we need for Trailing.
using SubstImpl = ImmutableDictionary<Variable, object>;

// A Tuple of this type is returned for each successful resolution step.
// This enables Tail Call Elimination through Thunking and Trampolining.
using Result = (ImmutableDictionary<Variable, object> subst, Next next);

// A function type that represents a backtracking operation.
public delegate Result? Next();

// A function type that represents a successful resolution.
public delegate Result? Emit(SubstImpl subst, Next next);

// The monadic computation type.
// 'yes' wraps the current continuation and 'no' wraps the continuation for
// normal backtracking. 'cut' wraps the continuation that a subsequent 'Cut'
// invokes to curtail backtracking at the previous choice point.
public delegate Result? Step(Emit yes, Next no, Next cut);

// The monadic continuation type.
public delegate Step Goal(SubstImpl subst);

// This must be a class and not a record because we want equality tests to be
// testing for object identity.
public class Variable(string name)
{
    public string Name => name;
    public override string ToString() => $"""Variable(Name="{name}")""";
}

// A proxy struct for SubstImpl we return to the client:
public readonly record struct Subst(SubstImpl subst)
{
    // deref'ing here is the whole reason we need this struct:
    public object this[Variable v] => subst.deref(v);
}

public static class Combinators
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static object deref(this SubstImpl subst, object obj)
    {
        // Chase down Variable bindings:
        while (obj is Variable variable && subst.ContainsKey(variable))
        {
            obj = subst[variable];
        }
        return obj;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Result tailcall(Next next)
        => (null, next);

    private static Result? emit(SubstImpl subst, Next next)
        => (subst, next);

    private static Result? quit()
        => null;

    public static IEnumerable<Subst> Resolve(Goal goal)
    {
        Result? result = goal(SubstImpl.Empty)(yes: emit, no: quit, cut: quit);
        while (result is (var subst, var next))
        {
            // We have to implement Tail Call Elimination ourselves:
            if (subst is not null)
            {
                yield return new Subst(subst);
            }
            result = next();
        }
    }

    public static Step Bind(this Step step, Goal goal)
        // Make 'goal' the continuation of 'step':
        => (yes, no, cut)
        => tailcall(
            () => step(
                yes: (subst, next) => goal(subst)(yes, no: next, cut),
                no,
                cut
            )
        );

    public static Step Unit(SubstImpl subst)
        // Normal execution path with `no` as backracking path:
        => (yes, no, cut)
        => tailcall(() => yes(subst, next: no));

    public static Step Cut(SubstImpl subst)
        // Like `Unit`, but we inject `cut` as backtracking path:
        => (yes, no, cut)
        => tailcall(() => yes(subst, next: cut));

    public static Step Fail(SubstImpl subst)
        // Ignore yes and immediately backtrack:
        => (yes, no, cut)
        => tailcall(no);

    public static Goal Then(Goal goal1, Goal goal2)
        // Sequencing is the default behavior of 'Bind':
        => subst
        => goal1(subst).Bind(goal2);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal And(IEnumerable<Goal> goals)
        // 'Unit' and 'Then' form a monoid, so we can just fold:
        => goals.Aggregate<Goal, Goal>(Unit, Then);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal And(params Goal[] goals)
        => And(goals);

    public static Goal Choice(Goal goal1, Goal goal2)
        // Make 'goal2' the new backtracking path of 'goal1':
        => subst
        => (yes, no, cut)
        => tailcall(
            () => goal1(subst)(
                yes,
                no: () => goal2(subst)(yes, no, cut),
                cut
            )
        );

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Goal or_impl(Goal goals)
        // Inject 'no' as the new cut path, so we can curtail backtracking
        // here and immediately continue at the previous choice point instead:
        => subst
        => (yes, no, cut)
        => tailcall(() => goals(subst)(yes, no, cut: no));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal Or(IEnumerable<Goal> goals)
        // 'Fail' and 'Choice' form a monoid, so we can just fold:
        => or_impl(goals.Aggregate<Goal, Goal>(Fail, Choice));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal Or(params Goal[] goals)
        => Or(goals);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal Not(Goal goal)
        // Negation as failure:
        => Or(And(goal, Cut, Fail), Unit);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool compatible(object seq1, object seq2)
    {
        if (seq1 is System.Collections.IList list1)
        {
            if (seq2 is System.Collections.IList list2)
            {
                return list1.Count == list2.Count
                    && list1.GetType() == list2.GetType();
            }
        }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static Goal unify_rec(object seq1, object seq2)
    {
        var list1 = ((System.Collections.IList)seq1).Cast<object>();
        var list2 = ((System.Collections.IList)seq2).Cast<object>();
        return UnifyAll(list1.Zip(list2));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal Unify(object left, object right)
        => subst
        => (subst.deref(left), subst.deref(right)) switch
        {
            // Equal things are already unified:
            (var a, var b) when a.Equals(b) => Unit(subst),
            // Recurse:
            (var a, var b) when compatible(a, b) => unify_rec(a, b)(subst),
            // Using an 'ImmutableDictionary' makes trailing easy:
            (Variable v, var o) => Unit(subst.Add(v, o)),
            (var o, Variable v) => Unit(subst.Add(v, o)),
            // All else:
            _ => Fail(subst)
        };

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal UnifyAny(Variable v, IEnumerable<object> objects)
        => Or(from o in objects select Unify(v, o));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal UnifyAny(Variable variable, params object[] objects)
        => UnifyAny(variable, objects);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal UnifyAll<T1, T2>(IEnumerable<(T1, T2)> pairs)
        => And(from pair in pairs select Unify(pair.Item1, pair.Item2));

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Goal UnifyAll<T1, T2>(params (T1, T2)[] pairs)
        => UnifyAll(pairs);

}
