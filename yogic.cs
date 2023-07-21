// See https://aka.ms/new-console-template for more information

using System;
using System.Collections.Generic;
using System.Collections.Immutable;

using Solution = System.Collections.Immutable.ImmutableDictionary<Variable, object>;
using Solutions = System.Collections.Generic.IEnumerable<System.Collections.Immutable.ImmutableDictionary<Variable, object>>;


public delegate Solutions Failure();
public delegate Solutions Success(Solution value, Failure n);
public delegate Solutions Ma(Success y, Failure n, Failure e);
public delegate Ma Mf(Solution value);


public class Variable {

  private string name;
  
  public Variable(string name) {
    this.name = name;
  }
  
  public override string? ToString() {
    return $"Variable({this.name})";
  }

}


public static class Combinators {

  public static Variable var(string name) {
    return new Variable(name);
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
    Solutions mb(Success y, Failure n, Failure e) {
      Solutions on_success(Solution s, Failure m) {
        return mf(s)(y, m, e);
      }
      return ma(on_success, n, e);
    }
    return mb;
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

  private static Mf _unify(ValueTuple<object, object> vo) {
    (var v, var o) = vo;
    Ma unifier(Solution s) {
      return (deref(s, v), deref(s, o)) switch {
        (object o1, object o2) when o1 == o2 => unit(s),
        (Variable o1, object o2) => unit(s.Add(o1, o2)),
        (object o1, Variable o2) => unit(s.Add(o2, o1)),
        _ => fail(s),
      };
    }
    return unifier;
  }

  public static Mf unify(params ValueTuple<object, object>[] oos) {
    return seq_from_iterable(from oo in oos select _unify(oo));
  }

  private static object deref(Solution s, object o) {
    while (o is Variable && s.ContainsKey((Variable) o)) {
      o = s[(Variable)o];
    };
    return o;
  }

  public static Solutions resolve(Mf goal) {
    return run(goal, Solution.Empty);
  }
   
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
    Ma mf(Solution subst) {
      Variable b = var("b");
      return amb(
        child(a, c), 
        seq(child(a, b), descendant(b, c))
      )(subst);
    }
    return mf;
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
      unify((a, "fifi")),
      seq(unify((a, "fluffy")), cut),
      unify((a, "daisy"))
    );
  }

  public static Mf not_dog(Variable a) {
    return no(dog(a));
  }

  public static Mf mortal(Variable a) {
    Ma mf(Solution subst) {
      Variable b = var("b");
      return amb(
        human(a),
        dog(a),
        seq(descendant(a, b), mortal(b))
      )(subst);
    }
    return mf;
  }

  public static void Main() {
    Variable x = var("x");
    Variable y = var("y");
    foreach (Solution subst in resolve(descendant(x, y))) {
      Console.WriteLine(subst[x] + " is the descendant of " + subst[y]);
    };
    foreach (Solution subst in resolve(mortal(x))) {
      Console.WriteLine(subst[x] + " is mortal");
    };
  }

}
