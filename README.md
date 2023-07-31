# yogic.cs
**Yogic, but in C#.**


An embedded DSL of monadic combinators for first-order logical predicates and
performing resolution with backtracking and pruning of search paths.

**Key features:**

- **Horn Clauses**: Express logical facts and rules as simple functions.

- **Combinators**: Compose expressions of first-order logic by simply
  composing combinator functions.

- **Logical Variables**: Represented by the ``Variable`` class, they can be
  bound to arbitrary values and other variables during resolution.

- **Substitution and Unification**: The substitution environment provides
  variable bindings and is incrementally constructed during resolution.
  It is returned for each successful resolution.

- **Backtracking**: The monad combines the List and the Triple-Barrelled
  Continuation Monads for resolution, backtracking, and branch pruning
  via the ``cut`` combinator.

- **Algebraic Structures**: ``unit`` and ``then`` form a *monoid* over monadic
  combinator functions, as do ``fail`` and ``choice``. Together they form a
  *Distributive Lattice* with ``then`` as the *meet* (infimum) and ``choice`` as
  the *join* (supremum) operator, and ``unit`` and ``fail`` as their
  respective identity elements. Because of the sequential nature of the
  employed resolution algorithm, the lattice is non-commutative.

**How it works:**

We interpret a function ``f(x1,...,xm) { return or(p1,...,pn); }``
as a set of logical implications:

```
p1  ⟶  f(x1,...,xm)
...
pn  ⟶  f(x1,...,xm)
```  

We call ``f(x1,...,xn)`` the *head* and each ``px`` a *body*.

We prove these by *modus ponens*:

```
A  ⟶  B
A
⎯⎯⎯⎯⎯
B
```

A function with head ``f(x1,...,xm)`` is proven by proving any of
``p1,...,pn`` recursively. When we reach a success goal that has no body,
there's nothing left to prove. This is called a *resolution*.

**How to use it:**

Just write functions that take in Variables and other values, and return
monadic functions of type ``Mf``, constructed by combining the functions
provided by this module, and start the resolution by giving an initial
function, a so-called *goal*, to ``resolve()`` and iterate over the results,
one for each way *goal* can be proven. No result means a failed resolution,
that is the function cannot be proven in the universe described by the given
set of functions/predicates.

**API:**

```csharp
public delegate Solutions Success(Subst subst, Failure backtrack)
```
- A function type that represents a successful resolution.  
  `Success` continuations are called with a substitution environment `subst`
  and a `Failure` continuation `backtrack` and yield the provided substitution
  environment once and then yield whatever `backtrack()` yields.  
  
```csharp
public delegate Solutions Failure()
```
- A function type that represents a failed resolution.  
  `Failure` continuations are called to initiate backtracking.  
  
```csharp
public delegate Solutions Ma(Success yes, Failure no, Failure esc)
```
- The monad type.  
  Takes a `Success` continuation and two `Failure` continuations.  
  
```csharp
public delegate Ma Mf(Subst subst)
```
- The monadic function type.  
  Takes a substitution environment `subst` and returns a monadic object.  
  
```csharp
public static Ma bind(Ma ma, Mf mf)
```
- Applies the monadic computation `mf` to `ma` and returns the result.  
  
```csharp
public static Ma unit(Subst subst)
```
- Lifts a substitution environment `subst` into a computation.  
  Always succeeds.  
  
```csharp
public static Ma cut(Subst subst)
```
- Lifts a substitution environment `subst` into a computation.  
  Succeeds once, and on backtracking aborts the current computation and jumps
  to the previous choice point, effectively pruning the search space.  
  
```csharp
public static Ma fail(Subst subst)
```
- Lifts a substitution environment `subst` into a computation.  
  Never succeeds. Immediately initiates backtracking.  
  
```csharp
public static Mf then(Mf mf, Mf mg)
```
- Composes two computations sequentially.  
  
```csharp
public static Mf and(params Mf[] mfs)
```
- Composes multiple computations sequentially.  
  
```csharp
public static Mf and_from_enumerable(IEnumerable<Mf> mfs)
```
- Composes multiple computations sequentially from an enumerable.  
  
```csharp
public static Mf choice(Mf mf, Mf mg)
```
- Represents a choice between two computations.  
  Takes two computations `mf` and `mg` and returns a new computation that tries
  `m`f, and if that fails, falls back to `mg`. This defines a *choice point*.  
  
```csharp
public static Mf or(params Mf[] mfs)
```
- Represents a choice between multiple computations.  
  Takes a variable number of computations and returns a new computation that
  tries all of them in series with backtracking.  
  
```csharp
public static Mf or_from_enumerable(IEnumerable<Mf> mfs)
```
- Represents a choice between multiple computations from an enumerable.  
  Takes a sequence of computations `mfs` and returns a new computation that
  tries all of them in series with backtracking.  
  
```csharp
public static Mf not(Mf mf)
```
- Negates the result of a computation.  
  Returns a new computation that succeeds if `mf` fails and vice versa.  
  
```csharp
public static Mf unify(params ValueTuple<object, object>[] pairs)
```
- Tries to unify pairs of objects. Fails if any pair is not unifiable.  
  
```csharp
public static Solutions resolve(Mf goal)
```
- Perform logical resolution of the computation represented by `goal`.  
  
```csharp
public class Variable
```
- Represents named logical variables.  
  
```csharp
public static Variable var(string name)
```
- Convenience function. Creates a new logical variable with the given name.  
  
**An Example:**  

```csharp
  public static Mf human(Variable a) {
    return or(
      unify((a, "socrates")),               // socrates is human.
      unify((a, "plato")),                  // plato is human.
      unify((a, "archimedes"))              // archimedes is human.
    );
  }

  public static Mf dog(Variable a) {
    return or(
      unify((a, "fluffy")),                 // fluffy is a dog.
      unify((a, "daisy")),                  // daisy is a dog.
      unify((a, "fifi"))                    // fifi is a dog.
    );
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
    return (subst) => or(                   // a is a descendant of c if:
      child(a, c),                          // a is a child of c, or:
      and(child(a, b), descendant(b, c))    // a is a child of b and b is b descendant of c.
    )(subst);
  }

  public static Mf mortal(Variable a) {
    var b = new Variable("b");
    return (subst) => or(                   // a is mortal if:
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
  }
```
Result:
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

Links:

Unification:  
https://eli.thegreenplace.net/2018/unification/
  
Backtracking:  
https://en.wikipedia.org/wiki/Backtracking
  
Logical Resolution:  
http://web.cse.ohio-state.edu/~stiff.4/cse3521/logical-resolution.html
  
Horn Clauses:  
https://en.wikipedia.org/wiki/Horn_clause
  
Monoid:  
https://en.wikipedia.org/wiki/Monoid
  
Distributive Lattice:  
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
