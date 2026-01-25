using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Immutable;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace domecekEnd
{
    // Parametry stavového prostoru
    // static int diceSize = 6;
    // static int[] p1 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23};
    // static int[] p2 = { -1, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 24, 25, 26 };

    class Program
    {
        // Parametry stavového prostoru
        static int diceSize = 6;
        static int[] p1 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22 };
        static int[] p2 = { -1, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 23, 24 };

        // Diagnostic Data
        static int initializationTime;
        static int iterationTime;
        static int writeoutTime;
        static int numOfIter;

        // Parametry Iterace Hodnot
        static Dictionary<GameState, double> V = new Dictionary<GameState, double>();
        static double gamma = 0.94f;
        static double epsilon = 1e-3;
        static string filepath = @"C:\Users\jakub\OneDrive\Plocha\Člověče nezlob se\CNS_Additional_Files\Strategie\cns_valueIterMinimax.json";
        static string filepath2 = @"C:\Users\jakub\OneDrive\Plocha\Člověče nezlob se\CNS_Additional_Files\Strategie\cns_valueIterMinimaxVALUES.json";

        //Caching
        static Dictionary<(GameState, int), List<Transition>> transitionCache;
        static Dictionary<GameState, int[]> actionCache;
        static Dictionary<GameState, int[]> actionCacheOpp;

        //Maximin
        static int isMinimax = 1;

        static void Main()
        {
            // Počáteční hodnoty
            var startState = new GameState(new int[] { 0, 0 }, new int[] { 0, 0 }, 1);
            Console.WriteLine($"MINIMAX VALUE ITERATION");
            Console.WriteLine($"Start: {startState}");
            Console.WriteLine($"[{string.Join(",", legalActions(startState))}]");

            Console.ReadLine();
            var inStopwatch = Stopwatch.StartNew();

            // Inicializace V
            transitionCache = new Dictionary<(GameState, int), List<Transition>>();
            actionCache = new Dictionary<GameState, int[]>();
            actionCacheOpp = new Dictionary<GameState, int[]>();

            Console.Clear();
            Console.WriteLine("Hledání všech stavů a inicializace \n");
            HashSet<GameState> allStates = searchAllAllStates(startState);
            foreach (var state in allStates)
            {
                V[state] = 0.0f;
            }
            SaveValueFunction(V, filepath2);


            inStopwatch.Stop();
            initializationTime = (int)inStopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Time: {initializationTime}");
            Console.ReadLine();
            inStopwatch.Reset();
            inStopwatch.Start();


            // Iterace Hodnot
            Console.Clear();
            Console.WriteLine("Iterace hodnot \n");
            ValueIteration(allStates);

            inStopwatch.Stop();
            iterationTime = (int)inStopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Time: {iterationTime}");
            Console.ReadLine();
            inStopwatch.Reset();
            inStopwatch.Start();


            //Vypsání optimální akce
            Console.Clear();
            Console.WriteLine("Nalezená strategie \n");
            SavePolicy(optimalPolicyGen(allStates), filepath);
            SaveValueFunction(V, filepath2);

            inStopwatch.Stop();
            writeoutTime = (int)inStopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Time: {writeoutTime}");
            Console.ReadLine();


            //Diagnostics
            Console.Clear();
            Console.WriteLine($"Initialization Time\t{initializationTime}");
            Console.WriteLine($"Iteration Time\t{iterationTime}");
            Console.WriteLine($"Writeout Time\t{writeoutTime}");
            Console.WriteLine($"Sum\t{initializationTime + iterationTime + writeoutTime}");
            Console.WriteLine($"Iterations:\t{numOfIter}");
            Console.ReadLine();
        }

        static int[] legalActions(GameState state)
        {
            List<int> resList = new List<int>();
            if (isTerminal(state))
            {
                resList.Add(-1);
                return resList.ToArray();
            }
            for (int i = 0; i < state.MyFigs.Length; i++)
            {
                int current = state.MyFigs[i] + state.Dice;
                if (!state.MyFigs.Contains(current) && current <= p1.Length-1)
                {
                    resList.Add(i);
                }
            }
            if (resList.Count == 0)
            {
                // -1 Značí nulovou akci (nic se neděje)
                resList.Add(-1);
            }

            return resList.ToArray();
        }


        static int[] legalActionsOpp(GameState state)
        {
            List<int> resList = new List<int>();
            if (isTerminal(state))
            {
                resList.Add(-1);
                return resList.ToArray();
            }
            for (int i = 0; i < state.OppFigs.Length; i++)
            {
                int current = state.OppFigs[i] + state.Dice;
                if (!state.OppFigs.Contains(current) && current <= p2.Length)
                {
                    resList.Add(i);
                }
            }
            if (resList.Count == 0)
            {
                // -1 Značí nulovou akci (nic se neděje)
                resList.Add(-1);
            }

            return resList.ToArray();
        }

        static List<Transition> nextStates(GameState state, int action)
        {
            List<Transition> result = new List<Transition>();

            if (action == -1)
            {
                for (int d = 1; d <= diceSize; d++)
                {
                    result.Add(new Transition(
                        new GameState(state.MyFigs.ToArray(), state.OppFigs.ToArray(), d),
                        1.0 / diceSize
                    ));
                }
                return result;
            }

            int[] newMy = state.MyFigs.ToArray();
            int[] newOpp = state.OppFigs.ToArray();

            newMy[action] = Math.Min(newMy[action] + state.Dice, p1.Length - 1);

            for (int i = 0; i < newOpp.Length; i++)
            {
                if (p1[newMy[action]] == p2[newOpp[i]])
                    newOpp[i] = 0;
            }

            Array.Sort(newMy);
            Array.Sort(newOpp);

            for (int d = 1; d <= diceSize; d++)
            {
                result.Add(new Transition(
                    new GameState(newMy, newOpp, d),
                    1.0 / diceSize
                ));
            }

            return result;
        }


        static double reward(GameState state)
        {
            
            if (state.MyFigs.SequenceEqual(new[] { 21, 22 }) && state.OppFigs.SequenceEqual(new[] { 21, 22 }))
            {
                return 0;
            } else if (state.MyFigs.SequenceEqual(new[] { 21, 22 }))
            {
                return 100*isMinimax;
            } else if (state.OppFigs.SequenceEqual(new[] { 21, 22 }))
            {
                return -100*isMinimax;
            }
            else
            {
                return 0;
            }

        }

        static bool isTerminal(GameState state)
        {
            return state.MyFigs.SequenceEqual(new[] { 21, 22 }) ||
                   state.OppFigs.SequenceEqual(new[] { 21, 22 });
        }

        static HashSet<GameState> searchAllAllStates(GameState start)
        {
            var allStates = new HashSet<GameState>();

            int myPiecesCount = start.MyFigs.Length;
            int oppPiecesCount = start.OppFigs.Length;
            int maxP1 = p1.Length - 1;
            int maxP2 = p2.Length - 1;

            int startMy = p1[0];
            int endMy = p1[maxP1];
            int startOpp = p2[0];
            int endOpp = p2[maxP2];

            // Helper to enumerate all positions for N pieces up to maxPos
            IEnumerable<int[]> AllPositions(int numPieces, int maxPos)
            {
                int[] positions = new int[numPieces];
                while (true)
                {
                    yield return (int[])positions.Clone();

                    int idx = 0;
                    while (idx < numPieces)
                    {
                        positions[idx]++;
                        if (positions[idx] <= maxPos)
                            break;
                        positions[idx] = 0;
                        idx++;
                    }
                    if (idx == numPieces) break;
                }
            }

            bool ArePositionsValid(int[] myPos, int[] oppPos)
            {
                var seen = new HashSet<int>();

                bool IsFreeOverlap(int pos) =>
                    pos == startMy || pos == endMy || pos == startOpp || pos == endOpp;

                foreach (var p in myPos)
                {
                    if (IsFreeOverlap(p)) continue;
                    if (!seen.Add(p1[p])) return false;
                }

                foreach (var p in oppPos)
                {
                    if (IsFreeOverlap(p)) continue;
                    if (!seen.Add(p2[p])) return false;
                }

                return true;
            }

            foreach (var myPos in AllPositions(myPiecesCount, maxP1))
            {
                foreach (var oppPos in AllPositions(oppPiecesCount, maxP2))
                {
                    // Skip invalid states
                    if (!ArePositionsValid(myPos, oppPos))
                        continue;

                    for (int dice = 1; dice <= diceSize; dice++)
                    {
                        var state = new GameState(myPos, oppPos, dice);
                        allStates.Add(state);

                        if (allStates.Count % 100000 == 0)
                            Console.WriteLine(allStates.Count);
                    }
                }
            }

            Console.WriteLine("Saving...");
            foreach (var state in allStates)
            {
                var legal = legalActions(state);
                actionCache[state] = legal;
                actionCacheOpp[state] = legalActionsOpp(state);

                foreach (var action in legal)
                    transitionCache[(state, action)] = nextStates(state, action);
            }

            Console.WriteLine(allStates.Count);
            return allStates;
        }


        static double BellmanMinimax(GameState state)
        {
            if (isTerminal(state))
                return reward(state);

            double bestValue = double.NegativeInfinity;

            // MAX: my action
            foreach (int myAction in actionCache[state])
            {
                double myExpectedValue = 0.0;

                // Dice after my action
                foreach (var myT in transitionCache[(state, myAction)])
                {
                    GameState afterMyMove = myT.State;
                    double pMy = myT.Probability;

                    if (isTerminal(afterMyMove))
                    {
                        myExpectedValue += pMy * reward(afterMyMove);
                        continue;
                    }

                    // MIN: opponent chooses action
                    double worstOpponentValue = double.PositiveInfinity;

                    foreach (int oppAction in actionCacheOpp[afterMyMove])
                    {
                        double oppExpected = 0.0;

                        // Dice after opponent action
                        foreach (var oppT in OpponentTransitions(afterMyMove, oppAction))
                        {
                            double r = reward(oppT.State);
                            double v = isTerminal(oppT.State) ? 0.0 : gamma * V[oppT.State];
                            oppExpected += oppT.Probability * (r + v);
                        }

                        worstOpponentValue = Math.Min(worstOpponentValue, oppExpected);
                    }

                    myExpectedValue += pMy * worstOpponentValue;
                }

                bestValue = Math.Max(bestValue, myExpectedValue);
            }

            return bestValue;
        }



        static List<Transition> OpponentTransitions(GameState state, int action)
        {
            List<Transition> result = new List<Transition>();

            int[] newMy = state.MyFigs.ToArray();
            int[] newOpp = state.OppFigs.ToArray();

            if (action != -1)
            {
                newOpp[action] = Math.Min(newOpp[action] + state.Dice, p2.Length - 1);

                for (int i = 0; i < newMy.Length; i++)
                {
                    if (p1[newMy[i]] == p2[newOpp[action]])
                        newMy[i] = 0;
                }

                Array.Sort(newMy);
                Array.Sort(newOpp);
            }

            for (int d = 1; d <= diceSize; d++)
            {
                result.Add(new Transition(
                    new GameState(newMy, newOpp, d),
                    1.0 / diceSize
                ));
            }

            return result;
        }



        static void ValueIteration(HashSet<GameState> states)
        {
            int iteration = 0;

            while (true)
            {
                var time = new Stopwatch();
                time.Reset();
                time.Start();

                double delta = 0.0;
                var newV = new Dictionary<GameState, double>(states.Count);

                foreach (var s in states)
                {
                    double oldValue = V[s];
                    double newValue = isTerminal(s) ? reward(s) : BellmanMinimax(s);

                    newV[s] = newValue;
                    delta = Math.Max(delta, Math.Abs(oldValue - newValue));
                }

                V = newV;
                iteration++;

                time.Stop();
                Console.WriteLine($"Iteration {iteration}, maxDelta = {delta}, time = {time.ElapsedMilliseconds}ms");

                if (delta < epsilon)
                {
                    numOfIter = iteration;
                    break;
                }
            }
        }


        static int BestAction(GameState state)
        {
            if (isTerminal(state))
                return -1;

            double bestValue = double.NegativeInfinity;
            int bestAction = -1;

            foreach (int myAction in actionCache[state])
            {
                double actionValue = 0.0;

                foreach (var myT in transitionCache[(state, myAction)])
                {
                    GameState afterMyMove = myT.State;
                    double pMy = myT.Probability;

                    if (isTerminal(afterMyMove))
                    {
                        actionValue += pMy * reward(afterMyMove);
                        continue;
                    }

                    double worstOpponentValue = double.PositiveInfinity;

                    foreach (int oppAction in actionCacheOpp[afterMyMove])
                    {
                        double oppExpected = 0.0;

                        foreach (var oppT in OpponentTransitions(afterMyMove, oppAction))
                        {
                            oppExpected += oppT.Probability *
                                (reward(oppT.State) + gamma * V[oppT.State]);
                        }

                        worstOpponentValue = Math.Min(worstOpponentValue, oppExpected);
                    }

                    actionValue += pMy * worstOpponentValue;
                }

                if (actionValue > bestValue)
                {
                    bestValue = actionValue;
                    bestAction = myAction;
                }
            }

            return bestAction;
        }





        static Dictionary<GameState, int> optimalPolicyGen(HashSet<GameState> allStates)
        {
            Dictionary<GameState, int> res = new Dictionary<GameState, int>();
            foreach (var state in allStates)
            {
                res[state] = BestAction(state);
            }
            return res;
        }

        static void SavePolicy(Dictionary<GameState, int> policy, string filePath)
        {
            var dictToSave = policy.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value
            );

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(dictToSave, options);

            File.WriteAllText(filePath, json);
        }

        static void SaveValueFunction(Dictionary<GameState, double> policy, string filePath)
        {
            var dictToSave = policy.ToDictionary(
                kvp => kvp.Key.ToString(),
                kvp => kvp.Value
            );

            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(dictToSave, options);

            File.WriteAllText(filePath, json);
        }

    }

    public struct GameState : IEquatable<GameState>
    {
        // Inicializace potřebných proměnných
        // Proměnné MyFigs a OppFigs obsahují pouze "imaginární" hodnotu pole,
        // skutečné pole najdeme v array p1 nebo p2 jako p1[MyFigs[i]] nebo p2[OppFigs[i]]
        public readonly ImmutableArray<int> MyFigs;
        public readonly ImmutableArray<int> OppFigs;
        public readonly int Dice;

        public GameState(int[] myPieces, int[] oppPieces, int dice)
        {
            MyFigs = ImmutableArray.Create(myPieces).OrderBy(x => x).ToImmutableArray();
            OppFigs = ImmutableArray.Create(oppPieces).OrderBy(x => x).ToImmutableArray();
            Dice = dice;
        }

        //Porovnávání stavů
        public bool Equals(GameState other)
        {
            return MyFigs.SequenceEqual(other.MyFigs)
            && OppFigs.SequenceEqual(other.OppFigs)
            && Dice == other.Dice;
        }

        public override bool Equals(object obj)
        {
            if (obj is GameState other)
                return Equals(other);
            return false;
        }

        public override string ToString()
        {
            return $"[{string.Join(",", MyFigs)}];" +
           $"[{string.Join(",", OppFigs)}];" +
           $"{Dice}";
        }

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
    }

    public struct Transition
    {
        public GameState State;
        public double Probability;

        public Transition(GameState state, double probability)
        {
            State = state;
            Probability = probability;
        }

        public override string ToString()
        {
            return $"[{string.Join(",", State.MyFigs)}];" +
            $"[{string.Join(",", State.OppFigs)}];" +
            $"{State.Dice},Prob:{Probability}";
        }
    }
}
