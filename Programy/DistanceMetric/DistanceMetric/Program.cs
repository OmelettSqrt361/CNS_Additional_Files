using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace DistanceMetric
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Paste your Excel distance table (tab-separated), then an empty line to finish:");

            // Read lines until empty line
            var lines = new List<string>();
            string input;
            while (!string.IsNullOrWhiteSpace(input = Console.ReadLine()))
                lines.Add(input);

            if (lines.Count < 2)
            {
                Console.WriteLine("Not enough data.");
                return;
            }

            // Parse labels
            string[] labels = lines[0].Split('\t').Skip(1).ToArray();
            int n = labels.Length;

            // Parse distance matrix
            double[,] D = new double[n, n];
            for (int i = 1; i <= n; i++)
            {
                var parts = lines[i].Split('\t');
                for (int j = 1; j <= n; j++)
                {
                    D[i - 1, j - 1] = double.Parse(parts[j].Replace(',', '.'), CultureInfo.InvariantCulture);
                }
            }

            // Perform classical MDS
            double[,] X = ClassicalMDS(D, 2);

            // Output results
            Console.WriteLine("\n2D coordinates:");
            for (int i = 0; i < n; i++)
            {
                Console.WriteLine($"{labels[i]}:\tX={X[i, 0]:F4}, Y={X[i, 1]:F4}");
            }
            Console.ReadLine();
        }

        // Classical MDS with manual eigen-decomposition
        static double[,] ClassicalMDS(double[,] D, int dimensions)
        {
            int n = D.GetLength(0);
            double[,] D2 = new double[n, n];

            // Step 1: square distances
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    D2[i, j] = D[i, j] * D[i, j];

            // Step 2: double center
            double[] rowMeans = new double[n];
            double[] colMeans = new double[n];
            double totalMean = 0.0;

            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                {
                    rowMeans[i] += D2[i, j];
                    colMeans[j] += D2[i, j];
                    totalMean += D2[i, j];
                }

            for (int i = 0; i < n; i++)
            {
                rowMeans[i] /= n;
                colMeans[i] /= n;
            }
            totalMean /= (n * n);

            // Step 3: build B matrix
            double[,] B = new double[n, n];
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    B[i, j] = -0.5 * (D2[i, j] - rowMeans[i] - colMeans[j] + totalMean);

            // Step 4: compute eigenvalues and eigenvectors manually
            JacobiEigen(B, out double[] eigenValues, out double[,] eigenVectors);

            // Step 5: keep only positive eigenvalues and sort descending
            var eigenPairs = new List<(double Lambda, double[] Vec)>();
            for (int i = 0; i < n; i++)
            {
                if (eigenValues[i] > 1e-10)
                {
                    double[] vec = new double[n];
                    for (int j = 0; j < n; j++)
                        vec[j] = eigenVectors[j, i];
                    eigenPairs.Add((eigenValues[i], vec));
                }
            }
            eigenPairs.Sort((a, b) => b.Lambda.CompareTo(a.Lambda));

            // Step 6: compute coordinates
            int dims = Math.Min(dimensions, eigenPairs.Count);
            double[,] X = new double[n, dims];
            for (int d = 0; d < dims; d++)
            {
                double lambda = eigenPairs[d].Lambda;
                var vec = eigenPairs[d].Vec;
                for (int i = 0; i < n; i++)
                    X[i, d] = vec[i] * Math.Sqrt(lambda);
            }

            return X;
        }

        // Jacobi method for symmetric eigen-decomposition
        static void JacobiEigen(double[,] A, out double[] eigenValues, out double[,] eigenVectors, int maxIter = 100, double tol = 1e-10)
        {
            int n = A.GetLength(0);
            eigenValues = new double[n];
            eigenVectors = new double[n, n];

            // Initialize eigenvectors as identity
            for (int i = 0; i < n; i++)
                for (int j = 0; j < n; j++)
                    eigenVectors[i, j] = (i == j) ? 1.0 : 0.0;

            double[,] a = (double[,])A.Clone();

            for (int iter = 0; iter < maxIter; iter++)
            {
                // Find largest off-diagonal element
                double max = 0.0;
                int p = 0, q = 1;
                for (int i = 0; i < n; i++)
                    for (int j = i + 1; j < n; j++)
                        if (Math.Abs(a[i, j]) > max)
                        {
                            max = Math.Abs(a[i, j]);
                            p = i;
                            q = j;
                        }

                if (max < tol)
                    break; // convergence

                double phi = 0.5 * Math.Atan2(2 * a[p, q], a[q, q] - a[p, p]);
                double c = Math.Cos(phi);
                double s = Math.Sin(phi);

                // Rotate matrix
                double app = c * c * a[p, p] - 2 * s * c * a[p, q] + s * s * a[q, q];
                double aqq = s * s * a[p, p] + 2 * s * c * a[p, q] + c * c * a[q, q];
                a[p, p] = app;
                a[q, q] = aqq;
                a[p, q] = a[q, p] = 0.0;

                for (int i = 0; i < n; i++)
                {
                    if (i != p && i != q)
                    {
                        double aip = c * a[i, p] - s * a[i, q];
                        double aiq = s * a[i, p] + c * a[i, q];
                        a[i, p] = a[p, i] = aip;
                        a[i, q] = a[q, i] = aiq;
                    }

                    // Rotate eigenvectors
                    double vip = c * eigenVectors[i, p] - s * eigenVectors[i, q];
                    double viq = s * eigenVectors[i, p] + c * eigenVectors[i, q];
                    eigenVectors[i, p] = vip;
                    eigenVectors[i, q] = viq;
                }
            }

            // Eigenvalues are diagonal elements
            for (int i = 0; i < n; i++)
                eigenValues[i] = a[i, i];
        }
    }
}
