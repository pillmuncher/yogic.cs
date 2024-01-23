// Copyright (c) 2023 Mick Krippendorf <m.krippendorf@freenet.de>

using System.Linq;
using System.Collections.Generic;

using MoreLinq;

namespace Yogic.Puzzle;

using static Yogic.Combinators;

using PuzzleDefinition = ValueTuple<List<Variable>, List<object>>;
using Candidates = Dictionary<object, HashSet<Variable>>;

public static class Puzzle
{
    private static Goal Solver1(PuzzleDefinition[] pairs)
    {
        return And(
            from pair in pairs
            select Or(
                from permutation in pair.Item2.Permutations()
                select Unify(pair.Item1, permutation)
            )
        );
    }

    private static Candidates Simplify(PuzzleDefinition[] puzzle)
    {
        var candidates = new Candidates();
        foreach (var (variables, numbers) in puzzle)
            foreach (var number in numbers)
                if (candidates.ContainsKey(number))
                    candidates[number].IntersectWith(variables);
                else
                    candidates[number] = new HashSet<Variable>(variables);
        return candidates;
    }

    private static Goal Solver2(PuzzleDefinition[] puzzle)
    {
        return And(
            from pair in Simplify(puzzle)
            select Or(from variable in pair.Value select Unify(variable, pair.Key))
        );
    }

    public static void Main()
    {
        var a = new Variable("a");
        var b = new Variable("b");
        var c = new Variable("c");
        var d = new Variable("d");
        var e = new Variable("e");
        var f = new Variable("f");
        var g = new Variable("g");
        var h = new Variable("h");
        var i = new Variable("i");
        var j = new Variable("j");
        var k = new Variable("k");
        var l = new Variable("l");

        var puzzle = new PuzzleDefinition[]
        {
            (new() { a, b, c, e, h, i, j }, new() { 2, 4, 5, 8, 10, 11, 12 }),
            (new() { a, b, f, i, j, k, l }, new() { 1, 4, 5, 6, 7, 8, 12 }),
            (new() { a, c, d, e, f, k, l }, new() { 1, 2, 6, 7, 8, 9, 10 }),
            (new() { a, c, f, g, i, j, k }, new() { 1, 2, 3, 4, 6, 8, 12 }),
            (new() { b, c, d, e, f, g, h }, new() { 1, 2, 3, 5, 9, 10, 11 }),
            (new() { b, c, e, g, h, j, l }, new() { 2, 3, 4, 5, 7, 10, 11 }),
        };

        foreach (var subst in Resolve(Solver2(puzzle)))
        {
            Console.WriteLine($"a = {subst[a]}");
            Console.WriteLine($"b = {subst[b]}");
            Console.WriteLine($"c = {subst[c]}");
            Console.WriteLine($"d = {subst[d]}");
            Console.WriteLine($"e = {subst[e]}");
            Console.WriteLine($"f = {subst[f]}");
            Console.WriteLine($"g = {subst[g]}");
            Console.WriteLine($"h = {subst[h]}");
            Console.WriteLine($"i = {subst[i]}");
            Console.WriteLine($"j = {subst[j]}");
            Console.WriteLine($"k = {subst[k]}");
            Console.WriteLine($"l = {subst[l]}");
            Console.WriteLine();
        }
    }
}
