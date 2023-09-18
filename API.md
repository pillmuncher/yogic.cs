# **API Documentation**

```csharp
public class Variable
```
- Represents named logical variables.

```csharp
using Subst = ImmutableDictionary<Variable, object>;
```
- Represents variable bindings within a solution.

```csharp
using Seq = IReadOnlyCollection<object>;
using Pair = ValueTuple<object, object>;
using Subst = ImmutableDictionary<Variable, object>;
using Result = Tuple<ImmutableDictionary<Variable, object>, Next>;
```
- Miscellaneous Type Alisases.

```csharp
public delegate Result? Next();
```
- A function type that represents a backtracking operation.

```csharp
public delegate Result? Emit(Subst subst, Next next);
```
- A function type that represents a successful resolution.

```csharp
public delegate Result? Step(Emit succeed, Next backtrack, Next
escape);
```
- A function type that represents a resolution step.

```csharp
public delegate Step Goal(Subst subst);
```
- A function type that represents a resolvable logical statement.

```csharp
public static Step Unit(Subst subst)
```
- Takes a substitution environment `subst` into a computation. Succeeds once
  and then initiates backtracking.

```csharp
public static Step Cut(Subst subst)
```
- Takes a substitution environment `subst` into a computation. Succeeds once
  but, instead of normal backtracking, aborts the current computation and
  jumps to the previous choice point, effectively pruning the search space.

```csharp
public static Step Fail(Subst subst)
```
- Takes a substitution environment `subst` into a computation. Never succeeds.
  Immediately initiates backtracking.

```csharp
public static Goal Then(Goal goal1, Goal goal2)
```
- Conjunction of two goals. Computes `goal1` first and if successful, computes
  `goal2`. Succeeds only of both goals succeed.

```csharp
public static Goal And(params Goal[] goals)
```
- Conjunction of multiple goals. Takes a variable number of goals and returns
  a new goal that tries all of them in series. Fails if any goal fails.

```csharp
public static Goal Choice(Goal goal1, Goal goal2)
```
- Adjunction of two goals. Computes `goal1` first and if if that fails,
  computes `goal2`. Succeeds if at least one goal succeeds.

```csharp
public static Goal Or(params Goal[] goals)
```
- A choice between multiple goals. Takes a variable number of goals and
  returns a new goal that tries all of them in series. Fails only if all goals
  fail. This defines a *choice point*.

```csharp
public static Goal Not(Goal goal)
```
- Negates `goal`. Fails if `goal` succeeds, and vice versa.

```csharp
public static Goal Unify(object o1, object o2)
```
- Attempts to unify two objects. Fails if they aren't unifiable.

```csharp
public static Goal UnifyAll(IEnumerable<Pair> pairs)
public static Goal UnifyAll(params Pair[] pairs)
```
- Attempts to unify pairs of objects. Fails if any pair is not unifiable.

```csharp
public static Goal UnifyAny(Variable v, IEnumerable<object> objects)
public static Goal UnifyAny(Variable v, params object[] objects)
```
- Attempts to unify a variable with any one of the objects. Fails if no object
  is unifiable.

```csharp
public static IEnumerable<SubstProxy> Resolve(Goal goal)
```
- Performs logical resolution of `goal`. Returns an enumerable collection of
  substitution proxies representing solutions.
