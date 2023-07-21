// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Deref = System.Func<Variable, object>;
using Pair = System.Collections.Generic.KeyValuePair<Variable, object>;
using Solution = System.Collections.Immutable.ImmutableStack<System.Collections.Generic.KeyValuePair<Variable, object>>;
using Solutions = System.Collections.Generic.IEnumerable<System.Collections.Immutable.ImmutableStack<System.Collections.Generic.KeyValuePair<Variable, object>>>;


public delegate Solutions Failure();
public delegate Solutions Success(Solution value, Failure n);
public delegate Solutions Ma(Success y, Failure n, Failure e);
public delegate Ma Mf(Solution value);


public class Variable {

  private int id; 
  private string name;
  
  public Variable(int id, string name) {
    this.id = id;
    this.name = name;
  }
  
  public override string? ToString() {
    return $"Variable({this.id}, {this.name})";
  }

}


public static class Combinators {

  private static IEnumerable<int> count() {
    for (int i = 0; true; i++) {
      yield return i;
    };
  }

  private static IEnumerator<int> counter = count().GetEnumerator();

  public static Variable var(string name) {
    counter.MoveNext();
    return new Variable(counter.Current, name);
  }

  public static Solutions success(Solution s, Failure n) {
    yield return s;
    foreach(Solution each in n()) {
      yield return each;
    };
  }

  public static Solutions failure(){
    yield break;
  }

  public static Ma bind(Ma ma, Mf mf) {
    Solutions bound(Success y, Failure n, Failure e) {
      Solutions on_success(Solution s, Failure m) {
        foreach (Solution each in mf(s)(y, m, e)) {
          yield return each;
        };
      }
      return ma(on_success, n, e);
    }
    return bound;
  }

  public static Ma unit(Solution s) {
  Solutions ma(Success y, Failure n, Failure e) {
      return y(s, n);
    }
    return ma;
  }

  public static Ma cut(Solution s) {
  Solutions ma(Success y, Failure n, Failure e) {
      return y(s, e);
    }
    return ma;
  }

  public static Ma fail(Solution s) {
    Solutions ma(Success y, Failure n, Failure e) {
      return n();
    }
    return ma;
  }

  public static Mf then(Mf mf, Mf mg) {
    Ma mh(Solution s) {
      return bind(mf(s), mg);
    }
    return mh;
  }

  public static Mf seq_from_iterable(IEnumerable<Mf> mfs) {
    return mfs.Aggregate<Mf, Mf>(unit, then);
  }

  public static Mf seq(params Mf[] mfs) {
    return seq_from_iterable(mfs);
  }

  public static Mf choice(Mf mf, Mf mg) {
    Ma mh(Solution s) {
      Solutions ma(Success y, Failure n, Failure e) {
        Solutions on_fail() {
          return mg(s)(y, n, e);
        }
        return mf(s)(y, on_fail, e);
      }
      return ma;
    }
    return mh;
  }

  public static Mf amb_from_iterable(IEnumerable<Mf> mfs) {
    Mf joined = mfs.Aggregate<Mf, Mf>(fail, choice);
    Ma mf(Solution s) {
      Solutions ma(Success y, Failure n, Failure e) {
        return joined(s)(y, n, n);
      }
      return ma;
    }
    return mf;
  }

  public static Mf amb(params Mf[] mfs) {
    return amb_from_iterable(mfs);
  }

  public static Mf no(Mf mf) {
    return amb(seq(mf, cut, fail), unit);
  }

  public static Solutions run(Mf mf, Solution s) {
    return mf(s)(success, failure, failure);
  }

  private static Solution trail(Solution stack, Variable v, object o) {
    return stack.Push(new Pair(v, o));
  }

  private static Mf _unify<T1, T2>(ValueTuple<T1, T2> two_tuple) {
    return two_tuple switch {
      (object e1, object e2) when e1 is object o1 && e2 is object o2 && o1 == o2 => unit,
      (object e1, object e2) when e1 is Variable v => ((s) => unit(trail(s, v, e2))),
      (object e1, object e2) when e2 is Variable v => ((s) => unit(trail(s, v, e1))),
      _ => fail,
    };
  }

  public static Mf unify(params ValueTuple<object, object>[] two_tuples) {
    return seq_from_iterable(from two_tuple in two_tuples select _unify(two_tuple));
  }

  public static IEnumerable<Deref> resolve(Mf goal) {
    foreach(Solution stack in run(goal, Solution.Empty)) {
      object deref(Variable v) {
        Pair result = stack.ToList().Find((pair) => v == pair.Key);
        if (result is object r) {
          return result.Value;
        } else {
          return v;
        }
      }
      yield return deref;
    };
  }
   
  public static Mf child(Variable a, Variable b) {
    return amb(
      unify((a, "jim"), (b, "bob")),
      unify((a, "joe"), (b, "bob")),
      unify((a, "ian"), (b, "jim"))
    );
  }

  public static Mf descendant(Variable a, Variable c) {
    Ma mf(Solution subst) {
      Variable b = var("b");
      return amb(
        child(a, c), 
        seq(child(a, b), descendant(b, c), cut)
      )(subst);
    }
    return mf;
  }

  public static void Main() {
    Variable x = var("x");
    Variable y = var("y");
    foreach (Deref deref in resolve(descendant(x, y))) {
      Console.WriteLine(deref(x) + " is the descendant of " + deref(y));
    };
  }

}
