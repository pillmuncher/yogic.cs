# yogic.cs
**An embedded DSL of monadic combinators for first-order logic programming in C#.**

It's called Yogic because logic programming is another step on the path to
enlightenment.

## **Key features:**

- **Horn Clauses as Composable Combinators**: Define facts and rules of
  first-order logic by simply composing combinator functions.

- **Unification, Substitution, and Logical Variables**: The substitution
  environment provides Variable bindings and is incrementally constructed
  during resolution through the Unification operation. It is returned for each
  successful resolution.

- **Backtracking and the Cut**: Internally, the code uses the Triple-Barrelled
  Continuation Monad for resolution, backtracking, and branch pruning via the
  `Cut` combinator.

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
            Unify((a, "jim"), (b, "bob")),          // jim is a child of bob.
            Unify((a, "joe"), (b, "bob")),          // joe is a child of bob.
            Unify((a, "ian"), (b, "jim")),          // ian is a child of jim.
            Unify((a, "fifi"), (b, "fluffy")),      // fifi is a child of fluffy.
            Unify((a, "fluffy"), (b, "daisy"))      // fluffy is a child of daisy.
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

We prove these by *modus ponens*:

```
A  ⟶  B            gi  ⟶  f(x1,...,xm)
A                  gi
⎯⎯⎯⎯⎯          ⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯
B                  f(x1,...,xm)
```

A function with head ``f(x1,...,xm)`` is proven by proving any of
``g1,...,gn`` recursively. When we reach a goal that has no body, there's
nothing left to prove. This process is called a *resolution*.

## **How to use it:**

Just write functions that take in Variables and other values like in the
example above, and return combinator functions of type ``Goal``, constructed
by composing your functions with the combinator functions provided by this
module, and start the resolution by giving an initial function, a so-called
*goal*, to `Resolve()` and iterate over the results, one for each way *goal*
can be proven. No result means a failed resolution, that is the function
cannot be proven in the universe described by the given set of
functions/predicates.

## **API:**

```csharp
Seq = IReadOnlyCollection<object>;
Pair = ValueTuple<object, object>;
```
- Miscellaneous type shortcuts.

```csharp
public delegate Result? Next()
```
- A function type that represents a backtracking operation.  

```csharp
public delegate Result? Emit(Subst subst, Next next)
```
- A function type that represents a successful resolution.  

```csharp
public delegate Result? Step(Emit succeed, Next backtrack, Next escape)
```
- A function type that represents a resolution step.  

```csharp
public delegate Step Goal(Subst subst)
```
- A function type that represents a resolvable logical statement.  

```csharp
public static Step Unit(Subst subst)
```
- Takes a substitution environment `subst` into a computation.  
  Succeeds once and then initiates backtracking.

```csharp
public static Step Cut(Subst subst)
```
- Takes a substitution environment `subst` into a computation.  
  Succeeds once, but instead of normal backtracking aborts the current
  computation and jumps to the previous choice point, effectively pruning the
  search space.

```csharp
public static Step Fail(Subst subst)
```
- Takes a substitution environment `subst` into a computation.  
  Never succeeds. Immediately initiates backtracking.

```csharp
public static Goal And(params Goal[] goals)
```
- Conjunction of multiple goals.  
  Takes a variable number of goals and returns a new goal that tries all of
  them in series. Fails if any goal fails.

```csharp
public static Goal Or(params Goal[] goals)
```
- A choice between multiple goals.  
  Takes a variable number of goals and returns a new goal that tries all of
  them in series. Fails only if all goals fail. This defines a *choice point*.

```csharp
public static Goal Not(Goal goal)
```
- Negates `goal`.  
  Fails if `goal` succeeds and vive versa.

```csharp
public static Goal Unify(object o1, object o2)
```
- Tries to unify two objects.  
  Fails if they aren't unifiable.

```csharp
public static Goal UnifyAll(IEnumerable<Pair> pairs)
public static Goal UnifyAll(params Pair[] pairs)
```
- Tries to unify pairs of objects.  
  Fails if any pair is not unifiable.

```csharp
public static Goal UnifyAny(Variable v, IEnumerable<object> objects)
public static Goal UnifyAny(Variable v, params object[] objects)
```
- Tries to unify a variable with any one of objects.  
  Fails if no object is unifiable.

```csharp
public class Variable
```
- Represents named logical variables.  

```csharp
public class SubstProxy
```
- A mapping representing the Variable bindings of a solution.  

```csharp
public static IEnumerable<SubstProxy> Resolve(Goal goal)
```
- Perform logical resolution of `goal`.  

## Links:

Horn Clauses:  
https://en.wikipedia.org/wiki/Horn_clause

Logical Resolution:  
http://web.cse.ohio-state.edu/~stiff.4/cse3521/logical-resolution.html

Unification:  
https://eli.thegreenplace.net/2018/unification/

Backtracking:  
https://en.wikipedia.org/wiki/Backtracking

## More Links:

Monoids:  
https://en.wikipedia.org/wiki/Monoid

Folding on Monoids:  
https://bartoszmilewski.com/2020/06/15/monoidal-catamorphisms/

Distributive Lattices:  
https://en.wikipedia.org/wiki/Distributive_lattice

Monads:  
https://en.wikipedia.org/wiki/Monad_(functional_programming)

Monads Explained in C# (again):  
https://mikhail.io/2018/07/monads-explained-in-csharp-again/

Discovering the Continuation Monad in C#:  
https://functionalprogramming.medium.com/deriving-continuation-monad-from-callbacks-23d74e8331d0

Continuations:  
https://en.wikipedia.org/wiki/Continuation

Continuations Made Simple and Illustrated:  
https://www.ps.uni-saarland.de/~duchier/python/continuations.html

The Discovery of Continuations:  
https://www.cs.ru.nl/~freek/courses/tt-2011/papers/cps/histcont.pdf

Tail Calls:  
https://en.wikipedia.org/wiki/Tail_call

On Recursion, Continuations and Trampolines:  
https://eli.thegreenplace.net/2017/on-recursion-continuations-and-trampolines/
