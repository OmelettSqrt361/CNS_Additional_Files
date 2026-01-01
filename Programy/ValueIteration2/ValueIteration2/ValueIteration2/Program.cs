using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;

namespace ValueIteration2
{
    class Program
    {
        static int diceSize = 2;
        static int[] p1 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10};
        static int[] p2 = { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 11};


        // Diagnostic Data
        static int initializationTime;
        static int iterationTime;
        static int writeoutTime;
        static int numOfIter;

        static double gamma = 0.95f;
        static double epsilon = 1e-6;
        static Dictionary<GameState, double> V = new Dictionary<GameState, double>();

        static void Main()
        {
            // Test
            var startState = new GameState(new int[] { 0, 0, 0, 0}, new int[] { 0, 0, 0, 0}, 1);
            Console.WriteLine($"Start: {startState}");
            Console.WriteLine($"legal:[{string.Join(",", legalActions(startState))}]");
            Console.ReadLine();


            var inStopwatch = Stopwatch.StartNew();
            // Inicializace V
            Console.Clear();
            Console.WriteLine("Hledání všech stavů a inicializace \n");
            HashSet<GameState> allStates = searchAllStates(startState);
            foreach (var state in allStates)
            {
                V[state] = 0.0f;
            }
            PrintAllStates(allStates);

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
            PrintGameStateDictionary(optimalPolicyGen(allStates));

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
                if (!(state.MyFigs.Contains(current) && current != p1.Length - 1))
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
                    result.Add(new Transition(new GameState(state.MyFigs, state.OppFigs, i), 1 / (double)diceSize));
                }
                return result;
            }

            int[] newFigs = (int[])state.MyFigs.Clone();
            int[] newOpps = (int[])state.OppFigs.Clone();

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
                if (!(state.OppFigs.Contains(current) && current != p2.Length - 1))
                {
                    legal.Add(i);
                }
            }

            List<Transition> diceless = new List<Transition>();
            if (legal.Count == 0)
            {
                diceless.Add(new Transition(new GameState(state.MyFigs, state.OppFigs, -1), 1.0));
            }
            else
            {
                foreach (var action in legal)
                {
                    int[] newFigs = (int[])state.MyFigs.Clone();
                    int[] newOpps = (int[])state.OppFigs.Clone();
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
                    diced.Add(new Transition(new GameState(transition.State.MyFigs, transition.State.OppFigs, i), transition.Probability / (double)diceSize));
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
                visited.Add(new GameState(start.MyFigs, start.OppFigs, i));
                queue.Enqueue(new GameState(start.MyFigs, start.OppFigs, i));
            }

            int iteration = 0;
            while (queue.Count > 0)
            {
                iteration++;
                Console.WriteLine(iteration);
                var s = queue.Dequeue();

                if (isTerminal(s)) continue;

                foreach (int action in legalActions(s))
                {
                    foreach (var t in nextStates(s, action))
                    {
                        if (!visited.Contains(t.State))
                        {
                            visited.Add(t.State);
                            queue.Enqueue(t.State);
                        }
                    }
                }
            }

            return visited;
        }

        static double BellmanUpdate(GameState state)
        {
            double best = double.NegativeInfinity;

            foreach (int action in legalActions(state))
            {
                double q = 0.0f;
                foreach (var t in nextStates(state, action))
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
                    numOfIter = iteration;
                    break;
            }


        }

        static int BestAction(GameState state)
        {
            double best = double.NegativeInfinity;
            int bestAction = -1;

            foreach (int action in legalActions(state))
            {
                double q = 0.0;

                foreach (var t in nextStates(state, action))
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


    }

    public struct GameState : IEquatable<GameState>
    {
        // Inicializace potřebných proměnných
        // Proměnné MyFigs a OppFigs obsahují pouze "imaginární" hodnotu pole,
        // skutečné pole najdeme v array p1 nebo p2 jako p1[MyFigs[i]] nebo p2[OppFigs[i]]
        public readonly int[] MyFigs;
        public readonly int[] OppFigs;
        public readonly int Dice;

        public GameState(int[] myPieces, int[] oppPieces, int dice)
        {
            MyFigs = (int[])myPieces.Clone(); // prevent external mutation
            OppFigs = (int[])oppPieces.Clone();
            Dice = dice;
        }

        //Porovnávání stavů
        public bool Equals(GameState other)
        {
            if (MyFigs.Length != other.MyFigs.Length) return false;
            if (OppFigs.Length != other.OppFigs.Length) return false;

            for (int i = 0; i < MyFigs.Length; i++)
                if (MyFigs[i] != other.MyFigs[i]) return false;

            for (int i = 0; i < OppFigs.Length; i++)
                if (OppFigs[i] != other.OppFigs[i]) return false;

            if (Dice != other.Dice) return false;

            return true;
        }

        public override string ToString()
        {
            return $"P1:[{string.Join(",", MyFigs)}]," +
           $"P2:[{string.Join(",", OppFigs)}]," +
           $"D:{Dice}";
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
            return $"P1:[{string.Join(",", State.MyFigs)}]," +
            $"P2:[{string.Join(",", State.OppFigs)}]," +
            $"D:{State.Dice},Prob:{Probability}";
        }
    }
}
