// Copyright (c) 2023 Mick Krippendorf <m.krippendorf@freenet.de>
//
// A library of Monadic Combinators for Logic Programming.
//
//
// It uses the Triple-Barreled Continuation Monad for resolution,
// backtracking, and branch pruning.
//
//
// “The continuation that obeys only obvious stack semantics,
// O grasshopper, is not the true continuation.” — Guy Steele.
//
//
// To keep more closely to the terminology of logic programming and to not
// bother users too much with jargon like "Monad" and "Continuation", the
// monadic computation type is called 'Step' and the monadic continuation type
// is called 'Goal'.
//
// A set of basic combinators you would expect in such a library is provided,
// like 'Unit' (succeeds once), 'Fail' (never succeeds), and 'Cut' (succeeds
// once, then curtails backtracking at the previous choice point), 'And' for
// conjunction of goals, 'Or' for adjunction, 'Not' for negation, and 'Unify*'
// for unification. The resolution process is started by calling 'Resolve' on
// a goal and then iterating over the solutions, which consist of substitution
// environments (proxy mappings) of variables to their bindings.
//
// The code makes use of the algebraic structure of the monadic combinators:
// 'Unit' and 'Then' form a Monoid over monadic combinator functions, as do
// 'Fail' and 'Choice', which allows us to fold a sequence of combinators into
// a single one. Taken thogether, these structures form a Distributive Lattice
// with 'Then' as the meet (infimum) and 'Choice' as the join (supremum)
// operator, a fact that is not utilized in the code, though. Because of the
// sequential nature of the employed resolution algorithm combined with the
// 'Cut', neither the lattice nor the monoids are commutative.
//
// Due to the absence of Tail Call Elimination in C#, Trampolining with
// Thunking is used to prevent stack overflows.

using System.Linq;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace Yogic;

// Miscellaneous type aliases.
using Seq = IReadOnlyCollection<object>;
using Pair = ValueTuple<object, object>;

// The type of the Substitution Environment.
// ImmutableDictionary brings everything we need for Trailing.
using Subst = ImmutableDictionary<Variable, object>;

// A Tuple of this type is returned for each successful resolution step.
// This enables Tail Call ELimination through Thunking and Trampolining.
using Result = Tuple<ImmutableDictionary<Variable, object>, Next>;

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
    private string name;

    public Variable(string name)
    {
        this.name = name;
    }

    public override string ToString() => $"Variable({name})";
}

public static class Combinators
{
    public class SubstProxy
    {
        private Subst subst;

        internal SubstProxy(Subst subst)
        {
            this.subst = subst;
        }

        public object this[Variable v] => Deref(subst, v);
    }

    private static object Deref(Subst subst, object obj)
    {
        // Chase down Variable bindings:
        while (obj is Variable variable && subst.ContainsKey(variable))
        {
            obj = subst[variable];
        }
        return obj;
    }

    private static Step Bind(Step step, Goal goal)
    {
        // Make 'goal' the continuation of 'step':
        return (succeed, backtrack, escape) =>
            step((subst, next) => goal(subst)(succeed, next, escape), backtrack, escape);
    }

    public static Step Unit(Subst subst)
    {
        return (succeed, backtrack, escape) => succeed(subst, backtrack);
    }

    public static Step Cut(Subst subst)
    {
        return (succeed, backtrack, escape) => succeed(subst, escape);
    }

    public static Step Fail(Subst subst)
    {
        return (succeed, backtrack, escape) => backtrack();
    }

    private static Goal Then(Goal goal1, Goal goal2)
    {
        // Sequencing is the default behavior of 'bind':
        return subst => Bind(goal1(subst), goal2);
    }

    public static Goal And(IEnumerable<Goal> goals)
    {
        // 'unit' and 'then' form a monoid, so we can just fold:
        return goals.Aggregate<Goal, Goal>(Unit, Then);
    }

    public static Goal And(Goal goal, params Goal[] goals)
    {
        return And(goals.Prepend(goal));
    }

    private static Goal Choice(Goal goal1, Goal goal2)
    {
        // We make 'goal2' the new backtracking path of 'goal1':
        return subst =>
            (succeed, backtrack, escape) =>
                goal1(subst)(succeed, () => goal2(subst)(succeed, backtrack, escape), escape);
    }

    public static Goal Or(IEnumerable<Goal> goals)
    {
        // 'fail' and 'choice' form a monoid, so we can just fold:
        var choices = goals.Aggregate<Goal, Goal>(Fail, Choice);
        // we inject 'backtrack' as the new escape path, so we can
        // curtail backtracking here and immediately continue at the
        // previous choice point instead:
        return subst =>
            (succeed, backtrack, escape) => choices(subst)(succeed, backtrack, backtrack);
    }

    public static Goal Or(Goal goal, params Goal[] goals)
    {
        return Or(goals.Prepend(goal));
    }

    // Negation as failure:
    public static Goal Not(Goal goal)
    {
        return Or(And(goal, Cut, Fail), Unit);
    }

    public static Goal Unify(object o1, object o2)
    {
        // Using an 'ImmutableDictionary' makes trailing easy:
        return subst =>
            (Deref(subst, o1), Deref(subst, o2)) switch
            {
                (var x1, var x2) when x1.Equals(x2) => Unit(subst),
                (Seq s1, Seq s2) when s1.Count == s2.Count => UnifyAll(s1.Zip(s2))(subst),
                (Variable v, var o) => Unit(subst.Add(v, o)),
                (var o, Variable v) => Unit(subst.Add(v, o)),
                _ => Fail(subst)
            };
    }

    public static Goal UnifyAll(IEnumerable<Pair> pairs)
    {
        return And(from pair in pairs select Unify(pair.Item1, pair.Item2));
    }

    public static Goal UnifyAll(Pair pair, params Pair[] pairs)
    {
        return UnifyAll(pairs.Prepend(pair));
    }

    public static Goal UnifyAny(Variable v, IEnumerable<object> objects)
    {
        return Or(from o in objects select Unify(v, o));
    }

    public static Goal UnifyAny(Variable v, object o, params object[] objects)
    {
        return UnifyAny(v, objects.Prepend(o));
    }

    private static Result? Quit() => null;

    private static Result? Emit(Subst subst, Next next) => new(subst, next);

    public static IEnumerable<SubstProxy> Resolve(Goal goal)
    {
        Result? result = goal(Subst.Empty)(Emit, Quit, Quit);
        // We have to implement Tail Call Elimination ourself:
        while (null != result)
        {
            yield return new SubstProxy(result.Item1);
            result = result.Item2();
        }
    }
}
