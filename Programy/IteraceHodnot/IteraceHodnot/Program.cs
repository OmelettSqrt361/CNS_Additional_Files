using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IteraceHodnot
{
    class Program
    {
        static Random rng = new Random();
        static int size = 5;
        static int diceSize = 2;

        static int DiceRoll()
        {
            return rng.Next(1, diceSize + 1);
        }

        static void Main(string[] args)
        {
            var V = new Dictionary<GameState, double>(); // Hodnotová funkce pro všechny stavy
            var policy = new Dictionary<GameState, int>(); // Uložit optimální akci
            float gamma = 0.99f;
            double epsilon = 1e-6;

            var startState = new GameState(new int[] { -1, -1 }, new int[] { -1, -1 }, true, -1);
            V[startState] = 0.0;
            policy[startState] = -1;

            // Optionally, pre-enumerate all reachable states
            var queue = new Queue<GameState>();
            queue.Enqueue(startState);

            int iteration1 = 0;
            while (queue.Count > 0)
            {
                iteration1++;
                Console.WriteLine($"Hledání všech stavů: {iteration1}");

                var state = queue.Dequeue();

                List<Transition> nextStates;

                if (state.Dice == -1)
                {
                    // Generate all possible dice rolls
                    nextStates = new List<Transition>();
                    for (int d = 1; d <= diceSize; d++)
                    {
                        var nextState = new GameState(state.MainFigs, state.OppFigs, state.MyTurn, d);
                        nextStates.Add(new Transition(nextState, 1.0 / diceSize, 0, -1));
                    }
                }
                else
                {
                    // Generate all moves for this dice value
                    nextStates = new List<Transition>();
                    int[] moves = state.MyTurn ? LegalMoves(state) : LegalMovesOpp(state);

                    foreach (var move in moves)
                    {
                        var (nextState, reward) = Transition(state, move);
                        nextStates.Add(new Transition(nextState, 1.0, reward, move));
                    }
                }

                foreach (var t in nextStates)
                {
                    if (!V.ContainsKey(t.NextState))
                    {
                        V[t.NextState] = 0.0;
                        policy[t.NextState] = -1;
                        queue.Enqueue(t.NextState);
                    }
                }
            }

            Console.WriteLine($"Máme {V.Count} stavů");

            var transitionCache = new Dictionary<GameState, Dictionary<int, Transition[]>>();

            foreach (var state in V.Keys)
            {
                var transitions = GetTransitions(state);
                var grouped = new Dictionary<int, List<Transition>>();

                foreach (var t in transitions)
                {
                    if (!grouped.ContainsKey(t.Action))
                        grouped[t.Action] = new List<Transition>();
                    grouped[t.Action].Add(t);
                }
                transitionCache[state] = grouped.ToDictionary(k => k.Key, k => k.Value.ToArray());
            }

            int iteration2 = 0;
            bool converged;

            var states = V.Keys.ToArray();
            do
            {
                iteration2++;
                converged = true;

                double deltaMax = 0.0;
                foreach (var state in states)
                {
                    // Terminal states have fixed value
                    if (IsTerminal(state))
                        continue;

                    var transitions = transitionCache[state];
                    double bestValue = double.NegativeInfinity;
                    int bestAction = -1;

                    var transitionsPerAction = transitionCache[state];
                    foreach (var kvp in transitionsPerAction)
                    {
                        double q = 0.0;
                        foreach (var t in kvp.Value)
                            q += t.Probability * (t.Reward + gamma * V[t.NextState]);

                        if (q > bestValue)
                        {
                            bestValue = q;
                            bestAction = kvp.Key;
                        }
                    }

                    double delta = Math.Abs(V[state] - bestValue);
                    if (delta > deltaMax) deltaMax = delta;

                    V[state] = bestValue;
                    if (state.Dice != -1 && LegalMoves(state).Length > 0)
                    {
                        policy[state] = bestAction;
                    }
                }
                converged = deltaMax < epsilon;
                Console.WriteLine($"Iteration {iteration2}");
                Console.WriteLine($"DeltaMax: {deltaMax}");

            } while (!converged);

            Console.WriteLine("\nFinal state values:");
            foreach (var kvp in V)
            {
                Console.WriteLine($"State: {kvp.Key}, Value: {kvp.Value}, Optimal Action: {policy[kvp.Key]}");
            }
            Console.ReadLine();

        }

        //Vyhodnotí všechny legální tahy z dané pozice (pro člověče nezlob se pravidla)
        static int[] LegalMoves(GameState state)
        {
            // Inicializovat výlsendou množinu možných tahů
            var result = new List<int>();

            //Pro každou figurku určíme jestli se může hýbat v závislosti na následujících faktorech:
            // 1. Může se hýbat pouze pokud nepřešla přes hranici hrací plochy
            // 2. Pokud ji nepřekáží jiná figurka našeho hráče
            for (int i = 0; i < state.MainFigs.Length; i++)
            {
                int current = state.MainFigs[i];
                if (!state.MainFigs.Contains(current + state.Dice) && current <= size)
                    result.Add(i);
            }

            //Vrátit množinu možných tahů
            return result.ToArray();
        }

        static int[] LegalMovesOpp(GameState state)
        {
            // Totéž co 'int[] LegalMoves()', ale pro soupeře
            var result = new List<int>();
            for (int i = 0; i < state.OppFigs.Length; i++)
            {
                int current = state.OppFigs[i];
                if (!state.OppFigs.Contains(current + state.Dice) && current <= size)
                    result.Add(i);
            }
            return result.ToArray();
        }

        // Vyhodnotí další nastávající stav z akce
        // druhý output je terminace 0 = hra pokračuje; -1 = prohra; 1 = výhra
        static (GameState, int) Transition(GameState state, int action)
        {
            //Inicializovat figurky
            int[] newFigs = (int[])state.MainFigs.Clone();
            int[] newOpps = (int[])state.OppFigs.Clone();

            // Zjisit je-li hra ukončená
            bool lost = newFigs.All(f => f > size);
            bool won = newOpps.All(f => f > size);

            int result = 0;
            if (won) { result = 1; }
            if (lost) { result = -1; }

            // Nastavit hodnotu herního stavu
            var v = state;

            // Vyhodnotit, pouze pokud stav není vyhrán či prohrán
            if (!won && !lost) {

                // Aktualizovat pozici pohlé figurky
                if (state.MyTurn) { newFigs[action] = Math.Min(state.Dice + state.MainFigs[action], size + 1); }
                else { newOpps[action] = Math.Min(state.Dice + state.OppFigs[action], size + 1); }

                // Aktualizovat pozice sebraných figurek, pokud nějaké jsou
                // Rozděleno na dvě větve, podle toho který z hráčů se hýbe
                if (state.OppFigs.Contains(newFigs[action]) && state.MyTurn)
                {
                    for (int i = 0; i < state.OppFigs.Length; i++)
                    {
                        if (state.OppFigs[i] == newFigs[action] && state.OppFigs[i] <= size && newFigs[action] > 0)
                        {
                            newOpps[i] = -1;
                        }
                    }
                } else if (state.MainFigs.Contains(newOpps[action]) && !state.MyTurn)
                {
                    for (int i = 0; i < state.OppFigs.Length; i++)
                    {
                        if (newOpps[action] == state.MainFigs[i] && newOpps[action] <= size && newOpps[action] > 0)
                        {
                            newFigs[i] = -1;
                        }
                    }
                }



                // Zbavit se permutační symetrie
                Array.Sort(newOpps);
                Array.Sort(newFigs);

                // Inicializovat nový herní stav
                v = new GameState(newFigs, newOpps, !state.MyTurn, state.Dice);
            }

            // Vrátit seřazené hodnoty
            return (v, result);
        }

        static List<Transition> GetTransitions(GameState state)
        {
            var transitions = new List<Transition>();

            if (IsTerminal(state))
            {
                transitions.Add(new Transition(state, 1.0, 0, -1));
                return transitions;
            }

            if (state.Dice == -1)
            {
                for (int d = 1; d <= diceSize; d++)
                {
                    var nextState = new GameState(state.MainFigs, state.OppFigs, state.MyTurn, d);
                    transitions.Add(new Transition(nextState, 1.0 / diceSize, 0, -1));
                }
                return transitions;
            }
            var moves = state.MyTurn ? LegalMoves(state) : LegalMovesOpp(state);
            foreach (var move in moves)
            {
                var (nextState, reward) = Transition(state, move);
                double prob = state.MyTurn ? 1.0 : 1.0 / moves.Length; // Random opponent
                transitions.Add(new Transition(nextState, prob, reward, move));
            }

            return transitions;
        }

        static int RandomOpponent(GameState state)
        {
            // Najít všechny možné validní tahy a uložit je do seznamu result
            var result = new List<int>();
            for (int i = 0; i < state.OppFigs.Length; i++)
            {
                int current = state.OppFigs[i];
                if (!state.OppFigs.Contains(current + state.Dice) && current <= size)
                    result.Add(i);
            }

            // Vybrat náhodně hodnotu z result a vrátit ji jako output
            int choice = result[rng.Next(0, result.Count)];
            return choice;
        }

        static bool IsTerminal(GameState s)
        {
            return s.MainFigs.All(f => f > size) ||
                   s.OppFigs.All(f => f > size);
        }
    }


    // Struct pro ukládání stavů
    public struct GameState : IEquatable<GameState>
    {
        // Inicializace potřebných proměnných
        public readonly int[] MainFigs;
        public readonly int[] OppFigs;
        public readonly bool MyTurn;
        public readonly int Dice;

        public GameState(int[] myPieces, int[] oppPieces, bool turn, int dice)
        {
            MainFigs = (int[])myPieces.Clone(); // prevent external mutation
            OppFigs = (int[])oppPieces.Clone();
            MyTurn = turn;
            Dice = dice;
        }


        //Porovnávání stavů
        public bool Equals(GameState other)
        {
            if (MyTurn != other.MyTurn || Dice != other.Dice) return false;
            if (MainFigs.Length != other.MainFigs.Length) return false;
            if (OppFigs.Length != other.OppFigs.Length) return false;

            for (int i = 0; i < MainFigs.Length; i++)
                if (MainFigs[i] != other.MainFigs[i]) return false;

            for (int i = 0; i < OppFigs.Length; i++)
                if (OppFigs[i] != other.OppFigs[i]) return false;

            return true;
        }

        public override int GetHashCode()
        {
            int hash = Dice * 2;
            if (!MyTurn){hash++;}
            foreach (var p in MainFigs) hash = hash * 67 + p;
            foreach (var p in OppFigs) hash = hash * 67 + p;
            return hash;
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", MainFigs)}]," +
           $"[{string.Join(", ", OppFigs)}]," +
           $"{MyTurn},{Dice}";
        }
    }

    public struct Transition
    {
        public readonly GameState NextState;
        public readonly double Probability;
        public readonly int Reward;
        public readonly int Action;

        public Transition(GameState nextState, double probability, int reward, int action)
        {
            NextState = nextState;
            Probability = probability;
            Reward = reward;
            Action = action;
        }
    }
}
