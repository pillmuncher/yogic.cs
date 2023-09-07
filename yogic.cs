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
// as the meet (infimum) and 'choice' as the join (supremum) operator, and
// 'unit' and 'fail' as their respective identity elements. Because of the
// sequential nature of the employed resolution algorithm combined with the
// 'cut', the lattice is non-commutative.
//
// Due to the absence of Tail Call Elimination in C#, Trampolining with
// Thunking is used to prevent stack overflows.
//
// Useful links for a deeper understanding of the code:
//
// Monoids:  
// https://en.wikipedia.org/wiki/Monoid
//
// Distributive Lattices:  
// https://en.wikipedia.org/wiki/Distributive_lattice
//
// Monads:  
// https://en.wikipedia.org/wiki/Monad_(functional_programming)
//
// Monads Explained in C# (again):  
// https://mikhail.io/2018/07/monads-explained-in-csharp-again/
//
// Discovering the Continuation Monad in C#:  
// https://functionalprogramming.medium.com/deriving-continuation-monad-from-callbacks-23d74e8331d0
//
// Continuations:  
// https://en.wikipedia.org/wiki/Continuation
//
// Continuations Made Simple and Illustrated:  
// https://www.ps.uni-saarland.de/~duchier/python/continuations.html
//
// The Discovery of Continuations:  
// https://www.cs.ru.nl/~freek/courses/tt-2011/papers/cps/histcont.pdf
//
// Tail Calls:  
// https://en.wikipedia.org/wiki/Tail_call
//
// On Recursion, Continuations and Trampolines:  
// https://eli.thegreenplace.net/2017/on-recursion-continuations-and-trampolines/

namespace yogic {

  // The type of the Substitution Envirnonment:
  using Subst = System.Collections.Immutable.ImmutableDictionary<Variable, object>;

  // A Tuple of this type is returned for each successful resolution step: 
  using Result = Tuple<System.Collections.Immutable.ImmutableDictionary<Variable, object>, Next>;
  // This enables Tail Call ELimination through Thunking and Trampolining.

  // A function type that represents a backtracking operation:
  public delegate Result? Next();

  // A function type that represents a successful resolution:
  public delegate Result? Emit(Subst subst, Next next);

  // The monadic computation type:
  public delegate Result? Step(Emit succeed, Next backtrack, Next escape);
  // 'succeed' represents the current continuation and 'backtrack' represents
  // the normal backtracking path. The 'escape' is continuation is only
  // invoked by the 'cut' combinator to curtail backtracking at the previous
  // choice point.
  
  // The monadic continuation type:
  public delegate Step Goal(Subst subst);

  public class Variable {
    private string Name { get; }
    public Variable(string name) { Name = name; }
    public override string ToString() => $"Variable({Name})";
  }

  public static class Yogic {

    public class SubstProxy {
      private Subst Subst { get; }
      internal SubstProxy(Subst subst) { Subst = subst; }
      public object this[Variable v] => deref(Subst, v);
    }

    private static Result? quit() => null;
    private static Result? emit(Subst subst, Next next) => new (subst, next);

    // Make 'goal' the continuation of 'step':
    private static Step bind(Step step, Goal goal) {
      return (succeed, backtrack, escape)
          => step(backtrack: backtrack,
                  escape: escape,
                  succeed: (subst, next) => goal(subst)(succeed: succeed,
                                                        backtrack: next,
                                                        escape: escape));
    }

    public static Step unit(Subst subst) {
      return (succeed, backtrack, escape) => succeed(subst, next: backtrack);
    }

    public static Step cut(Subst subst) {
      return (succeed, backtrack, escape) => succeed(subst, next: escape);
    }

    public static Step fail(Subst subst) {
      return (succeed, backtrack, escape) => backtrack();
    }

    // Conjunction:
    private static Goal then(Goal goal1, Goal goal2) {
      // Sequencing is the default behavior of 'bind':
      return subst => bind(goal1(subst), goal2);
    }

    // Conjunction:
    private static Goal and_from_enumerable(IEnumerable<Goal> goals) {
      // 'unit' and 'then' form a monoid, so we can just fold:
      return goals.Aggregate<Goal, Goal>(unit, then);
    }

    // Conjunction:
    public static Goal and(params Goal[] goals) => and_from_enumerable(goals);

    // Adjunction:
    private static Goal choice(Goal goal1, Goal goal2) {
      // we make 'goal2' the new backtracking path:
      return subst
          => (succeed, backtrack, escape)
              => goal1(subst)(succeed: succeed,
                              escape: escape,
                              backtrack: () => goal2(subst)(succeed: succeed,
                                                            backtrack: backtrack,
                                                            escape: escape));
    }

    // Adjunction:
    private static Goal or_from_enumerable(IEnumerable<Goal> goals) {
      // 'fail' and 'choice' form a monoid, so we can just fold:
      var choices = goals.Aggregate<Goal, Goal>(fail, choice);
      // we make 'backtrack' the new escape path, so we can curtail backtracking:
      return subst
          => (succeed, backtrack, escape)
              => choices(subst)(succeed: succeed,
                                backtrack: backtrack,
                                escape: backtrack);
    }

    // Adjunction:
    public static Goal or(params Goal[] goals) => or_from_enumerable(goals);

    // Negation as failure:
    public static Goal not(Goal goal) => or(and(goal, cut, fail), unit);

    private static object deref(Subst subst, object o) {
      // chase down Variable bindings:
      while (o is Variable && subst.ContainsKey((Variable) o)) {
        o = subst[(Variable) o];
      }
      return o;
    }

    private static Goal _unify(ValueTuple<object, object> pair)  {
      // using an 'ImmutableDictionary' makes trailing easy:
      return subst
          => (deref(subst, pair.Item1), deref(subst, pair.Item2))
                switch {
                  (var o1, var o2) when o1 == o2 => unit(subst),
                  (Variable v, var o) => unit(subst.Add(v, o)),
                  (var o, Variable v) => unit(subst.Add(v, o)),
                  _ => fail(subst)
                };
    }

    public static Goal unify(params ValueTuple<object, object>[] pairs) {
      // turn multiple unification requests into a conjunction:
      return and_from_enumerable(from pair in pairs select _unify(pair));
    }

    public static Goal unify_any(Variable v, params object[] objects) {
      // turn unification requests on a single variable into an adjunction:
      return or_from_enumerable(from o in objects select _unify((v, o)));
    }

    public static IEnumerable<SubstProxy> resolve(Goal goal) {
      var result = goal(Subst.Empty)(succeed: emit, backtrack: quit, escape: quit);
      // We have to implement Tail Call Elimination ourself:
      while (null != result) {
        yield return new SubstProxy(result.Item1);
        result = result.Item2();
      }
    }

  }

}
