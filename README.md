# yogic.cs
**Yogic, but in C#.**


An embedded DSL for expressing first-order logical predicates and
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

`public static Variable var(string name)`  
Creates a new logical variable with the given name.  

`public static Solutions success(Subst subst, Retry retry)`  
Represents a successful resolution.  
Takes a substitution environment and a retry continuation.
First yields the substitution environment once and then invokes
backtracking by delegating to the provided retry continuation.  

`public static Solutions failure()`  
Represents a failed resolution.  

`public static Ma bind(Ma ma, Mf mf)`  
Applies the monadic computation mf to ma.  

`public static Ma unit(Subst subst)`  
Lifts a substitution environment into a computation.  

`public static Ma cut(Subst subst)`  
Succeeds once, and on backtracking aborts the current computation,
effectively pruning the search space.  

`public static Ma fail(Subst subst)`  
Represents a failed computation. Immediately initiates backtracking.  

`public static Mf then(Mf mf, Mf mg)`  
Composes two computations sequentially.  

`public static Mf and_from_enumerable(IEnumerable<Mf> mfs)`  
Composes multiple computations sequentially from an enumerable.  

`public static Mf and(params Mf[] mfs)`  
Composes multiple computations sequentially.  

`public static Mf choice(Mf mf, Mf mg)`  
Represents a choice between two computations.  
Takes two computations mf and mg and returns a new computation that
tries mf, and if that fails, falls back to mg.  

`public static Mf or_from_enumerable(IEnumerable<Mf> mfs)`  
Represents a choice between multiple computations from an enumerable.  
Takes a collection of computations mfs and returns a new computation
that tries all of them in series with backtracking.  

`public static Mf or(params Mf[] mfs)`  
Represents a choice between multiple computations.  
Takes a variable number of computations and returns a new computation
that tries all of them in series with backtracking.  

`public static Mf not(Mf mf)`  
Negates the result of a computation.  
Returns a new computation that succeeds if mf fails and vice versa.  

`public static Mf unify(params ValueTuple<object, object>[] pairs)`  
Tries to unify pairs of objects. Fails if any pair is not unifiable.  

`public static Solutions resolve(Mf goal)`  
Perform logical resolution of the computation represented by goal.  

**An Example:**  

```
  public static Mf human(Variable a) {
    return or(
      unify((a, "socrates")),
      unify((a, "plato")),
      unify((a, "archimedes"))
    );
  }

  public static Mf dog(Variable a) {
    return or(
      unify((a, "fluffy")),
      unify((a, "daisy")),
      unify((a, "fifi"))
    );
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
```

Links:

Unification:
https://eli.thegreenplace.net/2018/unification/

Logical Resolution:
http://web.cse.ohio-state.edu/~stiff.4/cse3521/logical-resolution.html

Horn Clauses:
https://en.wikipedia.org/wiki/Horn_clause

Continuations Made Simple and Illustrated:
https://www.ps.uni-saarland.de/~duchier/python/continuations.html

Monads Explained in C# (again):
https://mikhail.io/2018/07/monads-explained-in-csharp-again/

Discovering the Continuation Monad in C#:
https://functionalprogramming.medium.com/deriving-continuation-monad-from-callbacks-23d74e8331d0

The Discovery of Continuations:
https://www.cs.ru.nl/~freek/courses/tt-2011/papers/cps/histcont.pdf

Monoid:
https://en.wikipedia.org/wiki/Monoid

Distributive Lattice:
https://en.wikipedia.org/wiki/Distributive_lattice
