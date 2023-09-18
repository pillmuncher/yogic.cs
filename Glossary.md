**GLOSSARY**:

**Algebraic Structure**:  
A set of operations and rules defined on a collection of things, used to
describe relationships between elements within that collection. Yogic's
combinator functions form algebraic structures, in particular Monoids and
also a Distributive Lattice.

**Backtracking**:  
A technique employed in search and logic programming to explore multiple
possibilities. If a certain path does not lead to a solution, the program can
backtrack and try an alternative path.

**Combinators**:  
Functions that take other functions as input and produce new functions as
output. In the context of logic programming, combinators are used to create
and manipulate logical goals.

**Continuation**:  
A representation of the future of a computation. Continuations are used to
manage the flow of logical resolution and backtracking.

**Continuation Monad**:  
A monad that encapsulates computations with continuations. 
See also the Triple-Barrelled Continuation Monad.

**Cut / Branch Pruning**:  
In the context of Logic programming, "cut" is the name of a combinator that
succeeds once and then prevents backtracking beyond the point where it was
invoked. This effectively prunes or cuts off branches in the search space.

**Distributive Lattice**:  
A mathematical structure where two binary operations, meet (infimum) and join
(supremum), satisfy certain distributive properties. In the code, combinators
like `Then` and `Choice` together with their respective identity elements
`Unit` and `Fail` form a distributive lattice.

**Goal**:  
In logic programming, a "Goal" represents a resolvable logical statement or
query. It defines the tasks to be accomplished during the resolution process.

**Horn Clauses**:  
A specific form of logical formula where there is at most one non-negated term
on the right side of an implication (=>). Horn clauses are commonly used in
logic programming.

**Logic Programming**:  
A programming paradigm that emphasizes using logic-based rules and statements
to describe computation. It relies on a formal system of deductive reasoning
and is used to derive conclusions.

**Logical Resolution**:  
A fundamental process in Logic Programming and formal reasoning. It involves
deriving new facts or conclusions from existing ones by applying logical rules
and inference methods, such as the Modus Ponens.

**Logical Variables**:  
Variables in logic programming that can be bound to values or other variables
in the substitution environment.

**Modus Ponens**:  
A common rule of inference in logic. If you have a premise in the form "if A
then B," and you know that A is true, you can infer that B is also true.

**Monad**:  
A design pattern and mathematical concept used for managing side effects and
computations in a structured way. In Yogic, the Triple-Barrelled Continuation
Monad is used for managing logical computations.

**Monoid**:  
A mathematical structure consisting of a Set and an associative binary
operation with an identity element. In Yogic, the combinators `Then` and
`Unit` form a Monoid, as do `Choice` and `Fail`.

**Negation as Failure**:  
A technique in logic programming where the absence of proof for a statement is
treated as evidence of its negation.

**Substitution Environment**:  
A key data structure in Logic Programming that maps logical Variables to their
bindings.

**Tail Calls / Tail Call Elimination**:  
A technique to optimize nested function calls by avoiding the accumulation of
stack frames.

**Thunking**:  
Thunking is a technique where a function's execution is delayed until it is
needed, potentially used to implement tail call optimization.

**Trampolining**:  
A technique for handling recursion without using additional stack space. It
turns nested calls into a loop.

**Unification**:  
The process of finding substitutions for logical variables such that two terms
become equal. It's a fundamental operation in Logic Programming.
