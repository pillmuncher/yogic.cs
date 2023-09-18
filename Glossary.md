Here's a glossary of terms based on the code and the related concepts:

**Algebraic Structure**:  
A set of operations and rules defined on a set, which can be used to describe relationships between elements in that set. In the code, algebraic structures are referred to in the context of monoids and distributive lattices.

**Backtracking**:  
A technique used in search and logic programming to explore multiple possibilities. If a certain path does not lead to a solution, the program can backtrack and try an alternative path.

**Combinators / Combinator Functions**:  
Functions that take other functions as input and produce new functions as output. In the code, combinators are used to create and manipulate logical goals.

**Continuation**:  
A representation of the future of a computation. In the code, continuations are used to manage the flow of logical resolution and backtracking.

**Continuation Monad**:  
A monad that encapsulates computations with continuations. In the code, the Triple-Barrelled Continuation Monad is used for logical resolution and backtracking.

**Cut / Branch Pruning**:  
In the context of logic programming, "cut" is a combinator that succeeds once and then prevents backtracking beyond the point where it was invoked. This effectively prunes or cuts off branches of the search space.

**Distributive Lattice**:  
A mathematical structure where two binary operations, meet (infimum) and join (supremum), satisfy certain distributive properties. In the code, combinators like `Then` and `Choice` form a distributive lattice.

**Horn Clauses**:  
A specific form of logic programming clause where there is at most one positive literal (i.e., a statement) on the right side of the implication (=>). Horn clauses are commonly used in logic programming.

**Logic Programming**:  
A paradigm for programming that uses symbolic logic and inference rules to express and solve problems. The code provides tools for logic programming.

**Logical Resolution / Goal**:  
The process of finding solutions to logical queries or goals. In the code, a goal represents a logical query to be resolved.

**Logical Variables**:  
Variables in logic programming that can be bound to values or other variables in the substitution environment.

**Modus Ponens**:  
A common rule of inference in logic. If you have a statement in the form "if A then B," and you know that A is true, you can infer that B is true.

**Monad**:  
A design pattern and mathematical concept used for managing side effects and computations in a structured way. In the code, monads are used for managing logical computations.

**Monoid**:  
A mathematical structure consisting of a set and an associative binary operation with an identity element. In the code, combinators like `Unit` and `Then` form a monoid.

**Negation as Failure**:  
A technique in logic programming where the absence of proof for a statement is treated as evidence of its negation.

**Substitution Environment**:  
An immutable dictionary that maps logical variables to their bindings. It's a key data structure in logic programming.

**Tail Calls / Tail Call Elimination**:  
A technique to optimize recursive function calls by avoiding the accumulation of stack frames. Tail call elimination is mentioned in the code comments.

**Thunk / Thunking**:  
Thunking is a technique where a function's execution is delayed until it's needed. It can be used to implement tail call optimization.

**Trampoline / Trampolining**:  
A technique for handling recursion without using additional stack space. It involves wrapping recursive calls in a loop.

**Triple-Barrelled Continuation Monad**:  
A specific type of continuation monad used in the code for logical resolution, backtracking, and branch pruning.

**Unification**:  
The process of finding substitutions for logical variables such that two terms become equal. It's a fundamental operation in logic programming.
