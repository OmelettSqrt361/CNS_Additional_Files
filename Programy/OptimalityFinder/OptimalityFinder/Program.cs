using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OptimalityFinder
{
    class Program
    {
        static void Main(string[] args)
        {
            string folderPath = @"C:\Users\jakub\OneDrive\Plocha\Člověče nezlob se\CNS_Additional_Files\UnityHryJson";
            string strategyFolder = @"C:\Users\jakub\OneDrive\Plocha\Člověče nezlob se\CNS_Additional_Files\Strategie\Minimax";

            if (!Directory.Exists(folderPath) || !Directory.Exists(strategyFolder))
            {
                Console.WriteLine("One or both folder paths do not exist. Exiting.");
                return;
            }

            string[] gameFiles = Directory.GetFiles(folderPath, "*.json");

            var gameStats = new Dictionary<string, (int correctMoves, int totalMoves)>();

            foreach (var file in gameFiles)
            {
                Console.WriteLine($"Processing file: {Path.GetFileName(file)}");

                string gameJson = File.ReadAllText(file);
                GameData gameData;

                try
                {
                    gameData = JsonSerializer.Deserialize<GameData>(gameJson);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to parse JSON: {e.Message}");
                    continue;
                }

                if (gameData.Items == null || gameData.Items.Count < 3)
                {
                    Console.WriteLine("Not enough moves to determine player start, skipping.");
                    continue;
                }

                // Determine game type
                string firstKey = gameData.Items[0].Key;
                string gameType = firstKey.Split(':')[0];

                string strategyPath = Path.Combine(strategyFolder, $"{gameType}.json");
                if (!File.Exists(strategyPath))
                {
                    Console.WriteLine($"Strategy file not found for game type {gameType}, skipping.");
                    continue;
                }

                Dictionary<string, int> strategyData;
                try
                {
                    string strategyJson = File.ReadAllText(strategyPath);
                    strategyData = JsonSerializer.Deserialize<Dictionary<string, int>>(strategyJson);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"Failed to parse strategy JSON: {e.Message}");
                    continue;
                }

                string firstMoveKey = gameData.Items[1].Key;
                string secondMoveKey = gameData.Items[2].Key;

                string[] firstParts = firstMoveKey.Split(';');
                string[] secondParts = secondMoveKey.Split(';');

                bool computerStarted = firstParts[1] != secondParts[1];
                int playerMoveIndex = computerStarted ? 2 : 1;

                int totalMoves = 0;
                int correctMoves = 0;

                for (int i = playerMoveIndex; i < gameData.Items.Count; i += 2)
                {
                    string moveKey = gameData.Items[i].Key;
                    int moveValue = gameData.Items[i].Value;

                    totalMoves++;

                    if (strategyData.TryGetValue(moveKey, out int optimalValue))
                    {
                        if (moveValue == optimalValue)
                        {
                            correctMoves++;
                        }
                    }
                    else
                    {
                        Console.WriteLine($"Move key {moveKey} not found in strategy, counting as incorrect.");
                    }
                }

                if (gameStats.ContainsKey(gameType))
                {
                    var current = gameStats[gameType];
                    gameStats[gameType] = (current.correctMoves + correctMoves, current.totalMoves + totalMoves);
                }
                else
                {
                    gameStats[gameType] = (correctMoves, totalMoves);
                }
            }

            // Print average ratios per game type (player POV)
            Console.WriteLine("\n--- Average Correct Move Ratios Per Game Type (Player POV) ---");
            foreach (var kvp in gameStats)
            {
                string gameType = kvp.Key;
                int totalCorrect = kvp.Value.correctMoves;
                int totalMoves = kvp.Value.totalMoves;
                double averageRatio = totalMoves > 0 ? (double)totalCorrect / totalMoves : 0;

                Console.WriteLine($"Game Type: {gameType}, Total Correct Moves: {totalCorrect}, Total Moves: {totalMoves}, Average Ratio: {averageRatio:F5}");
            }


            Console.ReadLine();
            Console.WriteLine("\nProcessing completed.");
        }
    }

    public class GameData
    {
        [JsonPropertyName("items")]
        public List<Item> Items { get; set; }
    }

    public class Item
    {
        [JsonPropertyName("key")]
        public string Key { get; set; }

        [JsonPropertyName("value")]
        public int Value { get; set; }
    }
}
