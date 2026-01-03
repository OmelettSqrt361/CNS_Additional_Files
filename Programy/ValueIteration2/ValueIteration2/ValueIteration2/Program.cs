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

namespace ValueIteration2
{
    class Program
    {
        // Parametry stavového prostoru
        static int diceSize = 2;
        static int[] p1 = { 0, 1, 2, 3, 4, 5, 6, 7};
        static int[] p2 = { 8, 1, 2, 3, 4, 5, 6, 9};

        // Diagnostic Data
        static int initializationTime;
        static int iterationTime;
        static int writeoutTime;
        static int numOfIter;

        // Parametry Iterace Hodnot
        static Dictionary<GameState, double> V = new Dictionary<GameState, double>();
        static double gamma = 0.95f;
        static double epsilon = 50;
        static string filepath = @"C:\Users\jakub\OneDrive\Plocha\Člověče nezlob se\CNS_Additional_Files\Strategie\cara7_highEps.json";

        //Caching
        static Dictionary<(GameState, int), List<Transition>> transitionCache;
        static Dictionary<GameState, int[]> actionCache;

        static void Main()
        {
            // Počáteční hodnoty
            var startState = new GameState(new int[] { 0, 0 }, new int[] { 0, 0 }, 1);
            Console.WriteLine($"Start: {startState}");
            Console.WriteLine($"[{string.Join(",", legalActions(startState))}]");

            Console.ReadLine();
            var inStopwatch = Stopwatch.StartNew();

            // Inicializace V
            transitionCache = new Dictionary<(GameState, int), List<Transition>>();
            actionCache = new Dictionary<GameState, int[]>();

            Console.Clear();
            Console.WriteLine("Hledání všech stavů a inicializace \n");
            HashSet<GameState> allStates = searchAllStates(startState);
            foreach (var state in allStates)
            {
                V[state] = 0.0f;
            }


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

            inStopwatch.Stop();
            writeoutTime = (int)inStopwatch.ElapsedMilliseconds;
            Console.WriteLine($"Time: {writeoutTime}");
            Console.ReadLine();


            //Diagnostics
            Console.Clear();
            Console.WriteLine($"Initialization Time\t{initializationTime}");
            Console.WriteLine($"Iteration Time\t{iterationTime}");
            Console.WriteLine($"Writeout Time\t{writeoutTime}");
            Console.WriteLine($"Sum\t{initializationTime+iterationTime+writeoutTime}");
            Console.WriteLine($"Iterations:\t{numOfIter}");
            Console.ReadLine();
        }

        static void PrintAllStates(HashSet<GameState> states)
        {
            foreach (var state in states)
            {
                Console.WriteLine($"{state},term:{isTerminal(state)}");
            }
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
                int current = Math.Min(state.MyFigs[i] + state.Dice, p1.Length - 1);
                if (!(state.MyFigs.Contains(current) && current != p1.Length - 1) && state.MyFigs[i] != p1.Length-1)
                {
                    resList.Add(i);
                }
            }
            if(resList.Count == 0)
            {
                // -1 Značí nulovou akci (nic se neděje)
                resList.Add(-1);
            }

            return resList.ToArray();
        }

        static List<Transition> nextStochastic(GameState state, int action)
        {
            // Řešení nulové akce pro hráče
            List<Transition> result = new List<Transition>();
            if (action == -1)
            {
                for (int i = 1; i < diceSize + 1; i++)
                {
                    result.Add(new Transition(new GameState(state.MyFigs.ToArray(), state.OppFigs.ToArray(), i), 1 / (double)diceSize));
                }
                return result;
            }

            int[] newFigs = state.MyFigs.ToArray();
            int[] newOpps = state.OppFigs.ToArray();

            newFigs[action] = Math.Min(state.MyFigs[action] + state.Dice, p1.Length - 1);
            for (int i = 0; i < newOpps.Length; i++)
            {
                if (p1[newFigs[action]] == p2[newOpps[i]])
                    newOpps[i] = 0;
            }

            //Zmenšení stavového prostoru, protože na permutaci figurek nezáleží
            Array.Sort(newFigs);
            Array.Sort(newOpps);

            for (int i = 1; i < diceSize + 1; i++)
            {
                result.Add(new Transition(new GameState(newFigs, newOpps, i), 1 / (double)diceSize));
            }
            return result;
        }

        // Random Opponent Policy
        static List<Transition> oppRngPolicy(GameState state)
        {

            List<int> legal = new List<int>();
            for (int i = 0; i < state.OppFigs.Length; i++)
            {
                int current = Math.Min(state.OppFigs[i] + state.Dice, p2.Length - 1);
                if (!(state.OppFigs.Contains(current) && current != p2.Length - 1) && state.OppFigs[i] != p2.Length - 1)
                {
                    legal.Add(i);
                }
            }

            List<Transition> diceless = new List<Transition>();
            if (legal.Count == 0)
            {
                diceless.Add(new Transition(new GameState(state.MyFigs.ToArray(), state.OppFigs.ToArray(), -1), 1.0));
            }
            else
            {
                foreach (var action in legal)
                {
                    int[] newFigs = state.MyFigs.ToArray();
                    int[] newOpps = state.OppFigs.ToArray();
                    newOpps[action] = Math.Min(state.OppFigs[action] + state.Dice, p2.Length - 1);
                    for (int i = 0; i < newFigs.Length; i++)
                    {
                        if (p1[newFigs[i]] == p2[newOpps[action]])
                            newFigs[i] = 0;
                    }
                    Array.Sort(newFigs);
                    Array.Sort(newOpps);

                    diceless.Add(new Transition(new GameState(newFigs, newOpps, -1), 1 / (double)legal.Count));
                }
            }

            List<Transition> diced = new List<Transition>();
            foreach (var transition in diceless)
            {
                for (int i = 1; i < diceSize + 1; i++)
                {
                    diced.Add(new Transition(new GameState(transition.State.MyFigs.ToArray(), transition.State.OppFigs.ToArray(), i), transition.Probability / (double)diceSize));
                }
            }
            return diced;
        }

