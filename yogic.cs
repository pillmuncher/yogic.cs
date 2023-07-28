using Subst = System.Collections.Immutable.ImmutableDictionary<Variable, object>;
using Solutions = System.Collections.Generic.IEnumerable<System.Collections.Immutable.ImmutableDictionary<Variable, object>>;


public delegate Solutions Retry();
public delegate Solutions Success(Subst subst, Retry retry);
public delegate Solutions Ma(Success yes, Retry no, Retry esc);
public delegate Ma Mf(Subst subst);


// Represents named logical variables.
public class Variable {

  private readonly string name;

  public Variable(string name) {
    this.name = name;
  }

  public override string? ToString() {
    return $"Variable({this.name})";
  }

}


public static class Combinators {

  // Creates a new logical variable with the given name.
  public static Variable var(string name) {
    return new Variable(name);
  }

  // Represents a successful resolution.
  // Takes a substitution environment and a retry continuation.
  // First yields the substitution environment once and then invokes
  // backtracking by delegating to the provided retry continuation.
  public static Solutions success(Subst subst, Retry retry) {
    yield return subst;
    foreach(var each in retry()) {
      yield return each;
    };
  }

  // Represents a failed resolution.
  public static Solutions failure() {
    yield break;
  }

  // Applies the monadic computation mf to ma.
  public static Ma bind(Ma ma, Mf mf) {
    // prepend 'mf' before the current 'yes' continuation, making it the new one:
    return (yes, no, esc) => ma((subst, retry) => mf(subst)(yes, retry, esc), no, esc);
  }

  // Lifts a substitution environment into a computation.
  public static Ma unit(Subst subst) {
    // we inject the current 'no' continuation as backtracking continuation:
    return (yes, no, esc) => yes(subst, no);
  }

  // Succeeds once, and on backtracking aborts the current computation,
  // effectively pruning the search space.
  public static Ma cut(Subst subst) {
    // we inject the current escape continuation as backtracking continuation:
    return (yes, no, esc) => yes(subst, esc);
  }

  // Represents a failed computation. Immediately initiates backtracking.
  public static Ma fail(Subst subst) {
    // immediately invoke backtracking, omitting the success continuation:
    return (yes, no, esc) => no();
  }

  // Composes two computations sequentially.
  public static Mf then(Mf mf, Mf mg) {
    // sequencing is the default behavior of 'bind':
    return subst => bind(mf(subst), mg);
  }

  // Composes multiple computations sequentially from an enumerable.
  public static Mf and_from_enumerable(IEnumerable<Mf> mfs) {
    // 'unit' and 'then' form a monoid, so we can just fold:
    return mfs.Aggregate<Mf, Mf>(unit, then);
  }

  // Composes multiple computations sequentially.
  public static Mf and(params Mf[] mfs) {
    return and_from_enumerable(mfs);
  }

  // Represents a choice between two computations. 
  // Takes two computations mf and mg and returns a new computation that tries
  // mf, and if that fails, falls back to mg.
  public static Mf choice(Mf mf, Mf mg) {
    // prepend 'mg' before the current 'no' continuation, making it the new one:
    return subst => (yes, no, esc) => mf(subst)(yes, () => mg(subst)(yes, no, esc), esc);
  }

  // Represents a choice between multiple computations from an enumerable.
  // Takes a collection of computations mfs and returns a new computation that
  // tries all of them in series with backtracking.
  public static Mf or_from_enumerable(IEnumerable<Mf> mfs) {
    // 'fail' and 'choice' form a monoid, so we can just fold:
    Mf joined = mfs.Aggregate<Mf, Mf>(fail, choice);
    // we also inject the current 'no' continuation as escape
    // continuation, so we can jump out of a computation:
    return subst => (yes, no, esc) => joined(subst)(yes, no, no);
  }

  // Represents a choice between multiple computations.
  // Takes a variable number of computations and returns a new computation that
  // tries all of them in series with backtracking.
  public static Mf or(params Mf[] mfs) {
    return or_from_enumerable(mfs);
  }

  // Negates the result of a computation.
  // Returns a new computation that succeeds if mf fails and vice versa.
  public static Mf not(Mf mf) {
    // negation as failure:
    return or(and(mf, cut, fail), unit);
  }

  private static Mf _unify(ValueTuple<object, object> pair) {
    // using an 'ImmutableDictionary' makes trailing easy:
    (var o1, var o2) = pair;
    return subst =>
      (deref(subst, o1), deref(subst, o2)) switch {
        (var o1, var o2) when o1 == o2 => unit(subst),
        (Variable o1, var o2) => unit(subst.Add(o1, o2)),
        (var o1, Variable o2) => unit(subst.Add(o2, o1)),
        _ => fail(subst),
      };
  }

  // Tries to unify pairs of objects. Fails if any pair is not unifiable.
  public static Mf unify(params ValueTuple<object, object>[] pairs) {
    // we turn multiple unification requests into a continuation:
    return and_from_enumerable(from pair in pairs select _unify(pair));
  }

  // Performs variable dereferencing based on substitutions in an environment.
  private static object deref(Subst subst, object o) {
    while (o is Variable && subst.ContainsKey((Variable) o)) {
      o = subst[(Variable)o];
    };
    return o;
  }

  // Perform the logical resolution of the computation represented by goal.
  public static Solutions resolve(Mf goal) {
    return goal(Subst.Empty)(success, failure, failure);
  }

  // ----8<--------8<--------8<--------8<--------8<--------8<--------8<----

  public static Mf human(Variable a) {
    return or(
      unify((a, "socrates")),
      unify((a, "plato")),
      unify((a, "archimedes"))
    );
  }

  public static Mf dog(Variable a) {
    return or(
      unify((a, "fluffy")),
      unify((a, "daisy")),
      unify((a, "fifi"))
    );
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
      Console.WriteLine($"{subst[x]} is the descendant of {subst[y]}.");
    };
    Console.WriteLine();
    foreach (var subst in resolve(and(mortal(x), not(dog(x))))) {
      Console.WriteLine($"{subst[x]} is mortal and no dog.");
    };
  }

}
