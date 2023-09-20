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

A classic example of Logic Programming is puzzle solving. Here we have a
puzzle where numbers have to be assigned to letters, such that all sets of
assignments are compatible with each other. Typically, there is ever only a
single solution. We solve this in a naive way, by generating all possible
permutations and then matching each permutation until we find one that
matches. In Logic Programming this kind of algorithm is known as *Generate and
Test*.
```csharp
using System.Collections.Generic;
using System.Linq;

using MoreLinq;

namespace Yogic.Puzzle;

using static Yogic.Combinators;

public static class Puzzle
{
    private static Goal Solver(ValueTuple<List<Variable>, List<object>>[] pairs)
    {
        return And(
            from pair in pairs
            select Or(
                from permutation in pair.Item2.Permutations()
                select Unify(pair.Item1, permutation)
            )
        );
    }

    public static void Main()
    {
        var a = new Variable("a");
        var b = new Variable("b");
        var c = new Variable("c");
        var d = new Variable("d");
        var e = new Variable("e");
        var f = new Variable("f");
        var g = new Variable("g");
        var h = new Variable("h");
        var i = new Variable("i");
        var j = new Variable("j");
        var k = new Variable("k");
        var l = new Variable("l");

        var puzzle = new ValueTuple<List<Variable>, List<object>>[]
        {
            (new() { a, b, c, e, h, i, j }, new() { 2, 4, 5, 8, 10, 11, 12 }),
            (new() { a, b, f, i, j, k, l }, new() { 1, 4, 5, 6, 7, 8, 12 }),
            (new() { a, c, d, e, f, k, l }, new() { 1, 2, 6, 7, 8, 9, 10 }),
            (new() { a, c, f, g, i, j, k }, new() { 1, 2, 3, 4, 6, 8, 12 }),
            (new() { b, c, d, e, f, g, h }, new() { 1, 2, 3, 5, 9, 10, 11 }),
            (new() { b, c, e, g, h, j, l }, new() { 2, 3, 4, 5, 7, 10, 11 }),
        };

        foreach (var subst in Resolve(Solver(puzzle)))
        {
            Console.WriteLine($"a = {subst[a]}");
            Console.WriteLine($"b = {subst[b]}");
            Console.WriteLine($"c = {subst[c]}");
            Console.WriteLine($"d = {subst[d]}");
            Console.WriteLine($"e = {subst[e]}");
            Console.WriteLine($"f = {subst[f]}");
            Console.WriteLine($"g = {subst[g]}");
            Console.WriteLine($"h = {subst[h]}");
            Console.WriteLine($"i = {subst[i]}");
            Console.WriteLine($"j = {subst[j]}");
            Console.WriteLine($"k = {subst[k]}");
            Console.WriteLine($"l = {subst[l]}");
            Console.WriteLine();
        }
    }
}
```
**Result:**
```
a = 8
b = 5
c = 2
d = 9
e = 10
f = 1
g = 3
h = 11
i = 12
j = 4
k = 6
l = 7
```

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