        static List<Transition> nextStates(GameState state, int action)
        {
            List<Transition> resTrans = new List<Transition>();
            foreach (var oppsState in nextStochastic(state, action))
            {
                if (isTerminal(oppsState.State))
                {
                    resTrans.Add(oppsState);
                } else
                {
                    foreach (var res in oppRngPolicy(oppsState.State))
                    {
                        resTrans.Add(new Transition(res.State, res.Probability * oppsState.Probability));
                    }
                }
            }
            return resTrans;

        }

        static double reward(GameState state)
        {
            if (state.MyFigs.All(p => p == p1[p1.Length - 1]))
            {
                return 100.0f;
            }
            else if (state.OppFigs.All(p => p == p2[p2.Length - 1]))
            {
                return -100.0f;
            }
            else
            {
                return 0.0f;
            }
            
        }

        static bool isTerminal(GameState state)
        {
            return state.MyFigs.All(p => p == p1[p1.Length - 1])
                || state.OppFigs.All(p => p == p2[p2.Length - 1]);
        }

        static HashSet<GameState> searchAllStates(GameState start)
        {
            var visited = new HashSet<GameState>();
            var queue = new Queue<GameState>();

            for (int i = 1; i < diceSize + 1; i++)
            {
                GameState startState = new GameState(start.MyFigs.ToArray(), start.OppFigs.ToArray(), i);
                visited.Add(startState);
                queue.Enqueue(startState);

                foreach (var nextState in oppRngPolicy(startState))
                {
                    GameState swapState = new GameState(nextState.State.OppFigs.ToArray(), nextState.State.MyFigs.ToArray(), nextState.State.Dice);

                    visited.Add(swapState);
                    queue.Enqueue(swapState);
                }

            }

            int iteration = 0;
            while (queue.Count > 0)
            {
                iteration++;
                if(iteration % 1000 == 0)
                {
                    Console.WriteLine(iteration);
                }
                var s = queue.Dequeue();

                var legal = legalActions(s);
                actionCache[s] = legal;

                if (isTerminal(s)) 
                {
                    var next = nextStates(s, -1);
                    transitionCache[(s, -1)] = next;
                    continue;
                } 

                foreach (int action in legal)
                {
                    var next = nextStates(s, action);

                    transitionCache[(s, action)] = next;

                    foreach (var t in next)
                    {
                        if (!visited.Contains(t.State))
                        {
                            visited.Add(t.State);
                            queue.Enqueue(t.State);
                        }
                    }
                }
            }
            Console.WriteLine(iteration);
            return visited;
        }

        static double BellmanUpdate(GameState state)
        {
            double best = double.NegativeInfinity;

            foreach (int action in actionCache[state])
            {
                double q = 0.0f;
                foreach (var t in transitionCache[(state, action)])
                {
                    q += t.Probability * (reward(t.State) + gamma * V[t.State]);
                }
                best = Math.Max(best, q);
            }

            return best;
        }

        static void ValueIteration(HashSet<GameState> states)
        {
            int iteration = 0;

            
            while (true)
            {
                double delta = 0.0f;
                var stopwatch = Stopwatch.StartNew();

                Dictionary<GameState, double> tempV = new Dictionary<GameState, double>();
                foreach (var s in states)
                {
                    double oldV = V[s];
                    double newV = oldV;
                    if (!isTerminal(s)) newV = BellmanUpdate(s);
                    tempV[s] = newV;

                    delta = Math.Max(delta, Math.Abs(oldV - newV));
                }
                V = tempV;

                stopwatch.Stop();
                iteration++;
                Console.WriteLine($"Iteration:{iteration}, MaxDelta:{delta}, Elapsed Time: {stopwatch.ElapsedMilliseconds}");

                if (delta < epsilon) {
                    numOfIter = iteration;
                    break;
                }
            }


        }

        static int BestAction(GameState state)
        {
            double best = double.NegativeInfinity;
            int bestAction = -1;

            foreach (int action in actionCache[state])
            {
                double q = 0.0;

                foreach (var t in transitionCache[(state,action)])
                {
                    q += t.Probability * (reward(t.State) + gamma * V[t.State]);
                }
                if (q > best)
                {
                    best = q;
                    bestAction = action;
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

        static void PrintGameStateDictionary(Dictionary<GameState, int> dict)
        {
            foreach (var kvp in dict)
            {
                GameState state = kvp.Key;
                int value = kvp.Value;
                Console.WriteLine($"{state} => {value}");
            }

        }

        static void SavePolicy(Dictionary<GameState, int> policy, string filePath)
        {
            // Convert GameState keys to string
            var dictToSave = policy.ToDictionary(
                kvp => kvp.Key.ToString(),  // string key
                kvp => kvp.Value
            );

            // Serialize to JSON
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
