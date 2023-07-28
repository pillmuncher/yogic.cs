# yogic.cs
Yogic, but in C#.


An embedded DSL for expressing first-order logical predicates and
performing resolution with backtracking and pruning of search paths.

Key features:

- Horn Clauses: Express logical facts and rules as simple functions.

- Combinators: Compose expressions of first-order logic by simply
  composing combinator functions.

- Logical Variables: Represented by the Variable class, they can be
  bound to arbitrary values and other variables during resolution.

- Substitution and Unification: The substitution environment provides
  variable bindings and is incrementally constructed during resolution.

- Backtracking: The monad combines the List and the Triple-Barrelled
  Continuation Monads for resolution, backtracking, and branch pruning.

- Algebraic Structures: 'unit' and 'then' form a monoid over monadic
  combinator functions, as do 'fail' and 'choice'. Together they form a
  Distributive Lattice with 'then' as the meet (infimum) and 'choice' as
  the join (supremum) operator, and 'unit' and 'fail' as their
  respective identity elements. Because of the sequential nature of the
  employed resolution algorithm, the lattice is non-commutative.

How it works:

We interpret a function f(x1,...,xm) { return or(p1,...,pn); } 
as a set of logical implications:

p1 -> f(x1,...,xm)
...
pn -> f(x1,...,xm)

The equivalen Prolog looks like this:

f(x1,...,xn) :- p1.
...
f(x1,...,xn) :- pn.

We prove these by modus ponens:

  A -> B
  A
 --------
  B

We call f(x1,...,xn) the head and each px a body.

A The function with head f(x1,...,xm) is proven by proving any of
p1,...,pm recursively. When we reach a success goal that has no body,
there's nothing left to prove. This is called a resolution.

How to use it:

Just write a function that takes in Variables and returns a monadic
function of type Mf, constructed by combining the functions provided
below, and start the resolution by giving the function to resolve()
and iterate over the results, because there can be more ways to
prove. No result means a failed resolution, that is the function
cannot be proven in the universe described by the given set of
functions/predicates.

There are some examples at the end.

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


