// Copyright (c) 2023 Mick Krippendorf <m.krippendorf@freenet.de>

using System.Linq;
using System.Collections.Generic;

using MoreLinq;

using static Yogic.Combinators;

namespace Yogic.Puzzle;

using PuzzleDefinition = (List<Variable> variables, List<int> numbers);
using Candidates = Dictionary<object, HashSet<Variable>>;

public static class Puzzle
{

    private static Goal Solver1(PuzzleDefinition[] puzzle)
    {
        return And(
            from line in puzzle
            select Or(
                from permutation in line.numbers.Permutations()
                select UnifyAll(line.variables.Zip(permutation))
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
            ([ a, b, c, e, h, i, j ], [ 2, 4, 5, 8, 10, 11, 12 ]),
            ([ a, b, f, i, j, k, l ], [ 1, 4, 5, 6, 7, 8, 12 ]),
            ([ a, c, d, e, f, k, l ], [ 1, 2, 6, 7, 8, 9, 10 ]),
            ([ a, c, f, g, i, j, k ], [ 1, 2, 3, 4, 6, 8, 12 ]),
            ([ b, c, d, e, f, g, h ], [ 1, 2, 3, 5, 9, 10, 11 ]),
            ([ b, c, e, g, h, j, l ], [ 2, 3, 4, 5, 7, 10, 11 ]),
        };

        foreach (var subst in Resolve(Solver1(puzzle)))
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
