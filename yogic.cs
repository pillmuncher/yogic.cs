// Copyright (c) 2023 Mick Krippendorf <m.krippendorf@freenet.de>

namespace yogic {

  using Subst = System.Collections.Immutable.ImmutableDictionary<Variable, object>;
  using Solutions = System.Collections.Generic.IEnumerable<System.Collections.Immutable.ImmutableDictionary<Variable, object>>;
  
  public delegate Tuple<Subst, Thunk>? Thunk();
  public delegate Tuple<Subst, Thunk>? Result(Subst subst, Thunk backtrack);
  public delegate Tuple<Subst, Thunk>? Ma(Result yes, Thunk no, Thunk esc);
  public delegate Ma Mf(Subst subst);


  public class Variable {
    private string Name { get; }
    public Variable(string name) { Name = name; }
    public override string ToString() => $"Variable({Name})";
  }


  public static class Yogic {

    private static Solutions trampoline(Thunk thunk) { 
      // C# doesn't have Tail Call Elimination,
      // so we have to implement it ourself:
      Tuple<Subst, Thunk>? result = thunk();
      while(result != null) {
        (var subst, thunk) = result;
        yield return subst;
        result = thunk();
      }
    }

    private static Tuple<Subst, Thunk>? quit() {
      // no solutions:
      return null;
    }

    private static Tuple<Subst, Thunk> emit(Subst subst, Thunk backtrack) {
      // the current solution plus all the
      // solutions retrieved from backtracking:
      return new (subst, backtrack);
    }

    public static Ma bind(Ma ma, Mf mf) =>
      // prepend 'mf' before the current 'yes'
      // continuation, making it the new one,
      // and inject the 'backtrack' continuation
      // as the subsequent 'no' continuation:
      (yes, no, esc) => ma(no  : no,
                           esc : esc,
                           yes : (subst, backtrack) => mf(subst)(yes : yes,
                                                                 esc : esc,
                                                                 no  : backtrack));

    public static Ma unit(Subst subst) =>
      // inject the current 'no' continuation
      // as backtrack continuation:
      (yes, no, esc) => yes(subst, backtrack : no);

    public static Ma cut(Subst subst) =>
      // inject the current escape continuation
      // as backtrack continuation:
      (yes, no, esc) => yes(subst, backtrack : esc);

    public static Ma fail(Subst subst) =>
      // immediately invoke backtracking,
      // omitting the 'yes' continuation:
      (yes, no, esc) => no();

    public static Mf then(Mf mf, Mf mg) =>
      // sequencing is the default behavior of 'bind':
      subst => bind(mf(subst), mg);

    public static Mf and_from_enumerable(IEnumerable<Mf> mfs) =>
      // 'unit' and 'then' form a monoid,
      // so we can just fold:
      mfs.Aggregate<Mf, Mf>(unit, then);

    public static Mf and(params Mf[] mfs) =>
      and_from_enumerable(mfs);


    public static Mf choice(Mf mf, Mf mg) =>
      // create a choice point.
      // prepend 'mg' before the current 'no'
      // continuation, making it the new one:
      subst =>
        (yes, no, esc) => mf(subst)(yes : yes,
                                    esc : esc,
                                    no  : () => mg(subst)(yes : yes,
                                                          no  : no,
                                                          esc : esc));

    public static Mf or_from_enumerable(IEnumerable<Mf> mfs) {
      // 'fail' and 'choice' form a monoid, so we can just fold:
      var choices = mfs.Aggregate<Mf, Mf>(fail, choice);
      // inject the current 'no' continuation as escape
      // continuation, so we can jump out of a computation
      // and curtail backtracking at the previous choice point:
      return subst =>
              (yes, no, esc) => choices(subst)(yes : yes,
                                               no  : no,
                                               esc : no);
    }

    public static Mf or(params Mf[] mfs) =>
      or_from_enumerable(mfs);

    public static Mf not(Mf mf) =>
      // negation as failure:
      or(and(mf, cut, fail), unit);

    private static object deref(Subst subst, object o) {
      // chase down Variable bindings:
      while (o is Variable && subst.ContainsKey((Variable) o)) {
        o = subst[(Variable)o];
      }
      return o;
    }

    private static Mf _unify(ValueTuple<object, object> pair) {
      // using an 'ImmutableDictionary' makes trailing easy:
      (var o1, var o2) = pair;
      return subst => (deref(subst, o1), deref(subst, o2)) switch {
                        (var o1, var o2) when o1 == o2 => unit(subst),
                        (Variable o1, var o2) => unit(subst.Add(o1, o2)),
                        (var o1, Variable o2) => unit(subst.Add(o2, o1)),
                        _ => fail(subst)};
    }

    public static Mf unify(params ValueTuple<object, object>[] pairs) =>
      // turn multiple unification requests into a continuation:
      and_from_enumerable(from pair in pairs select _unify(pair));

    public static Mf unify_any(Variable v, params object[] objects) =>
      // turn multiple unification requests of a
      // single variable into choice continuations:
      or_from_enumerable(from o in objects select _unify((v, o)));

    public class SubstProxy {
      private Subst Subst { get; }
      public SubstProxy(Subst subst) { Subst = subst; }
      public object this[Variable v] => deref(Subst, v);
    }

    public static IEnumerable<SubstProxy> resolve(Mf goal) =>
      trampoline(() => goal(Subst.Empty)(emit, quit, quit)).Select(s => new SubstProxy(s));

  }

}
