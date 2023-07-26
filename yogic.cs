// An embedded DSL for expressing first-order logical predicates and
// performing resolution with backtracking and pruning of search paths.
//
// Key features:
//
// - Horn Clauses: Express logical facts and implications as simple functions.
//
// - Combinators: Compose first-order logical expressions by simply sticking
// together combinator functions.
//
// - Logical Variables: Represented by the Variable class, they can be bound
// to arbitrary values and other variables during computation.
//
// - Substitution and Unification: The substitution environment provides
// variable bindings and is incrementally constructed during resolution.
//
// - Backtracking: The monad combines the List and the Triple-Barrelled
// Continuation Monads for resolution, backtracking, and pruning.
//
// - Algebraic Structures: 'unit' and 'then' form a monoid over monadic
// functions, as do 'fail' and 'choice'. Together they form a Bounded Lattice
// with 'then' as the meet (infimum) and 'choice' as the join (supremum)
// operators, and 'unit' and 'fail' as their respective identity elements.
// Because of the sequential nature of the employed resolution algorithm, the
// lattice is non-commutative.
//
// Links:
//
// Unification:
// https://eli.thegreenplace.net/2018/unification/
//
// Logical Resolution:
// http://web.cse.ohio-state.edu/~stiff.4/cse3521/logical-resolution.html
//
// Horn Clauses in Deductive Databases:
// https://www.geeksforgeeks.org/horn-clauses-in-deductive-databases/
//
// Continuations Made Simple and Illustrated:
// https://www.ps.uni-saarland.de/~duchier/python/continuations.html
//
// Monads Explained in C# (again):
// https://mikhail.io/2018/07/monads-explained-in-csharp-again/
//
// Discovering the Continuation Monad in C#:
// https://functionalprogramming.medium.com/deriving-continuation-monad-from-callbacks-23d74e8331d0
//
// The Discovery of Continuations:
// https://www.cs.ru.nl/~freek/courses/tt-2011/papers/cps/histcont.pdf
//
// Monoid:
// https://en.wikipedia.org/wiki/Monoid
//
// Bounded Lattice:
// https://en.wikipedia.org/wiki/Lattice_(order)#Bounded_lattice


using Subst = System.Collections.Immutable.ImmutableDictionary<Variable, object>;
using Solutions = System.Collections.Generic.IEnumerable<System.Collections.Immutable.ImmutableDictionary<Variable, object>>;


public delegate Solutions Retry();
public delegate Solutions Success(Subst subst, Retry retry);
public delegate Solutions Ma(Success yes, Retry no, Retry esc);
public delegate Ma Mf(Subst subst);


public class Variable {
  // Represents named logical variables.

  private string name;

  public Variable(string name) {
    this.name = name;
  }

  public override string? ToString() {
    return $"Variable( {this.name})";
  }

}


public static class Combinators {

  // Creates a new logical variable with the given name.
  public static Variable var(string name) {
    return new Variable(name);
  }

  // Takes a substitution environment and a retry continuation.
  // First yields the substitution environment once and then invokes
  // backtracking by delegating to the provided retry continuation.
  public static Solutions success(Subst subst, Retry retry) {
    yield return subst;
    foreach(var each in retry()) {
      yield return each;
    };
  }

  // Represents a failed computation.
  public static Solutions failure() {
    yield break;
  }

  // Applies the monadic computation mf to ma.
  public static Ma bind(Ma ma, Mf mf) {
    return (yes, no, esc) => ma((subst, retry) => mf(subst)(yes, retry, esc), no, esc);
  }

  // Lifts a substitution environment into a computation.
  public static Ma unit(Subst subst) {
    // we inject the current no continuation as backtracking continuation:
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
    return (yes, no, esc) => no();
  }

  // Composes two computations sequentially.
  public static Mf then(Mf mf, Mf mg) {
    return (subst) => bind(mf(subst), mg);
  }

  // Composes multiple computations sequentially from an enumerable.
  public static Mf and_from_enumerable(IEnumerable<Mf> mfs) {
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
    return (subst) => (yes, no, esc) => mf(subst)(yes, () => mg(subst)(yes, no, esc), esc);
  }

  // Represents a choice between multiple computations from an enumerable.
  // Takes a collection of computations mfs and returns a new computation that
  // tries all of them in series, allowing backtracking.
  public static Mf or_from_enumerable(IEnumerable<Mf> mfs) {
    Mf joined = mfs.Aggregate<Mf, Mf>(fail, choice);
    // we inject the current no continuation as escape continuation:
    return (subst) => (yes, no, esc) => joined(subst)(yes, no, no);
  }

  // Represents a choice between multiple computations.
  // Takes a variable number of computations and returns a new computation
  // that tries all of them in series, allowing backtracking.
  public static Mf or(params Mf[] mfs) {
    return or_from_enumerable(mfs);
  }

  // Negates the result of a computation.
  // Returns a new computation that succeeds if mf fails and vice versa.
  public static Mf not(Mf mf) {
    return or(and(mf, cut, fail), unit);
  }

  private static Mf _unify(ValueTuple<object, object> pair) {
    (var o1, var o2) = pair;
    Ma unifier(Subst subst) {
      return (deref(subst, o1), deref(subst, o2)) switch {
        (var o1, var o2) when o1 == o2 => unit(subst),
        (Variable o1, var o2) => unit(subst.Add(o1, o2)),
        (var o1, Variable o2) => unit(subst.Add(o2, o1)),
        _ => fail(subst),
      };
    }
    return unifier;
  }

  // Tries to unify pairs of objects. Fails if any pair is not unifiable.
  public static Mf unify(params ValueTuple<object, object>[] pairs) {
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
      unify((a, "fiffi"))
    );
  }

  public static Mf child(Variable a, Variable b) {
    return or(
      unify((a, "jim"), (b, "bob")),
      unify((a, "joe"), (b, "bob")),
      unify((a, "ian"), (b, "jim")),
      unify((a, "fiffi"), (b, "fluffy")),
      unify((a, "fluffy"), (b, "daisy"))
    );
  }

  public static Mf descendant(Variable a, Variable c) {
    Variable b = var("b");
    return (subst) =>
      or(
        child(a, c),
        and(child(a, b), descendant(b, c))
    )(subst);
  }

  public static Mf mortal(Variable a) {
    Variable b = var("b");
    return (subst) =>
      or(
        human(a),
        dog(a),
        and(descendant(a, b), mortal(b))
      )(subst);
  }

  public static void Main() {
    Variable x = var("x");
    Variable y = var("y");
    foreach (var subst in resolve(descendant(x, y))) {
      Console.WriteLine($"{subst[x]} is the descendant of {subst[y]}");
    };
    Console.WriteLine();
    foreach (var subst in resolve(and(mortal(x), not(dog(x))))) {
      Console.WriteLine($"{subst[x]} is mortal");
    };
  }

}
