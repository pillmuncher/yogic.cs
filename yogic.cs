// Copyright (c) 2023 Mick Krippendorf <m.krippendorf@freenet.de>

using Subst = System.Collections.Immutable.ImmutableDictionary<Variable, object>;
using Solutions = System.Collections.Generic.IEnumerable<System.Collections.Immutable.ImmutableDictionary<Variable, object>>;


public delegate Solutions Failure();
public delegate Solutions Success(Subst subst, Failure backtrack);
public delegate Solutions Ma(Success yes, Failure no, Failure esc);
public delegate Ma Mf(Subst subst);


public class Variable {

  private string Name { get; }

  public Variable(string name) {
    Name = name;
  }

  public override string ToString() {
    return $"Variable({Name})";
  }

}


public static class Yogic {

  private static Solutions failure() {
    yield break;
  }

  private static Solutions success(Subst subst, Failure backtrack) {
    yield return subst;
    foreach(var each in backtrack()) {
      yield return each;
    };
  }

  public static Ma bind(Ma ma, Mf mf) {
    // prepend 'mf' before the current 'yes' continuation,
    // making it the new one, and inject the 'backtrack'
    // continuation as the subsequent 'no' continuation:
    return (yes, no, esc) => ma(no  : no,
                                esc : esc,
                                yes : (subst, backtrack) => mf(subst)(yes : yes,
                                                                      esc : esc,
                                                                      no  : backtrack));
    }

  public static Ma unit(Subst subst) {
    // inject the current 'no' continuation as backtrack continuation:
    return (yes, no, esc) => yes(subst,
                                 backtrack : no);
  }

  public static Ma cut(Subst subst) {
    // inject the current escape continuation as backtrack continuation:
    return (yes, no, esc) => yes(subst,
                                 backtrack : esc);
  }

  public static Ma fail(Subst subst) {
    // immediately invoke backtracking, omitting the 'yes'continuation:
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

  public static Mf and(params Mf[] mfs) {
    return and_from_enumerable(mfs);
  }

  public static Mf choice(Mf mf, Mf mg) {
    // prepend 'mg' before the current 'no' continuation, making it the
    // new one:
    return subst =>
                (yes, no, esc) => mf(subst)(yes : yes,
                                            esc : esc,
                                            no  : () => mg(subst)(yes : yes,
                                                                  no  : no,
                                                                  esc : esc));
  }

  public static Mf or_from_enumerable(IEnumerable<Mf> mfs) {
    // 'fail' and 'choice' form a monoid, so we can just fold:
    var choices = mfs.Aggregate<Mf, Mf>(fail, choice);
    // inject the current 'no' continuation as escape continuation,
    // so we can jump out of a computation and curtail backtracking
    // at the previous choice point:
    return subst =>
                (yes, no, esc) => choices(subst)(yes : yes,
                                                 no  : no,
                                                 esc : no);
  }

  public static Mf or(params Mf[] mfs) {
    return or_from_enumerable(mfs);
  }

  public static Mf not(Mf mf) {
    // negation as failure:
    return or(and(mf, cut, fail), unit);
  }

  private static object deref(Subst subst, object o) {
    while (o is Variable && subst.ContainsKey((Variable) o)) {
      o = subst[(Variable)o];
    };
    return o;
  }

  private static Mf _unify(ValueTuple<object, object> pair) {
    // using an 'ImmutableDictionary' makes trailing easy:
    (var o1, var o2) = pair;
    return subst => (deref(subst, o1), deref(subst, o2)) switch {
                        (var o1, var o2) when o1 == o2 => unit(subst),
                        (Variable o1, var o2) => unit(subst.Add(o1, o2)),
                        (var o1, Variable o2) => unit(subst.Add(o2, o1)),
                        _ => fail(subst),
                    };
  }

  public static Mf unify(params ValueTuple<object, object>[] pairs) {
    // turn multiple unification requests into a continuation:
    return and_from_enumerable(from pair in pairs select _unify(pair));
  }

  public static Mf unify_any(Variable v, params object[] os) {
    return or_from_enumerable(from o in os select _unify((v, o)));
  }

  public class SubstProxy {

    private Subst Subst { get; }

    public SubstProxy(Subst subst) {
      Subst = subst;
    }

    public object this[Variable v] => deref(Subst, v);

  }

  public static IEnumerable<SubstProxy> resolve(Mf goal) {
    return goal(Subst.Empty)(success, failure, failure).Select(s => new SubstProxy(s));
  }

  // ----8<--------8<--------8<--------8<--------8<--------8<--------8<----

  public static Mf human(Variable a) {
    return unify_any(a, "socrates", "plato", "archimedes");
  }

  public static Mf dog(Variable a) {
    return unify_any(a, "fluffy", "daisy", "fifi");
  }

  public static Mf child(Variable a, Variable b) {
    return or(
      unify((a, "jim"), (b, "bob")),
      unify((a, "joe"), (b, "bob")),
      unify((a, "ian"), (b, "jim")),
      unify((a, "fifi"), (b, "fluffy")),
      unify((a, "fluffy"), (b, "daisy"))
    );
  }

  public static Mf descendant(Variable a, Variable c) {
    var b = new Variable("b");
    return (subst) => or(
      child(a, c),
      and(child(a, b), descendant(b, c))
    )(subst);
  }

  public static Mf mortal(Variable a) {
    var b = new Variable("b");
    return (subst) => or(
      human(a),
      dog(a),
      and(descendant(a, b), mortal(b))
    )(subst);
  }

  public static void Main() {
    var x = new Variable("x");
    var y = new Variable("y");
    foreach (var subst in resolve(descendant(x, y))) {
      Console.WriteLine($"{subst[x]} is a descendant of {subst[y]}.");
    };
    Console.WriteLine();
    foreach (var subst in resolve(and(mortal(x), not(dog(x))))) {
      Console.WriteLine($"{subst[x]} is mortal and no dog.");
    };
    Console.WriteLine();
    foreach (var subst in resolve(and(not(dog(x)), mortal(x)))) {
      Console.WriteLine($"{subst[x]} is mortal and no dog.");
    };
  }

}
