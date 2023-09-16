// Copyright (c) 2023 Mick Krippendorf <m.krippendorf@freenet.de>

using Yogic;
using static Yogic.Combinators;

public static class Example {

    public static Goal Human(Variable a) {          // socrates, plato, and archimedes are human
        return UnifyAny(a, "socrates", "plato", "archimedes");
    }

    public static Goal Dog(Variable a) {            // fluffy, daisy, and fifi are dogs
        return UnifyAny(a, "fluffy", "daisy", "fifi");
    }

    public static Goal ChildOf(Variable a, Variable b) {
        return Or(
            UnifyAll((a, "jim"), (b, "bob")),          // jim is a child of bob.
            UnifyAll((a, "joe"), (b, "bob")),          // joe is a child of bob.
            UnifyAll((a, "ian"), (b, "jim")),          // ian is a child of jim.
            UnifyAll((a, "fifi"), (b, "fluffy")),      // fifi is a child of fluffy.
            UnifyAll((a, "fluffy"), (b, "daisy"))      // fluffy is a child of daisy.
        );
    }

    public static Goal DescendantOf(Variable a, Variable c) {
        var b = new Variable("b");
        // by returning a lambda function we
        // create another level of indirection,
        // so that the recursion doesn't
        // immediately trigger an infinite loop
        // and cause a stack overflow:
        return (subst) => Or(                       // a is a descendant of c iff:
            ChildOf(a, c),                          // a is a child of c, or
            And(ChildOf(a, b), DescendantOf(b, c))  // a is a child of b and b is a descendant of c.
        )(subst);
    }

    public static Goal Mortal(Variable a) {
        var b = new Variable("b");
        return (subst) => Or(                       // a is mortal iff:
            Human(a),                               // a is human, or
            Dog(a),                                 // a is a dog, or
            And(DescendantOf(a, b), Mortal(b))      // a descends from a mortal.
        )(subst);
    }

    public static void Main1() {
        var x = new Variable("x");
        var y = new Variable("y");
        foreach (var subst in Resolve(DescendantOf(x, y))) {
            Console.WriteLine($"{subst[x]} is a descendant of {subst[y]}.");
        };
        Console.WriteLine();
        foreach (var subst in Resolve(And(Mortal(x), Not(Dog(x))))) {
            Console.WriteLine($"{subst[x]} is mortal and no dog.");
        };
        Console.WriteLine();
        foreach (var subst in Resolve(And(Not(Dog(x)), Mortal(x)))) {
            Console.WriteLine($"{subst[x]} is mortal and no dog.");
        };
    }

}
