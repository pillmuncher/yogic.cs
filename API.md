## **API:**

### `Variable` Class
- Represents named logical variables.

### `SubstProxy` Class
- Represents variable bindings within a solution.

### Type Shortcuts
- `Seq = IReadOnlyCollection<object>;`
- `Pair = ValueTuple<object, object>;`
- `Subst = ImmutableDictionary<Variable, object>;`
- `Result = Tuple<ImmutableDictionary<Variable, object>, Next>;`

### `Next` Delegate
- `public delegate Result? Next();`
- A function type that represents a backtracking operation.

### `Emit` Delegate
- `public delegate Result? Emit(Subst subst, Next next);`
- A function type that represents a successful resolution.

### `Step` Delegate
- `public delegate Result? Step(Emit succeed, Next backtrack, Next escape);`
- A function type that represents a resolution step.

### `Goal` Delegate
- `public delegate Step Goal(Subst subst);`
- A function type that represents a resolvable logical statement.

### `Unit` Method
- `public static Step Unit(Subst subst)`
- Takes a substitution environment `subst` into a computation. Succeeds once and then initiates backtracking.

### `Cut` Method
- `public static Step Cut(Subst subst)`
- Takes a substitution environment `subst` into a computation. Succeeds once but, instead of normal backtracking, aborts the current computation and jumps to the previous choice point, effectively pruning the search space.

### `Fail` Method
- `public static Step Fail(Subst subst)`
- Takes a substitution environment `subst` into a computation. Never succeeds. Immediately initiates backtracking.

### `And` Method
- `public static Goal And(params Goal[] goals)`
- Conjunction of multiple goals. Takes a variable number of goals and returns a new goal that tries all of them in series. Fails if any goal fails.

### `Or` Method
- `public static Goal Or(params Goal[] goals)`
- A choice between multiple goals. Takes a variable number of goals and returns a new goal that tries all of them in series. Fails only if all goals fail. This defines a *choice point*.

### `Not` Method
- `public static Goal Not(Goal goal)`
- Negates `goal`. Fails if `goal` succeeds, and vice versa.

### `Unify` Method
- `public static Goal Unify(object o1, object o2)`
- Attempts to unify two objects. Fails if they aren't unifiable.

### `UnifyAll` Methods
- `public static Goal UnifyAll(IEnumerable<Pair> pairs)`
- `public static Goal UnifyAll(params Pair[] pairs)`
- Attempts to unify pairs of objects. Fails if any pair is not unifiable.

### `UnifyAny` Methods
- `public static Goal UnifyAny(Variable v, IEnumerable<object> objects)`
- `public static Goal UnifyAny(Variable v, params object[] objects)`
- Attempts to unify a variable with any one of the objects. Fails if no object is unifiable.

### `Resolve` Method
- `public static IEnumerable<SubstProxy> Resolve(Goal goal)`
- Performs logical resolution of `goal`. Returns an enumerable collection of substitution proxies representing solutions.
