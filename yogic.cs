// Copyright (c) 2023 Mick Krippendorf <m.krippendorf@freenet.de>

// A library of Monadic Combinators for Logic Programming.
//
// It uses the Triple-Barreled Continuation Monad for resolution,
// backtracking, and branch pruning.
//
// To keep more closely to the terminology of logic programming and to not
// bother users too much with the terminology of monads and continuations, the
// monadic computation type is called 'step' and the monadic continuation type
// is called 'goal'.
//
// A set of basic combinators you would expect in such a library is provided,
// like 'unit' (succeeds once), 'fail' (never succeeds), and 'cut' (succeeds
// once, then curtails backtracking at the previous choice point), 'and' for
// conjunction of goals, 'or' for adjunction, 'not' for negation, and 'unify'
// and 'unify_any' for unification. The resolution process is started by
// calling 'resolve' on a goal and then iterating over the solutions, which
// consist of substitution environments (proxy mappings) of variables to their
// bindings.
//
// The code makes use of the algebraic structure of the monadic combinators:
// 'unit' and 'then' form a Monoid over monadic combinator functions, as do
// 'fail' and 'choice'. Together they form a Distributive Lattice with 'then'
// as the meet (infimum) and 'choice' as the join (supremum) operator, a fact
// that is not utilized in the code. Because of the sequential nature of the
// employed resolution algorithm combined with the 'cut', neither the lattice
// nor the monoids are commutative.
//
// Due to the absence of Tail Call Elimination in C#, Trampolining with
// Thunking is used to prevent stack overflows.

namespace yogic
{
    // The type of the Substitution Environment:
    using Subst = System.Collections.Immutable.ImmutableDictionary<Variable, object>;

    // A Tuple of this type is returned for each successful resolution step.
    // This enables Tail Call ELimination through Thunking and Trampolining.
    using Result = Tuple<System.Collections.Immutable.ImmutableDictionary<Variable, object>, Next>;

    // A function type that represents a backtracking operation:
    public delegate Result? Next();

    // A function type that represents a successful resolution:
    public delegate Result? Emit(Subst subst, Next next);

    // The monadic computation type. 'succeed' represents the current
    // continuation and 'backtrack' represents the normal backtracking path.
    // The 'escape' is continuation is only invoked by the 'cut' combinator to
    // curtail backtracking at the previous choice point.
    public delegate Result? Step(Emit succeed, Next backtrack, Next escape);

    // The monadic continuation type:
    public delegate Step Goal(Subst subst);

    public class Variable
    {
        private string Name { get; }

        public Variable(string name)
        {
            Name = name;
        }

        public override string ToString() => $"Variable({Name})";
    }

    public static class Yogic
    {
        public class SubstProxy
        {
            private Subst Subst { get; }

            internal SubstProxy(Subst subst)
            {
                Subst = subst;
            }

            public object this[Variable v] => deref(Subst, v);
        }

        private static object deref(Subst subst, object o)
        {
            // chase down Variable bindings:
            while (o is Variable && subst.ContainsKey((Variable)o))
            {
                o = subst[(Variable)o];
            }
            return o;
        }

        private static Step bind(Step step, Goal goal)
        {
            // Make 'goal' the continuation of 'step':
            return (succeed, backtrack, escape) =>
                step((subst, next) => goal(subst)(succeed, next, escape), backtrack, escape);
        }

        public static Step unit(Subst subst)
        {
            return (succeed, backtrack, escape) => succeed(subst, backtrack);
        }

        public static Step cut(Subst subst)
        {
            return (succeed, backtrack, escape) => succeed(subst, escape);
        }

        public static Step fail(Subst subst)
        {
            return (succeed, backtrack, escape) => backtrack();
        }

        private static Goal then(Goal goal1, Goal goal2)
        {
            // Sequencing is the default behavior of 'bind':
            return subst => bind(goal1(subst), goal2);
        }

        private static Goal and_from_enumerable(IEnumerable<Goal> goals) =>
            // 'unit' and 'then' form a monoid, so we can just fold:
            goals.Aggregate<Goal, Goal>(unit, then);

        public static Goal and(params Goal[] goals) => and_from_enumerable(goals);

        private static Goal choice(Goal goal1, Goal goal2)
        {
            // we make 'goal2' the new backtracking path of 'goal1':
            return subst =>
                (succeed, backtrack, escape) =>
                    goal1(subst)(succeed, () => goal2(subst)(succeed, backtrack, escape), escape);
        }

        private static Goal or_from_enumerable(IEnumerable<Goal> goals)
        {
            // 'fail' and 'choice' form a monoid, so we can just fold:
            var choices = goals.Aggregate<Goal, Goal>(fail, choice);
            // we make 'backtrack' the new escape path, so we can curtail backtracking:
            return subst =>
                (succeed, backtrack, escape) => choices(subst)(succeed, backtrack, backtrack);
        }

        public static Goal or(params Goal[] goals) => or_from_enumerable(goals);

        // Negation as failure:
        public static Goal not(Goal goal) => or(and(goal, cut, fail), unit);

        private static Goal _unify(ValueTuple<object, object> pair)
        {
            // using an 'ImmutableDictionary' makes trailing easy:
            return subst =>
                (deref(subst, pair.Item1), deref(subst, pair.Item2)) switch
                {
                    (var o1, var o2) when o1 == o2 => unit(subst),
                    (Variable v, var o) => unit(subst.Add(v, o)),
                    (var o, Variable v) => unit(subst.Add(v, o)),
                    _ => fail(subst)
                };
        }

        public static Goal unify(params ValueTuple<object, object>[] pairs) =>
            and_from_enumerable(from pair in pairs select _unify(pair));

        public static Goal unify_any(Variable v, params object[] objects) =>
            or_from_enumerable(from o in objects select _unify((v, o)));

        private static Result? quit() => null;

        private static Result? emit(Subst subst, Next next) => new(subst, next);

        public static IEnumerable<SubstProxy> resolve(Goal goal)
        {
            var result = goal(Subst.Empty)(emit, quit, quit);
            // We have to implement Tail Call Elimination ourself:
            while (null != result)
            {
                yield return new SubstProxy(result.Item1);
                result = result.Item2();
            }
        }
    }
}
