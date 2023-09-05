// Copyright (c) 2023 Mick Krippendorf <m.krippendorf@freenet.de>

namespace yogic {

  using Subst = System.Collections.Immutable.ImmutableDictionary<Variable, object>;
  using Result = Tuple<System.Collections.Immutable.ImmutableDictionary<Variable, object>, Retry>;

  public delegate Result? Retry();
  public delegate Result? Emit(Subst subst, Retry retry);
  public delegate Result? Comp(Emit yes, Retry no, Retry esc);
  public delegate Comp Cont(Subst subst);

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
    private static Result? emit(Subst subst, Retry retry) => new (subst, retry);

    public static Comp bind(Comp comp, Cont cont) {
      // we prepend 'cont' before the current 'yes'
      // continuation, making it the new one, and
      // inject the 'retry' continuation as the
      // subsequent 'no' continuation:
      return (yes, no, esc) => comp(no  : no,
                                    esc : esc,
                                    yes : (subst, retry) => cont(subst)(yes : yes,
                                                                        esc : esc,
                                                                        no  : retry));
    }

    public static Comp unit(Subst subst) {
      // we inject the current 'no' continuation
      // as retry continuation:
      return (yes, no, esc) => yes(subst, retry : no);
    }

    public static Comp cut(Subst subst) {
      // we inject the current escape continuation
      // as retry continuation:
      return (yes, no, esc) => yes(subst, retry : esc);
    }

    public static Comp fail(Subst subst) {
      // we immediately invoke backtracking,
      // omitting the 'yes' continuation:
      return (yes, no, esc) => no();
    }

    public static Cont then(Cont cont1, Cont cont2) {
      // sequencing is the default behavior of 'bind':
      return subst => bind(cont1(subst), cont2);
    }

    public static Cont and_from_enumerable(IEnumerable<Cont> conts) {
      // 'unit' and 'then' form a monoid, so we can just fold:
      return conts.Aggregate<Cont, Cont>(unit, then);
    }

    public static Cont and(params Cont[] conts) => and_from_enumerable(conts);

    public static Cont choice(Cont cont1, Cont cont2) {
      // we prepend 'cont2' before the current 'no'
      // continuation, making it the new one:
      return subst
          => (yes, no, esc) => cont1(subst)(yes : yes,
                                            esc : esc,
                                            no  : () => cont2(subst)(yes : yes,
                                                                     no  : no,
                                                                     esc : esc));
    }

    public static Cont or_from_enumerable(IEnumerable<Cont> conts) {
      // 'fail' and 'choice' form a monoid, so we can just fold:
      var choices = conts.Aggregate<Cont, Cont>(fail, choice);
      // we inject the current 'no' continuation as escape
      // continuation, so we can jump out of a computation
      // and curtail backtracking at the previous choice point:
      return subst
          => (yes, no, esc) => choices(subst)(yes : yes,
                                              no  : no,
                                              esc : no);
    }

    public static Cont or(params Cont[] conts) => or_from_enumerable(conts);

    // negation as failure:
    public static Cont not(Cont cont) => or(and(cont, cut, fail), unit);

    private static object deref(Subst subst, object o) {
      // chase down Variable bindings:
      while (o is Variable && subst.ContainsKey((Variable) o)) {
        o = subst[(Variable) o];
      }
      return o;
    }

    private static Cont _unify(ValueTuple<object, object> pair)  {
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

    public static Cont unify(params ValueTuple<object, object>[] pairs) {
      // turn multiple unification requests into a continuation:
      return and_from_enumerable(from pair in pairs select _unify(pair));
    }

    public static Cont unify_any(Variable v, params object[] objects) {
      // turn multiple unification requests on a single variable into
      // retry continuations:
      return or_from_enumerable(from o in objects select _unify((v, o)));
    }

    public static IEnumerable<SubstProxy> resolve(Cont goal) {
      // C# doesn't have Tail Call Elimination,
      // so we have to implement it ourself:
      var result = goal(Subst.Empty)(emit, quit, quit);
      while (null != result) {
        yield return new SubstProxy(result.Item1);
        result = result.Item2();
      }
    }

  }

}
