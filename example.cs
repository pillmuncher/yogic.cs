using yogic;
using static yogic.Yogic;

public static class Example {

  public static Mf human(Variable a) {      //  socrates, plato, and archimedes are human
    return unify_any(a, "socrates", "plato", "archimedes");
  }

  public static Mf dog(Variable a) {        // fluffy, daisy, and fifi are dogs
    return unify_any(a, "fluffy", "daisy", "fifi");
  }

  public static Mf child(Variable a, Variable b) {
    return or(
      unify((a, "jim"), (b, "bob")),        // jim is a child of bob.
      unify((a, "joe"), (b, "bob")),        // joe is a child of bob.
      unify((a, "ian"), (b, "jim")),        // ian is a child of jim.
      unify((a, "fifi"), (b, "fluffy")),    // fifi is a child of fluffy.
      unify((a, "fluffy"), (b, "daisy"))    // fluffy is a child of daisy.
    );
  }

  public static Mf descendant(Variable a, Variable c) {
    var b = new Variable("b");
    // by returning a lambda function we
    // create another level of indirection,
    // so that the recursion doesn't
    // immediately trigger an infinite loop
    // and cause a stack overflow:
    return (subst) => or(                   // a is a descendant of c iff:
      child(a, c),                          // a is a child of c, or
      and(child(a, b), descendant(b, c))    // a is a child of b and b is b descendant of c.
    )(subst);
  }

  public static Mf mortal(Variable a) {
    var b = new Variable("b");
    return (subst) => or(                   // a is mortal iff:
      human(a),                             // a is human, or
      dog(a),                               // a is a dog, or
      and(descendant(a, b), mortal(b))      // a descends from a mortal.
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
