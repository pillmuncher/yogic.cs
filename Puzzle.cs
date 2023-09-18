// Copyright (c) 2021 Mick Krippendorf <m.krippendorf@freenet.de>

using System.Linq;
using System.Collections.Generic;

namespace Yogic.Puzzle;

using static Yogic.Combinators;

public static class Puzzle
{
    static IEnumerable<List<object>> GetPermutations(List<int> numbers)
    {
        if (1 == numbers.Count)
        {
            yield return new List<object> { (object)numbers[0] };
        }
        else
        {
            for (var i = 0; i < numbers.Count; ++i)
            {
                var number = numbers[i];
                numbers.RemoveAt(i);
                foreach (var permutation in GetPermutations(numbers))
                {
                    yield return new List<object>(permutation.Prepend<object>(number));
                }
                numbers.Insert(i, number);
            }
        }
    }

    private static Goal Solver(ValueTuple<int[], Variable[]>[] pairs)
    {
        return And(
            from pair in pairs
            select Or(
                from permutation in GetPermutations(new List<int>(pair.Item1))
                select Unify(permutation, pair.Item2)
            )
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

        var puzzle = new ValueTuple<int[], Variable[]>[]
        {
            new (new int[] { 1, 2, 3, 4, 6, 8, 12 }, new Variable[] { a, c, f, g, i, j, k }),
            new (new int[] { 1, 2, 4, 5, 6, 7, 12 }, new Variable[] { a, b, f, i, j, k, l }),
            new (new int[] { 1, 2, 6, 7, 8, 9, 10 }, new Variable[] { a, c, d, e, f, k, l }),
            new (new int[] { 1, 3, 5, 8, 9, 10, 11 }, new Variable[] { b, c, d, e, f, g, h }),
            new (new int[] { 2, 4, 5, 8, 10, 11, 12 }, new Variable[] { a, b, c, e, h, i, j }),
            new (new int[] { 3, 4, 5, 7, 8, 10, 11 }, new Variable[] { b, c, e, g, h, j, l }),
        };

        var goal = Solver(puzzle);

        foreach (var subst in Resolve(goal))
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
