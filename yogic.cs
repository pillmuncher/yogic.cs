// Copyright (c) 2023 Mick Krippendorf <m.krippendorf@freenet.de>

namespace yogic {

  using Subst = System.Collections.Immutable.ImmutableDictionary<Variable, object>;
  using Result = Tuple<System.Collections.Immutable.ImmutableDictionary<Variable, object>, Retry>;

  public delegate Result? Retry();
  public delegate Result? Emit(Subst subst, Retry retry);
  public delegate Result? Ma(Emit yes, Retry no, Retry esc);
  public delegate Ma Mf(Subst subst);

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

    public static Ma bind(Ma ma, Mf mf) {
      // we prepend 'mf' before the current 'yes'
      // continuation, making it the new one, and
      // inject the 'retry' continuation as the
      // subsequent 'no' continuation:
      return (yes, no, esc) => ma(no  : no,
                                  esc : esc,
                                  yes : (subst, retry) => mf(subst)(yes : yes,
                                                                    esc : esc,
                                                                    no  : retry));
    }

    public static Ma unit(Subst subst) {
      // we inject the current 'no' continuation
      // as retry continuation:
      return (yes, no, esc) => yes(subst, retry : no);
    }

    public static Ma cut(Subst subst) {
      // we inject the current escape continuation
      // as retry continuation:
      return (yes, no, esc) => yes(subst, retry : esc);
    }

    public static Ma fail(Subst subst) {
      // we immediately invoke backtracking,
      // omitting the 'yes' continuation:
      return (yes, no, esc) => no();
    }

    public static Mf then(Mf mf, Mf mg) {
      // sequencing is the default behavior of 'bind':
      return subst => bind(mf(subst), mg);
    }

    public static Mf and_from_enumerable(IEnumerable<Mf> mfs) {
      // 'unit' and 'then' form a monoid, so we can just fold:
      return mfs.Aggregate<Mf, Mf>(unit, then);
    }

    public static Mf and(params Mf[] mfs) => and_from_enumerable(mfs);

    public static Mf choice(Mf mf, Mf mg) {
      // we prepend 'mg' before the current 'no'
      // continuation, making it the new one:
      return subst
          => (yes, no, esc) => mf(subst)(yes : yes,
                                         esc : esc,
                                         no  : () => mg(subst)(yes : yes,
                                                               no  : no,
                                                               esc : esc));
    }

    public static Mf or_from_enumerable(IEnumerable<Mf> mfs) {
      // 'fail' and 'choice' form a monoid, so we can just fold:
      var choices = mfs.Aggregate<Mf, Mf>(fail, choice);
      // we inject the current 'no' continuation as escape
      // continuation, so we can jump out of a computation
      // and curtail backtracking at the previous choice point:
      return subst
          => (yes, no, esc) => choices(subst)(yes : yes,
                                              no  : no,
                                              esc : no);
    }

    public static Mf or(params Mf[] mfs) => or_from_enumerable(mfs);

    // negation as failure:
    public static Mf not(Mf mf) => or(and(mf, cut, fail), unit);

    private static object deref(Subst subst, object o) {
      // chase down Variable bindings:
      while (o is Variable && subst.ContainsKey((Variable) o)) {
        o = subst[(Variable) o];
      }
      return o;
    }

    private static Mf _unify(ValueTuple<object, object> pair)  {
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

    public static Mf unify(params ValueTuple<object, object>[] pairs) {
      // turn multiple unification requests into a continuation:
      return and_from_enumerable(from pair in pairs select _unify(pair));
    }

    public static Mf unify_any(Variable v, params object[] objects) {
      // turn multiple unification requests on a single variable into
      // retry continuations:
      return or_from_enumerable(from o in objects select _unify((v, o)));
    }

    public static IEnumerable<SubstProxy> resolve(Mf goal) {
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
