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
// bother users too much with jargon like 'Monad' and 'Continuation', the
// monadic computation type is called 'Step' and the monadic continuation type
// is called 'Goal'.
//
// A set of basic combinators you would expect in such a library is provided,
// like 'Unit' (succeeds once), 'Fail' (never succeeds), and 'Cut' (succeeds
// once, then curtails backtracking at the previous choice point), 'And' for
// conjunction of goals, 'Or' for adjunction, 'Not' for negation, and 'Unify'
// and 'Unify_any' for unification. The resolution process is started by
// calling 'resolve' on a goal and then iterating over the solutions, which
// consist of substitution environments (proxy mappings) of variables to their
// bindings.
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

namespace Yogic
{
    // Only used for recursive unification of collections:
    using Seq = System.Collections.Generic.ICollection<object>;

    // The type of the Substitution Environment.
    // ImmutableDictionary brings everything we need for Trailing.
    using Subst = System.Collections.Immutable.ImmutableDictionary<Variable, object>;

    // A Tuple of this type is returned for each successful resolution step.
    // This enables Tail Call ELimination through Thunking and Trampolining.
    using Result = Tuple<System.Collections.Immutable.ImmutableDictionary<Variable, object>, Next>;

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
        private string Name { get; }

        public Variable(string name)
        {
            Name = name;
        }

        public override string ToString() => $"Variable({Name})";
    }

    public static class Combinators
    {
        public class SubstProxy
        {
            private Subst Subst { get; }

            internal SubstProxy(Subst subst)
            {
                Subst = subst;
            }

            public object this[Variable v] => Deref(Subst, v);
        }

        private static object Deref(Subst subst, object obj)
        {
            // Chase down Variable bindings:
            while (obj is Variable && subst.ContainsKey((Variable)obj))
            {
                obj = subst[(Variable)obj];
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

        private static Goal AndFromEnumerable(IEnumerable<Goal> goals) =>
            // 'unit' and 'then' form a monoid, so we can just fold:
            goals.Aggregate<Goal, Goal>(Unit, Then);

        public static Goal And(params Goal[] goals) => AndFromEnumerable(goals);

        private static Goal Choice(Goal goal1, Goal goal2)
        {
            // We make 'goal2' the new backtracking path of 'goal1':
            return subst =>
                (succeed, backtrack, escape) =>
                    goal1(subst)(succeed, () => goal2(subst)(succeed, backtrack, escape), escape);
        }

        private static Goal OrFromEnumerable(IEnumerable<Goal> goals)
        {
            // 'fail' and 'choice' form a monoid, so we can just fold:
            var choices = goals.Aggregate<Goal, Goal>(Fail, Choice);
            // we inject 'backtrack' as the new escape path, so we can
            // curtail backtracking here and immediately continue at the
            // previous choice point instead.
            return subst =>
                (succeed, backtrack, escape) => choices(subst)(succeed, backtrack, backtrack);
        }

        public static Goal Or(params Goal[] goals) => OrFromEnumerable(goals);

        // Negation as failure:
        public static Goal Not(Goal goal) => Or(And(goal, Cut, Fail), Unit);

        private static Goal UnifySeqs(Seq seq1, Seq seq2)
        {
            if (seq1.GetType() != seq2.GetType() || seq1.Count != seq2.Count)
            {
                return Fail;
            };
            return AndFromEnumerable(from pair in seq1.Zip(seq2) select UnifyPair(pair));
        }

        private static Goal UnifyPair(ValueTuple<object, object> pair)
        {
            // Using an 'ImmutableDictionary' makes trailing easy:
            return subst =>
                (Deref(subst, pair.Item1), Deref(subst, pair.Item2)) switch
                {
                    (var o1, var o2) when o1 == o2 => Unit(subst),
                    (Variable v, var o) => Unit(subst.Add(v, o)),
                    (var o, Variable v) => Unit(subst.Add(v, o)),
                    (Seq seq1, Seq seq2) => UnifySeqs(seq1, seq2)(subst),
                    _ => Fail(subst)
                };
        }

        public static Goal Unify(params ValueTuple<object, object>[] pairs) =>
            AndFromEnumerable(from pair in pairs select UnifyPair(pair));

        public static Goal UnifyAny(Variable v, params object[] objects) =>
            OrFromEnumerable(from o in objects select UnifyPair((v, o)));

        private static Result? Quit() => null;

        private static Result? Emit(Subst subst, Next next) => new(subst, next);

        public static IEnumerable<SubstProxy> Resolve(Goal goal)
        {
            var result = goal(Subst.Empty)(Emit, Quit, Quit);
            // We have to implement Tail Call Elimination ourself:
            while (null != result)
            {
                yield return new SubstProxy(result.Item1);
                result = result.Item2();
            }
        }
    }
}
