using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;

namespace PolicyComparator
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Enter policy file paths (one per line).");
            Console.WriteLine("Press ENTER on an empty line to finish.\n");

            List<string> paths = new List<string>();

            while (true)
            {
                string line = Console.ReadLine();
                if (string.IsNullOrWhiteSpace(line))
                    break;

                paths.Add(line);
            }

            if (paths.Count < 2)
            {
                Console.WriteLine("Need at least two policies.");
                return;
            }

            // Load policies
            var policies = new Dictionary<string, Dictionary<string, int>>();

            foreach (string path in paths)
            {
                try
                {
                    string json = File.ReadAllText(path);
                    policies[path] = JsonSerializer.Deserialize<Dictionary<string, int>>(json);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to load {path}: {ex.Message}");
                    return;
                }
            }

            // Print table header
            Console.Write("\t");
            foreach (string name in paths)
                Console.Write(Path.GetFileName(name) + "\t");
            Console.WriteLine();

            // Compute similarity matrix
            foreach (string p1 in paths)
            {
                Console.Write(Path.GetFileName(p1) + "\t");

                foreach (string p2 in paths)
                {
                    double similarity = ComputeSimilarity(policies[p1], policies[p2]);
                    Console.Write($"{similarity:F3}\t");
                }

                Console.WriteLine();
            }

            Console.ReadLine();
        }

        static double ComputeSimilarity(
            Dictionary<string, int> a,
            Dictionary<string, int> b)
        {
            int same = 0;
            int different = 0;

            foreach (var kvp in a)
            {
                if (b.TryGetValue(kvp.Key, out int value))
                {
                    if (value == kvp.Value)
                        same++;
                    else
                        different++;
                }
                else
                {
                    different++;
                }
            }

            foreach (var key in b.Keys)
            {
                if (!a.ContainsKey(key))
                    different++;
            }

            return (same + different) == 0
                ? 1.0
                : (double)same / (same + different);
        }
    }
}
