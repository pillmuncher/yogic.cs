// This module implements a backtracking monad for the resolution of Horn clauses,
// a concept used in logic programming. It is an embedded DSL for Prolog-like
// programs. It enables non-deterministic computations, allowing for multiple
// possible solutions for logical goals, and pruning of search branches.
//
// The key features of the code are as follows:
//
//   Horn Clauses: The code allows expressing Horn clauses, which are logical
//   statements with a head and a body. These clauses are used in logic
//   programming and represent implications.
//
//   Logical Variables: The code introduces the concept of logical variables,
//   represented by the Variable class. These variables can be bound to values
//   during the computation.
//
//   Substitution and Unification: The code uses the Subst class to represent
//   variable substitutions. When a logical variable and another object are
//   unified, the binding is added to the substitution environment.
//
//   Backtracking: The code leverages the List Monad to enable backtracking. It
//   yields substitution environments for a given goal, allowing the exploration
//   of various solutions to a logical query.
//
//   Triple-Barrelled Continuation Monad: The code uses a continuation monad with
//   three continuations: success, failure, and escape. These continuations enable
//   backtracking, pruning of search spaces, and handling success and failure
//   states during computation.
//
//   Combinators: The code provides a set of combinator functions that allow
//   composing computations and defining choices, sequences, negation,
//   unification, and more.
//
// In summary, this module offers a powerful mechanism for expressing logical
// formulas, performing backtracking searches, and finding solutions to logical
// queries. The use of the Triple-Barrelled Continuation Monad, logical variables,
// and substitution environments allows for a concise and expressive
// representation of complex logic-based computations.


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

  public static Variable var(string name) {
    // Creates a new logical variable with the given name.
    return new Variable(name);
  }

  public static Solutions success(Subst subst, Retry retry) {
    // Takes a substitution environment and a retry continuation.
    // First yields the substitution environment once and then invokes
    // backtracking by delegating to the provided retry continuation.
    yield return subst;
    foreach(Subst each in retry()) {
      yield return each;
    };
  }

  public static Solutions failure() {
    // Represents a failed computation.
    yield break;
  }

  public static Ma bind(Ma ma, Mf mf) {
    // Applies the monadic computation mf to ma.
    Solutions mb(Success yes, Retry no, Retry esc) {
      Solutions on_success(Subst subst, Retry retry) {
        return mf(subst)(yes, retry, esc);
      }
      return ma(on_success, no, esc);
    }
    return mb;
  }

  public static Ma unit(Subst subst) {
    // Lifts a substitution environment into a computation.
    Solutions ma(Success yes, Retry no, Retry esc) {
      return yes(subst, no);
    }
    return ma;
  }

  public static Ma cut(Subst subst) {
    // Succeeds once, and on backtracking aborts the current computation,
    // effectively pruning the search space.
    Solutions ma(Success yes, Retry no, Retry esc) {
      // we inject the current escape continuation
      // as the subsequent backtracking path:
      return yes(subst, esc);
    }
    return ma;
  }

  public static Ma fail(Subst subst) {
    // Represents a failed computation. Immediately initiates backtracking.
    Solutions ma(Success yes, Retry no, Retry esc) {
      return no();
    }
    return ma;
  }

  public static Mf then(Mf mf, Mf mg) {
    // Composes two computations sequentially.
    Ma mh(Subst subst) {
      return bind(mf(subst), mg);
    }
    return mh;
  }

  public static Mf seq_from_enumerable(IEnumerable<Mf> mfs) {
    // Composes multiple computations sequentially from an enumerable.
    return mfs.Aggregate<Mf, Mf>(unit, then);
  }

  public static Mf seq(params Mf[] mfs) {
    // Composes multiple computations sequentially.
    return seq_from_enumerable(mfs);
  }

  public static Mf choice(Mf mf, Mf mg) {
    // Represents a choice between two computations.
    // Takes two computations mf and mg and returns a new computation that tries
    // mf, and if that fails, falls back to mg.
    Ma mh(Subst subst) {
      Solutions ma(Success yes, Retry no, Retry esc) {
        Solutions on_fail() {
          return mg(subst)(yes, no, esc);
        }
        return mf(subst)(yes, on_fail, esc);
      }
      return ma;
    }
    return mh;
  }

  public static Mf amb_from_enumerable(IEnumerable<Mf> mfs) {
    // Represents a choice between multiple computations from an enumerable.
    // Takes a collection of computations mfs and returns a new computation that
    // tries all of them in series, allowing backtracking.
    Mf joined = mfs.Aggregate<Mf, Mf>(fail, choice);
    Ma mf(Subst subst) {
      Solutions ma(Success yes, Retry no, Retry esc) {
        // we inject the current no continuation
        // as the new escape continuation:
        return joined(subst)(yes, no, no);
      }
      return ma;
    }
    return mf;
  }

  public static Mf amb(params Mf[] mfs) {
    // Represents a choice between multiple computations.
    // Takes a variable number of computations and returns a new computation
    // that tries all of them in series, allowing backtracking.
    return amb_from_enumerable(mfs);
  }

  public static Mf not(Mf mf) {
    // Negates the result of a computation.
    // Returns a new computation that succeeds if mf fails and vice versa.
    return amb(seq(mf, cut, fail), unit);
  }

  private static Mf _unify(ValueTuple<object, object> pair) {
    (var o1, var o2) = pair;
    Ma unifier(Subst subst) {
      return (deref(subst, o1), deref(subst, o2)) switch {
        (object o1, object o2) when o1 == o2 => unit(subst),
        (Variable o1, object o2) => unit(subst.Add(o1, o2)),
        (object o1, Variable o2) => unit(subst.Add(o2, o1)),
        _ => fail(subst),
      };
    }
    return unifier;
  }

  public static Mf unify(params ValueTuple<object, object>[] pairs) {
    // Tries to unify pairs of objects.
    return seq_from_enumerable(from pair in pairs select _unify(pair));
  }

  private static object deref(Subst subst, object o) {
    // Performs variable dereferencing based on substitutions in an environment.
    while (o is Variable && subst.ContainsKey((Variable) o)) {
      o = subst[(Variable)o];
    };
    return o;
  }

  public static Solutions resolve(Mf goal) {
    // Perform the logical resolution of the computation represented by goal.
    return goal(Subst.Empty)(success, failure, failure);
  }

  // ----8<--------8<--------8<--------8<--------8<--------8<--------8<----

  public static Mf child(Variable a, Variable b) {
    return amb(
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
      amb(
        child(a, c),
        seq(child(a, b), descendant(b, c))
    )(subst);
  }

  public static Mf human(Variable a) {
    return amb(
      unify((a, "socrates")),
      unify((a, "plato")),
      unify((a, "archimedes"))
    );
  }

  public static Mf dog(Variable a) {
    return amb(
    );
  }

  public static Mf mortal(Variable a) {
    Variable b = var("b");
    return (subst) =>
      amb(
        human(a),
        dog(a),
        seq(descendant(a, b), mortal(b))
      )(subst);
  }

  public static void Main() {
    Variable x = var("x");
    Variable yes = var("yes");
    foreach (Subst subst in resolve(descendant(x, yes))) {
      Console.WriteLine(subst[x] + " is the descendant of " + subst[yes]);
    };
    Console.WriteLine();
    foreach (Subst subst in resolve(seq(mortal(x), not(dog(x))))) {
      Console.WriteLine(subst[x] + " is mortal");
    };
  }

}
