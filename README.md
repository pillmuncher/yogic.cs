# yogic.cs
**An embedded DSL of monadic combinators for first-order logic programming in C#.**

It's called Yogic because logic programming is another step on the path to
enlightenment.

## **Key features:**

- **Horn Clauses as Functions**: Express logical facts and rules as simple
  functions.

- **Composable Combinators**: Define expressions of first-order logic by
  simply composing combinator functions.

- **Logical Variables**: Represented by the ``Variable`` class, they can be
  bound to arbitrary values including other variables during resolution.

- **Substitution and Unification**: The substitution environment provides
  variable bindings and is incrementally constructed during resolution. It
  is returned for each successful resolution.

- **Backtracking**: The monad combines the List and the Triple-Barrelled
  Continuation Monads for resolution, backtracking, and branch pruning via
  the ``cut`` combinator.

- **Algebraic Structures**: ``unit`` and ``then`` form a *Monoid* over
  monadic combinator functions, as do ``fail`` and ``choice``. Together they
  form a *Distributive Lattice* with ``then`` as the *meet* (infimum) and
  ``choice`` as the *join* (supremum) operator, and ``unit`` and ``fail`` as
  their respective identity elements. Because of the sequential nature of
  the employed resolution algorithm combined with the `cut`, the lattice is
  *non-commutative*.

## **A Motivating Example:**

We represent logical facts as functions that specify which individuals are
humans and dogs and define a `child(a, b)` relation such that `a` is the child
of `b`. Then we define rules that specify what a descendant and a mortal being
is. We then run queries that tell us which individuals are descendants of whom
and which individuals are both mortal and no dogs:
```csharp
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
      and(child(a, b), descendant(b, c))    // a is a child of b and b is a descendant of c.
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
second query because we didn't specify that they are human. Also note that
the third query doesn't produce any solutions, because in the clause 
`not(dog(x))` the variable `x` isn't bound yet, and unbound variables are
implicitely ∀-quantified and we're saying that nothing is a dog, which in the
universe we defined is not true.

## **How it works:**

We interpret a function ``f(x1,...,xm) { return or(g1,...,gn); }``
as a set of logical implications:

```
g1  ⟶  f(x1,...,xm)
...
gn  ⟶  f(x1,...,xm)
```

We call ``f(x1,...,xn)`` the *head* and each ``gi`` a *body*.

We prove these by *modus ponens*:

```
A  ⟶  B            gi  ⟶  f(x1,...,xn)
A                  gi
⎯⎯⎯⎯⎯          ⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯⎯
B                  f(x1,...,xn)
```

A function with head ``f(x1,...,xm)`` is proven by proving any of
``g1,...gn`` recursively. When we reach a success goal that has no body,
there's nothing left to prove. This process is called a *resolution*.

## **How to use it:**

Just write functions that take in Variables and other values like in the
example above, and return monadic functions of type ``Mf``, constructed by
composing your functions with the combinator functions provided by this
module, and start the resolution by giving an initial function, a so-called
*goal*, to ``resolve()`` and iterate over the results, one for each way *goal*
can be proven. No result means a failed resolution, that is the function
cannot be proven in the universe described by the given set of
functions/predicates.

## **API:**

```csharp
public delegate Result Emit(Subst subst, Retry retry)
```
- A function type that represents a successful resolution.  `Emit`
  continuations are called with a substitution environment `subst` and a
  `Retry` continuation `retry` and yield the provided substitution
  environment once and then yield whatever `retry()` yields.

```csharp
public delegate Result? Retry()
```
- A function type that represents a backtracking operation.  

```csharp
public delegate Result? Ma(Emit yes, Retry no, Retry esc)
```
- The monadic computation type.  
  Combinators of this type take a `Emit` continuation and two `Retry`
  continuations. `yes` represents the current continuation and `no` represents
  the backtracking path. `esc` is the escape continuation that is invoked by
  the `cut` combinator to jump out of the current comptutation back to the
  previous choice point. 

```csharp
public delegate Ma Mf(Subst subst)
```
- The monadic function type.  
  Combinators of this type take a substitution environment `subst` and
  return a monadic object.

```csharp
public static Ma bind(Ma ma, Mf mf)
```
- Applies the monadic continuation `mf` to `ma` and returns the result.  
  In the context of the backtracking monad this means turning `mf` into the
  continuation of `ma`.

```csharp
public static Ma unit(Subst subst)
```
- Takes a substitution environment `subst` into a monadic computation.  
  Succeeds once and then initates backtracking.

```csharp
public static Ma cut(Subst subst)
```
- Takes a substitution environment `subst` into a monadic computation.  
  Succeeds once, and instead of normal backtracking aborts the current
  computation and jumps to the previous choice point, effectively pruning the
  search space.

```csharp
public static Ma fail(Subst subst)
```
- Takes a substitution environment `subst` into a monadic computation.  
  Never succeeds. Immediately initiates backtracking.

```csharp
public static Mf then(Mf mf, Mf mg)
```
- Composes two continuations sequentially.

```csharp
public static Mf and(params Mf[] mfs)
```
- Composes multiple continuations sequentially.

```csharp
public static Mf and_from_enumerable(IEnumerable<Mf> mfs)
```
- Composes multiple continuations sequentially from an enumerable.

```csharp
public static Mf choice(Mf mf, Mf mg)
```
- Represents a choice between two monadic continuations.  
  Takes two continuations `mf` and `mg` and returns a new continuation that
  tries `mf`, and if that fails, falls back to `mg`. This defines a *choice
  point*.

```csharp
public static Mf or(params Mf[] mfs)
```
- Represents a choice between multiple monadic continuations.  
  Takes a variable number of continuations and returns a new continuation
  that tries all of them in series with backtracking. This defines a
  *choice point*.

```csharp
public static Mf or_from_enumerable(IEnumerable<Mf> mfs)
```
- Represents a choice between multiple monadic continuations from an enumerable.  
  Takes a sequence of continuations `mfs` and returns a new continuation that
  tries all of them in series with backtracking. This defines a *choice point*.

```csharp
public static Mf not(Mf mf)
```
- Negates the result of a continuation.  
  Returns a new continuation that succeeds if `mf` fails and vice versa.

```csharp
public static Mf unify(params ValueTuple<object, object>[] pairs)
```
- Tries to unify pairs of objects. Fails if any pair is not unifiable.

```csharp
  public static Mf unify_any(Variable v, params object[] objects) =>
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
public static IEnumerable<SubstProxy> resolve(Mf goal)
```
- Perform logical resolution of the monadic continuation represented by `goal`.

## Links:

Unification:  
https://eli.thegreenplace.net/2018/unification/

Backtracking:  
https://en.wikipedia.org/wiki/Backtracking

Logical Resolution:  
http://web.cse.ohio-state.edu/~stiff.4/cse3521/logical-resolution.html

Horn Clauses:  
https://en.wikipedia.org/wiki/Horn_clause

Monoids:  
https://en.wikipedia.org/wiki/Monoid

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
