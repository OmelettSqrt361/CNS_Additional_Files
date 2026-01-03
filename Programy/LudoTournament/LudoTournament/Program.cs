using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Collections.Immutable;

namespace LudoTournament
{
    class Program
    {
        static int diceSize = 2;
        static int[] p1 = { 0, 1, 2, 3, 4, 5, 6, 7 };
        static int[] p2 = { 8, 1, 2, 3, 4, 5, 6, 9 };
        static int numGamesPerPair = 100000;

        static Random rnd = new Random();

        static void Main()
        {
            List<string> strategyFiles = new List<string>();
            Console.WriteLine("Enter the full paths to your strategy JSON files, one per line.");
            Console.WriteLine("Press Enter on a blank line to start the simulation.");

            while (true)
            {
                string line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    break;

                if (File.Exists(line))
                    strategyFiles.Add(line);
                else
                    Console.WriteLine("File not found. Please enter a valid path.");
            }

            if (strategyFiles.Count < 2)
            {
                Console.WriteLine("Need at least 2 strategies to simulate.");
                return;
            }

            var strategies = strategyFiles.Select(path => LoadStrategy(path)).ToArray();
            string[] strategyNames = strategyFiles.Select(Path.GetFileNameWithoutExtension).ToArray();

            int n = strategies.Length;

            double[,] firstPlayerWinsProc = new double[n, n];

            for (int i = 0; i < n; i++)
            {
                for (int j = 0; j < n; j++)
                {

                    Console.WriteLine($"Simulating {strategyNames[i]} vs {strategyNames[j]}");
                    int firstWins = 0;
                    for (int g = 0; g < numGamesPerPair; g++)
                    {
                        int winner = PlayGame(strategies[j], strategies[i]);
                        if (winner == 1) firstWins++;
                    }

                    firstPlayerWinsProc[i, j] = (double)firstWins / (double)numGamesPerPair;
                }
            }

            // Print table
            Console.WriteLine("First player win procent:");
            PrintMatrix(firstPlayerWinsProc, strategyNames);;

            Console.ReadLine();
            Console.WriteLine("Tournament finished!");
            Console.ReadLine();
        }


        //Načítání strategií
        static Dictionary<GameState, int> LoadStrategy(string filePath)
        {
            string json = File.ReadAllText(filePath);
            var dict = JsonSerializer.Deserialize<Dictionary<string, int>>(json);

            return dict.ToDictionary(
                kvp => ParseGameState(kvp.Key),
                kvp => kvp.Value
            );
        }

        // Parsování her
        static GameState ParseGameState(string s)
        {
            // Format: "[0,1];[0,1];1"
            var parts = s.Split(';');
            int dice = int.Parse(parts.Last());
            var myFigs = parts[0].Trim('[', ']').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            var oppFigs = parts[1].Trim('[', ']').Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();
            return new GameState(myFigs, oppFigs, dice);
        }

        // Zahrát hru
        static int PlayGame(Dictionary<GameState, int> firstStrategy, Dictionary<GameState, int> secondStrategy)
        {
            var myFigs = new int[] { 0, 0 };
            var oppFigs = new int[] { 0, 0 };
            int dice = rnd.Next(1, diceSize + 1);
            bool firstToPlay = true;
            var state = new GameState(myFigs, oppFigs, dice);

            while (true)
            {
                Dictionary<GameState, int> strategy = firstToPlay ? firstStrategy : secondStrategy;
                int action = strategy.ContainsKey(state) ? strategy[state] : -1;

                int[] figs;
                int[] opps;


                figs = state.MyFigs.ToArray();
                opps = state.OppFigs.ToArray();

                // Provést akci
                if (action != -1)
                {

                    figs[action] = Math.Min(figs[action] + dice, p1.Length - 1);

                    // Sebrání
                    for (int i = 0; i < opps.Length; i++)
                    {
                        if (firstToPlay)
                        {
                            if (p1[figs[action]] == p2[opps[i]])
                            {
                                opps[i] = 0;
                            }
                        }
                        else
                        {
                            if (p2[figs[action]] == p1[opps[i]])
                            {
                                opps[i] = 0;
                            }
                        }
                    }
                }

                dice = rnd.Next(1, diceSize + 1);
                Array.Sort(figs);
                Array.Sort(opps);
                state = new GameState(opps, figs, dice);
                

                // Podívat se jestli je terminální
                if (state.MyFigs.All(p => p == p1[p1.Length - 1]))
                {
                    if (firstToPlay) return 1;
                    else return 2;
                }

                firstToPlay = !firstToPlay;
            }
        }

        // Vypisování matic
        static void PrintMatrix(double[,] matrix, string[] names)
        {
            Console.Write("\t");
            foreach (var name in names) Console.Write($"{name}\t");
            Console.WriteLine();
            for (int i = 0; i < names.Length; i++)
            {
                Console.Write($"{names[i]}\t");
                for (int j = 0; j < names.Length; j++)
                {
                    Console.Write($"{matrix[i, j]}\t");
                }
                Console.WriteLine();
            }
        }

        // Definice GameStatů
        public struct GameState : IEquatable<GameState>
        {
            public readonly ImmutableArray<int> MyFigs;
            public readonly ImmutableArray<int> OppFigs;
            public readonly int Dice;

            public GameState(int[] myPieces, int[] oppPieces, int dice)
            {
                MyFigs = ImmutableArray.Create(myPieces).OrderBy(x => x).ToImmutableArray();
                OppFigs = ImmutableArray.Create(oppPieces).OrderBy(x => x).ToImmutableArray();
                Dice = dice;
            }

            public bool Equals(GameState other) =>
                MyFigs.SequenceEqual(other.MyFigs) &&
                OppFigs.SequenceEqual(other.OppFigs) &&
                Dice == other.Dice;

            public override bool Equals(object obj) => obj is GameState other && Equals(other);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hash = 17;
                    foreach (int p in MyFigs) hash = hash * 23 + p;
                    foreach (int p in OppFigs) hash = hash * 23 + p;
                    hash = hash * 23 + Dice;
                    return hash;
                }
            }

            public override string ToString()
            {
                return $"[{string.Join(",", MyFigs)}];[{string.Join(",", OppFigs)}];{Dice}";
            }
        }
    }
}
