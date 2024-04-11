# **GLOSSARY**

### **Algebraic Structure**:

* A collection of things, together with a set of operations and rules defined
  on it. Used to describe relationships between elements within that
  collection. Yogic's combinator functions form algebraic structures, in
  particular two [Monoids](#Monoid) and a [Distributive
  Lattice](#Distributive-Lattice).

### **Backtracking**:

* A technique employed in search and [Logic Programming](#Logic-Programming)
  to explore different possibilities of outcomes. If a certain path does not
  lead to a solution, the program can backtrack and try an alternative path.
  Backtracking can also be used to generate an exhaustive list of solutions
  for a logical query.

### **Branch Pruning with the Cut**:

* In [Logic Programming](#Logic-Programming), *cut* (or *!* in the programming
  language Prolog) is the name of an operator that succeeds once and then
  prevents backtracking beyond the point where it was invoked. This
  effectively prunes branches in a search tree.

### **Choice Point**:

* In [Logic Programming](#Logic-Programming), a Choice Point is a juncture
  where multiple options are available. It's used to explore different paths
  until a solution is found or all possibilities are exhausted.

### **Combinators**:

* Higher Order Functions that have no free variables and produce new functions
  as output. In Yogic, combinators are used to create and compose logical
  [Goals](#Goal).

### **Continuation**:

* A representation of the future of a computation. Continuations are used to
  manage the flow of logical [Resolution](#Resolution) and
  [Backtracking](#Backtracking). In Yogic, there are at any point three
  possible futures: One in which the current [Goal](#Goal) succeeds, one where
  it fails, and one where it succeeds once and after that curtails
  [Backtracking](#Backtracking) at the previous [Choice Point](#Choice-Point).

### **Continuation Monad**:

* A [Monad](#Monad) that encapsulates computations with
  [Continuations](#Continuation). Yogic employs the *Triple-Barrelled
  Continuation Monad* to manage [Resolution](#Resolution),
  [Backtracking](#Backtracking) and [Branch Pruning with the
  Cut](#Branch-Pruning-with-the-Cut).

### **Distributive Lattice**:

* A mathematical structure where two binary operations, *meet* (infimum) and
  *join* (supremum), satisfy certain distributive properties. In the code, the
  combinators `Then` and `Choice` together with their respective identity
  elements `Unit` and `Fail` form a Distributive Lattice.

### **Goal**:

* In [Logic Programming](#Logic-Programming), a [Goal](#Goal) represents a
  resolvable logical statement or query. It defines the tasks to be
  accomplished during the [Resolution](#Resolution) process.

### **Horn Clauses**:

* A specific form of logical formula where there is at most one non-negated
  term on the right side of an implication (=>). Horn clauses are commonly
  used in [Logic Programming](#Logic-Programming). In Yogic,
  [Combinator](#Combinators) functions are interpreted as Horn Clauses.

### **Logic Programming**:

* A programming paradigm that emphasizes using logic-based rules and
  statements to describe computation. It relies on a formal system of
  deductive reasoning and is used to derive conclusions.

### **Logical Variables**:

* Variables in [Logic Programming](#Logic-Programming) are automatically bound
  to values or other variables in the [Substitution
  Environment](#Substitution-Environment) during [Resolution](#Resolution).
  Like mathematical variables, they represent a value and cannot be re-asigned
  another.

### **Monad**:

* A concept from Category Theory and a design pattern in Functional Programming.
  Monads are used to manage side effects and computations in a structured way. In
  Yogic, the *Triple-Barrelled Continuation Monad* is used for managing logical
  computations.

### **Monoid**:

* A mathematical structure consisting of a Set and an associative binary
  operation with an identity element. In Yogic, the combinators `Then` and
  `Unit` form a Monoid, as do `Choice` and `Fail`.

### **Negation as Failure**:

* A technique in [Logic Programming](#Logic-Programming) where the absence of
  proof for a statement is treated as evidence of its negation.

### **Resolution**:

* A fundamental process in [Logic Programming](#Logic-Programming) and formal
  reasoning. It involves deriving new facts or conclusions from existing ones
  by applying logical rules and inference methods.

### **Substitution Environment**:

* A key data structure in [Logic Programming](#Logic-Programming) that maps
  [Logical Variables](#Logical-Variables) to values. These are called their
  *bindings*.

### **Tail Call Elimination**:

* A technique to optimize deeply chained function calls by avoiding the
  accumulation of stack frames.

### **Thunking**:

* Thunking is a technique where a function's execution is delayed until it is
  needed. This usually takes the form of a parameter-less function (a
  so-called *Thunk*) which can be passed around in a program and executed at
  a different point in the code from where it was created. Yogic uses it to
  implement [Tail Call Elimination](#Tail-Call-Elimination).

### **Trailing**:

* The process of tracking bound [Logical Variables](#Logical-Variables).

### **Trampolining**:

* A technique for handling function calls without using additional stack
  space. It turns these calls into a loop. It's a way to implement [Tail Call
  Elimination](#Tail-Call-Elimination) in a language that doesn't support it
  but has function objects, like C#.

### **Unification**:

* The process of finding [substitutions](#Substitution-Environment) for
  [Logical Variables](#Logical-Variables) such that two terms become equal.
  It's a fundamental operation in [Logic Programming](#Logic-Programming).
