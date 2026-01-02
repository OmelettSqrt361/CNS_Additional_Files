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

namespace NaiveStrategies
{
    class Program
    {
        // Parametry stavového prostoru
        static int diceSize = 2;
        static int[] p1 = { 0, 1, 2, 3, 4, 5, 6, 7 };
        static int[] p2 = { 8, 1, 2, 3, 4, 5, 6, 9 };

        // Diagnostic Data
        static int numOfIter;

        // Parametry Iterace Hodnot
        static Dictionary<GameState, double> V = new Dictionary<GameState, double>();
        static double gamma = 0.95f;
        static double epsilon = 10;
        static string filepath = @"C:\Users\jakub\OneDrive\Plocha\Člověče nezlob se\CNS_Additional_Files\Strategie\cara7_depthSearch5.json";

        //Caching
        static Dictionary<(GameState, int), List<Transition>> transitionCache;
        static Dictionary<GameState, int[]> actionCache;

        static void Main()
        {
            // Počáteční hodnoty
            var startState = new GameState(new int[] { 0, 0 }, new int[] { 0, 0 }, 1);
            Console.WriteLine($"Naive start: {startState}");
            Console.WriteLine($"[{string.Join(",", legalActions(startState))}]");

            Console.ReadLine();

            // Inicializace V
            transitionCache = new Dictionary<(GameState, int), List<Transition>>();
            actionCache = new Dictionary<GameState, int[]>();

            Console.Clear();
            Console.WriteLine("Hledání všech stavů a inicializace \n");
            HashSet<GameState> allStates = searchAllStates(startState);

            //Vypsání vybrané akce
            Console.Clear();
            Console.WriteLine("Nalezená strategie \n");
            SavePolicy(minimaxPolicyGen(allStates,5), filepath);
            Console.WriteLine("Hotovo");
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
                if (!(state.MyFigs.Contains(current) && current != p1.Length - 1) && state.MyFigs[i] != p1.Length - 1)
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
                if (p1[newFigs[action]] == p2[newOpps[i]]) //System.IndexOutOfRangeException: Index je mimo hranice pole.
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
                }
                else
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
                    visited.Add(nextState.State);
                    queue.Enqueue(nextState.State);
                }

            }

            int iteration = 0;
            while (queue.Count > 0)
            {
                iteration++;
                if (iteration % 1000 == 0)
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

                if (delta < epsilon)
                {
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

                foreach (var t in transitionCache[(state, action)])
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

        static Dictionary<GameState, int> bulletPolicyGen(HashSet<GameState> allStates)
        {
            Dictionary<GameState, int> res = new Dictionary<GameState, int>();
            foreach (var state in allStates)
            {
                List<int> values = new List<int>();
                foreach (var legal in legalActions(state))
                {
                    values.Add(legal);
                }
                res.Add(state, values.Max());
            }
            return res;
        }

        static Dictionary<GameState, int> raidPolicyGen(HashSet<GameState> allStates)
        {
            Dictionary<GameState, int> res = new Dictionary<GameState, int>();
            foreach (var state in allStates)
            {
                List<int> values = new List<int>();
                foreach (var legal in legalActions(state))
                {
                    values.Add(legal);
                }
                res.Add(state, values.Min());
            }
            return res;
        }

        static Dictionary<GameState, int> bulletPlusPolicyGen(HashSet<GameState> allStates)
        {
            Dictionary<GameState, int> res = new Dictionary<GameState, int>();

            foreach (var state in allStates)
            {
                int d = state.Dice;
                List<int> taking = new List<int>();

                foreach (var legal in legalActions(state))
                {
                    // ignore null action
                    if (legal < 0) continue;

                    int myNext = Math.Min(state.MyFigs[legal] + d, p1.Length - 1);

                    foreach (var enemy in state.OppFigs)
                    {
                        if (p1[myNext] == p2[enemy])
                        {
                            taking.Add(legal);
                            break;
                        }
                    }
                }

                if (taking.Count > 0)
                {
                    res[state] = taking.Max();
                }
                else
                {
                    // fallback = normal bullet policy
                    res[state] = legalActions(state).Max();
                }
            }

            return res;
        }

        static Dictionary<GameState, int> raidPlusPolicyGen(HashSet<GameState> allStates)
        {
            Dictionary<GameState, int> res = new Dictionary<GameState, int>();

            foreach (var state in allStates)
            {
                int d = state.Dice;
                List<int> taking = new List<int>();

                foreach (var legal in legalActions(state))
                {
                    if (legal < 0) continue;

                    int myNext = Math.Min(state.MyFigs[legal] + d, p1.Length - 1);

                    foreach (var enemy in state.OppFigs)
                    {
                        if (p1[myNext] == p2[enemy])
                        {
                            taking.Add(legal);
                            break;
                        }
                    }
                }

                if (taking.Count > 0)
                {
                    res[state] = taking.Min();
                }
                else
                {
                    res[state] = legalActions(state).Max();
                }
            }

            return res;
        }

        static double HeuristicScore(GameState state)
        {
            return state.MyFigs.Sum() - state.OppFigs.Sum();
        }

        static Dictionary<GameState, int> scoreBasedPolicyGen(HashSet<GameState> allStates)
        {
            Dictionary<GameState, int> policy = new Dictionary<GameState, int>();

            foreach (var state in allStates)
            {
                // terminal states have no meaningful action
                if (isTerminal(state))
                {
                    policy[state] = -1;
                    continue;
                }

                double bestScore = double.NegativeInfinity;
                int bestAction = -1;

                foreach (int action in legalActions(state))
                {
                    double expectedScore = 0.0;

                    foreach (var transition in nextStates(state, action))
                    {
                        expectedScore += transition.Probability * HeuristicScore(transition.State);
                    }

                    if (expectedScore > bestScore)
                    {
                        bestScore = expectedScore;
                        bestAction = action;
                    }
                }

                policy[state] = bestAction;
            }

            return policy;
        }

        static List<Transition> playerChanceMoves(GameState state, int action)
        {
            return nextStochastic(state, action);
        }

        static List<Transition> opponentMinTransitions(GameState state)
        {
            List<int> legal = new List<int>();

            for (int i = 0; i < state.OppFigs.Length; i++)
            {
                int current = Math.Min(state.OppFigs[i] + state.Dice, p2.Length - 1);
                if (!(state.OppFigs.Contains(current) && current != p2.Length - 1)
                    && state.OppFigs[i] != p2.Length - 1)
                {
                    legal.Add(i);
                }
            }

            // No legal move → opponent skips, only dice happens
            if (legal.Count == 0)
            {
                List<Transition> skip = new List<Transition>();
                for (int d = 1; d <= diceSize; d++)
                {
                    skip.Add(new Transition(
                        new GameState(state.MyFigs.ToArray(),
                                      state.OppFigs.ToArray(),
                                      d),
                        1.0 / diceSize));
                }
                return skip;
            }

            double worstScore = double.PositiveInfinity;
            List<GameState> worstStates = new List<GameState>();

            foreach (int action in legal)
            {
                int[] newMy = state.MyFigs.ToArray();
                int[] newOpp = state.OppFigs.ToArray();

                newOpp[action] = Math.Min(state.OppFigs[action] + state.Dice, p2.Length - 1);

                for (int i = 0; i < newMy.Length; i++)
                    if (p1[newMy[i]] == p2[newOpp[action]])
                        newMy[i] = 0;

                Array.Sort(newMy);
                Array.Sort(newOpp);

                GameState afterMove = new GameState(newMy, newOpp, state.Dice);
                double score = HeuristicScore(afterMove);

                if (score < worstScore)
                {
                    worstScore = score;
                    worstStates.Clear();
                    worstStates.Add(afterMove);
                }
                else if (score == worstScore)
                {
                    worstStates.Add(afterMove);
                }
            }

            // Dice roll after opponent move
            List<Transition> result = new List<Transition>();
            foreach (var s in worstStates)
            {
                for (int d = 1; d <= diceSize; d++)
                {
                    result.Add(new Transition(
                        new GameState(s.MyFigs.ToArray(),
                                      s.OppFigs.ToArray(),
                                      d),
                        1.0 / (diceSize * worstStates.Count)));
                }
            }

            return result;
        }


        static double Expectiminimax(GameState state, int depth)
        {
            if (depth == 0 || isTerminal(state))
                return HeuristicScore(state);

            double best = double.NegativeInfinity;

            foreach (int action in legalActions(state))
            {
                double expected = 0.0;

                // Player move + dice
                foreach (var t in nextStochastic(state, action))
                {
                    // Opponent MIN + dice
                    foreach (var opp in opponentMinTransitions(t.State))
                    {
                        expected += t.Probability *
                                    opp.Probability *
                                    Expectiminimax(opp.State, depth - 1);
                    }
                }

                best = Math.Max(best, expected);
            }

            return best;
        }


        static Dictionary<GameState, int> minimaxPolicyGen(HashSet<GameState> allStates, int depth)
        {
            Dictionary<GameState, int> policy = new Dictionary<GameState, int>();

            foreach (var state in allStates)
            {
                if (isTerminal(state))
                {
                    policy[state] = -1;
                    continue;
                }

                double best = double.NegativeInfinity;
                int bestAction = -1;

                foreach (int action in legalActions(state))
                {
                    double expected = 0.0;

                    foreach (var t in nextStochastic(state, action))
                    {
                        foreach (var opp in opponentMinTransitions(t.State))
                        {
                            expected += t.Probability *
                                        opp.Probability *
                                        Expectiminimax(opp.State, depth - 1);
                        }
                    }

                    if (expected > best)
                    {
                        best = expected;
                        bestAction = action;
                    }
                }

                policy[state] = bestAction;
            }

            return policy;
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
            return $"[{string.Join(",", MyFigs)}]," +
           $"[{string.Join(",", OppFigs)}]," +
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
            return $"[{string.Join(",", State.MyFigs)}]," +
            $"[{string.Join(",", State.OppFigs)}]," +
            $"{State.Dice},Prob:{Probability}";
        }
    }
}
