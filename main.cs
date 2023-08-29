using yogic;
using static yogic.Yogic;

public static class Example {

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
