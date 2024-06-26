# yogic.cs

**An embedded DSL of monadic combinators for first-order logic programming in C#.**

It's called Yogic because logic programming is another step on the path to
enlightenment.

## **Key Features:**

Yogic is a toolset designed to enable logic programming in C#. It provides
an efficient way to express, query, and solve logical problems.

1. **Logical Statements**:

   Logical statements are usually expressed as combinations of facts and rules.
   These statements are represented by combining, well, combinator functions
   provided by the library. Each function encapsulates a specific logical
   operation, such as logical negation, conjunction, disjunction, and so on.

2. **Logical Queries**:

   Define your logical queries using these combinator functions. A query is
   a statement or goal you want to resolve. To find solutions to your queries,
   call the `Resolve` function on them to start the resolution process.

3. **Unification and Substitution of Variables**:

   Unification is a fundamental operation in logic programming. It's a way to
   bind objects to logical variables and match objects with one another. During
   the resolution process, the library handles unification for you. As goals are
   pursued and objects are matched, a substitution environment is constructed.
   This environment map of logical variables to objects can be queried after
   a solution is found.

4. **Backtracking for Multiple Paths**:

   Logic programming often involves exploring different paths that might not all
   lead to a solution. If a particular path or goal doesn't succeed,
   backtracking is initiated automatically to explore other alternatives or to
   find all solutions for a goal. You don't need to specify or manage this
   backtracking manually since it's handled automatically.

5. **Optimizing with the 'Cut' Combinator**:

   To optimize your logical queries, you can use the `Cut` combinator. It
   serves as a way to curtail backtracking at specific points in your logic.
   By strategically placing 'Cut' combinators, you can prune branches of the
   search tree that are no longer relevant. This can significantly improve the
   efficiency of your logic programs. It can also be used as an extra-logical
   operator to disable the search for other solutions than the current one.

By combining these elements, you can express and solve complex logical problems
effectively. Many of the intricacies of Logic Programming are abstracted away,
allowing you to focus on defining your logic in a more intuitive and structured
manner.

## **How to use it:**

Just write functions that take Variables and other values as arguments, like in
the example below, and return combinator functions of type `Goal`, constructed
by composing your functions with the combinators provided by this module, and
start the resolution by giving an initial function, a so-called *query*, to
`Resolve()` and iterate over the results, one for each way the query can be
solved.  No result means a failed resolution, that is the function cannot be
proven in the universe described by the given set of functions/predicates.

## **How it works:**

We interpret a function `f(x1,...,xm) { return Or(g1,...,gn); }`
as a set of logical implications:

```
g1  ⟶  f(x1,...,xm)
...
gn  ⟶  f(x1,...,xm)
```

We call `f(x1,...,xm)` the *head* and each `gi` a *body*.

A function with head `f(x1,...,xm)` is proven by proving any of `g1,...,gn`.
When we reach a goal that has a head but no body, there's nothing left to prove.
This process is called a *resolution*.

## **An Example:**

A classic example of Logic Programming is puzzle solving. Here we have a puzzle
where numbers have to be assigned to letters, such that all sets of assignments
are compatible with each other. Typically, there is ever only a single solution.
We solve this in a combined way, by first procedurally generating a set of
candidate letters for each number and then declaratively specifying how
a solution should look like. We leave it to the `Resolve()` function to compute
the actual solution.

```csharp
using System.Linq;
using System.Collections.Generic;

using static Yogic.Combinators;

namespace Yogic.Puzzle;

using PuzzleDefinition = (HashSet<Variable> variables, HashSet<int> numbers);
using Candidates = Dictionary<object, HashSet<Variable>>;

public static class Puzzle
{
    private static Candidates Simplify(PuzzleDefinition[] puzzle)
    {
        var candidates = new Candidates();
        foreach (var (variables, numbers) in puzzle)
            foreach (var number in numbers)
                if (candidates.ContainsKey(number))
                    candidates[number].IntersectWith(variables);
                else
                    candidates[number] = new HashSet<Variable>(variables);
        return candidates;
    }

    private static Goal Solver(PuzzleDefinition[] puzzle)
    {
        return And(
            from pair in Simplify(puzzle)
            select Or(from variable in pair.Value select Unify(variable, pair.Key))
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

        var puzzle = new PuzzleDefinition[]
        {
            ([ a, b, c, e, h, i, j ], [ 2, 4, 5, 8, 10, 11, 12 ]),
            ([ a, b, f, i, j, k, l ], [ 1, 4, 5, 6, 7, 8, 12 ]),
            ([ a, c, d, e, f, k, l ], [ 1, 2, 6, 7, 8, 9, 10 ]),
            ([ a, c, f, g, i, j, k ], [ 1, 2, 3, 4, 6, 8, 12 ]),
            ([ b, c, d, e, f, g, h ], [ 1, 2, 3, 5, 9, 10, 11 ]),
            ([ b, c, e, g, h, j, l ], [ 2, 3, 4, 5, 7, 10, 11 ]),
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

## **Documentation:**

* [API Documentation](API.md)
* [Glossary](Glossary.md)

## Links:

### **Horn Clauses**:

https://en.wikipedia.org/wiki/Horn\_clause

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

https://en.wikipedia.org/wiki/Distributive\_lattice

### **Monads**:

https://en.wikipedia.org/wiki/Monad\_(functional\_programming)

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

https://en.wikipedia.org/wiki/Tail\_call

### **On Recursion, Continuations and Trampolines**:

https://eli.thegreenplace.net/2017/on-recursion-continuations-and-trampolines/
