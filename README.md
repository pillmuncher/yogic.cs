# yogic.cs
**An embedded DSL of monadic combinators for first-order logic programming in C#.**

It's called Yogic because logic programming is another step on the path to
enlightenment.

## **Key Features:**


Yogic is a toolset designed to simplify logic programming tasks. It provides
an efficient way to express, query, and solve logical problems.

1. **Combinator Functions for Logic Statements**:

   Begin by thinking of your logical statements as combinations of facts and
   rules. These statements can be represented using combinator functions
   provided by the library. Each function encapsulates a specific logical
   operation.

2. **Resolution - Finding Solutions**:

   Define your logical goals using these combinator functions. A goal
   represents a statement or query you want to resolve. To find solutions to
   your goals, call the `Resolve` method on them. This method starts the
   resolution process.

3. **Unification and Substitution of Variables**:

   Unification is a fundamental operation in logic programming. It's a way to
   match and bind objects together. During the resolution process, the library
   handles unification for you. As goals are pursued and objects are matched,
   a substitution environment is constructed. This environment maps logical
   variables to their bindings or values.

4. **Backtracking for Multiple Paths**:

   Logic programming often involves exploring different possibilities. If a
   particular path or goal doesn't succeed, the library automatically
   backtracks to explore other alternatives. You don't need to manage this
   backtracking manually; it's handled seamlessly.

5. **Optimizing with the 'Cut' Combinator**:

   To optimize your logical queries, you can use the `Cut` combinator. It
   serves as a way to curtail backtracking at specific points in your logic.
   By strategically placing 'Cut' combinators, you can prune branches of the
   search tree that are no longer relevant. This can significantly improve the
   efficiency of your logic programs.

By combining these elements, you can express and solve complex logical
problems effectively. Many of the intricacies of Logic Programming are
abstracted away, allowing you to focus on defining your logic in a more
intuitive and structured manner.

## **An Example:**

We represent logical facts as functions that specify which individuals are
humans and dogs and define a `ChildOf(a, b)` relation such that `a` is the
child of `b`. Then we define rules that specify what a descendant and a mortal
being is. We then run queries that tell us which individuals are descendants
of whom and which individuals are both mortal and not a dog:
```csharp
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
            UnifyAll((a, "jim"), (b, "bob")),       // jim is a child of bob.
            UnifyAll((a, "joe"), (b, "bob")),       // joe is a child of bob.
            UnifyAll((a, "ian"), (b, "jim")),       // ian is a child of jim.
            UnifyAll((a, "fifi"), (b, "fluffy")),   // fifi is a child of fluffy.
            UnifyAll((a, "fluffy"), (b, "daisy"))   // fluffy is a child of daisy.
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

    public static void Main() {
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
```
**Result:**
```
jim is a descendant of bob.
joe is a descendant of bob.
ian is a descendant of jim.
fifi is a descendant of fluffy.
fluffy is a descendant of daisy.
ian is a descendant of bob.
fifi is a descendant of daisy.

socrates is mortal and no dog.
plato is mortal and no dog.
archimedes is mortal and no dog.
```
Note that `jim`, `bob`, `joe` and `ian` are not part of the result of the
second query because we didn't specify that they are human. Also note that the
third query doesn't produce any solutions. `Dog(x)` is only true if there
exists an `x` such that x is a dog. In Predicate Logic we would write
`∃x:dog(x)`, and when we negate that, we arrive at `-∃x:dog(x)`, which is
equivalent to `∀x:-dog(x)`, meaning that nothing is a dog. Since we defined a
predicate `Dog(_)` in our universe, that assertion is false.

## **How it works:**

We interpret a function ``f(x1,...,xm) { return or(g1,...,gn); }``
as a set of logical implications:

```
g1  ⟶  f(x1,...,xm)
...
gn  ⟶  f(x1,...,xm)
```

We call ``f(x1,...,xm)`` the *head* and each ``gi`` a *body*.

A function with head ``f(x1,...,xm)`` is proven by proving any of
``g1,...,gn`` recursively. When we reach a goal that has a head but no body,
there's nothing left to prove. This process is called a *resolution*.

## **How to use it:**

Just write functions that take in Variables and other values like in the
example above, and return combinator functions of type ``Goal``, constructed
by composing your functions with the combinator functions provided by this
module, and start the resolution by giving an initial function, a so-called
*goal*, to `Resolve()` and iterate over the results, one for each way *goal*
can be proven. No result means a failed resolution, that is the function
cannot be proven in the universe described by the given set of
functions/predicates.

## **Documentation:**

- [API Documentation](API.md)
- [Glossary](Glossary.md)

## Links:

### **Horn Clauses**:  
https://en.wikipedia.org/wiki/Horn_clause

### **Logical Resolution**:  
http://web.cse.ohio-state.edu/~stiff.4/cse3521/logical-resolution.html

### **Unification**:  
https://eli.thegreenplace.net/2018/unification/

### **Backtracking**:  
https://en.wikipedia.org/wiki/Backtracking

### **Monoids**:  
https://en.wikipedia.org/wiki/Monoid

### **Folding on Monoids**:  
https://bartoszmilewski.com/2020/06/15/monoidal-catamorphisms/

### **Distributive Lattices**:  
https://en.wikipedia.org/wiki/Distributive_lattice

### **Monads**:  
https://en.wikipedia.org/wiki/Monad_(functional_programming)

### **Monads Explained in C# (again)**:  
https://mikhail.io/2018/07/monads-explained-in-csharp-again/

### **Discovering the Continuation Monad in C#**:  
https://functionalprogramming.medium.com/deriving-continuation-monad-from-callbacks-23d74e8331d0

### **Continuations**:  
https://en.wikipedia.org/wiki/Continuation

### **Continuations Made Simple and Illustrated**:  
https://www.ps.uni-saarland.de/~duchier/python/continuations.html

### **The Discovery of Continuations**:  
https://www.cs.ru.nl/~freek/courses/tt-2011/papers/cps/histcont.pdf

### **Tail Calls**:  
https://en.wikipedia.org/wiki/Tail_call

### **On Recursion, Continuations and Trampolines**:  
https://eli.thegreenplace.net/2017/on-recursion-continuations-and-trampolines/
