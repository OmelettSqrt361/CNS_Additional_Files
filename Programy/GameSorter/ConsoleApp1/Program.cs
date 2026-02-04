using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Text.Json;

class Program
{
    static void Main(string[] args)
    {
        string folderPath = @"C:\Users\jakub\OneDrive\Plocha\Člověče nezlob se\CNS_Additional_Files\UnityHryJson";

        Dictionary<StatKey, StatValue> stats =
            new Dictionary<StatKey, StatValue>();

        foreach (string file in Directory.GetFiles(folderPath, "*.json"))
        {
            string json = File.ReadAllText(file);
            JsonDocument doc = JsonDocument.Parse(json);

            List<JsonElement> items =
                doc.RootElement.GetProperty("items")
                               .EnumerateArray()
                               .ToList();

            if (items.Count < 2)
                continue;

            string firstKey =
                items[0].GetProperty("key").GetString() ?? "";

            string[] parts = firstKey.Split(new[] { ':' }, 2);
            string gameName = parts[0];
            string strategyName =
                parts.Length > 1 ? parts[1] : "UNKNOWN";

            string secondKey =
                items[2].GetProperty("key").GetString() ?? "";

            bool computerStarted = DidComputerStart(secondKey);

            int moveCount = items.Count - 1;

            bool starterWon = (moveCount % 2 == 1);

            bool computerWon;
            if (starterWon)
            {
                computerWon = computerStarted;
            }
            else
            {
                computerWon = !computerStarted;
            }

            StatKey key =
                new StatKey(gameName, strategyName, computerStarted);

            StatValue value;
            if (!stats.TryGetValue(key, out value))
            {
                value = new StatValue();
                stats[key] = value;
            }

            value.GamesPlayed++;
            if (computerWon)
                value.ComputerWins++;
            else
                value.PlayerWins++;
        }

        Console.WriteLine("Hra;Strategie;ZačalPočítač;Odehraných her;Výhry Člověk;Výhry Počítač;Winrate");

        foreach (KeyValuePair<StatKey, StatValue> entry in
            stats.OrderBy(e => e.Key.Game)
                 .ThenBy(e => e.Key.Strategy)
                 .ThenBy(e => e.Key.ComputerStarted))
        {
            StatKey k = entry.Key;
            StatValue v = entry.Value;

            double playerWinRate =
                v.GamesPlayed == 0
                ? 0.0
                : (double)v.PlayerWins / v.GamesPlayed;

            Console.WriteLine(
            "{0};{1};{2};{3};{4};{5};{6}",
            k.Game,
            k.Strategy,
            k.ComputerStarted,
            v.GamesPlayed,
            v.PlayerWins,
            v.ComputerWins,
            playerWinRate
            );

        }

        Console.ReadLine();
    }

    static bool DidComputerStart(string key)
    {
        string[] parts = key.Split(';');
        if (parts.Length < 2)
            return false;

        int[] secondArray = ParseArray(parts[1]);
        return secondArray.Any(v => v != 0);
    }

    static int[] ParseArray(string s)
    {
        s = s.Trim('[', ']');

        string[] parts = s.Split(',');
        int[] result = new int[parts.Length];

        for (int i = 0; i < parts.Length; i++)
        {
            int value;
            if (!int.TryParse(parts[i], out value))
                value = 0;

            result[i] = value;
        }

        return result;
    }
}

// ---------- Helper Classes ----------

class StatKey
{
    public string Game { get; private set; }
    public string Strategy { get; private set; }
    public bool ComputerStarted { get; private set; }

    public StatKey(string game, string strategy, bool computerStarted)
    {
        Game = game;
        Strategy = strategy;
        ComputerStarted = computerStarted;
    }

    public override bool Equals(object obj)
    {
        StatKey other = obj as StatKey;
        if (other == null)
            return false;

        return Game == other.Game &&
               Strategy == other.Strategy &&
               ComputerStarted == other.ComputerStarted;
    }

    public override int GetHashCode()
    {
        unchecked
        {
            int hash = 17;
            hash = hash * 23 + (Game != null ? Game.GetHashCode() : 0);
            hash = hash * 23 + (Strategy != null ? Strategy.GetHashCode() : 0);
            hash = hash * 23 + ComputerStarted.GetHashCode();
            return hash;
        }
    }
}

class StatValue
{
    public int GamesPlayed;
    public int PlayerWins;
    public int ComputerWins;
}
