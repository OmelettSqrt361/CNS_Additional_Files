using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avrg
{
    class Program
    {
        class AvrgStater
        {
            // Násobení vektoru maticí
            static double[] Mult(double[] x, double[,] M)
            {
                int n = x.Length;
                double[] result = new double[n];
                for (int i = 0; i < n; i++)
                {
                    result[i] = 0;
                    for (int j = 0; j < n; j++)
                    {
                        result[i] += x[j] * M[j, i];
                    }
                }
                return result;
            }

            // Hadamardův součin dvou vektorů
            static double[] Hada(double[] x, double[] y)
            {
                int n = x.Length;
                double[] result = new double[n];
                for (int i = 0; i < n; i++)
                {
                    result[i] = x[i] * y[i];
                }
                return result;
            }

            // Součin v rámci jednoho vektoru
            static double Sum(double[] v)
            {
                return v.Sum();
            }

            // Pasivní krok
            // pass(x, y, x_null, M) = x - hada(x, mult(y, M)) + (sum(hada(x,y*M)) * x_null)
            static double[] Pass(double[] x, double[] y, double[] x_null, double[,] M)
            {
                int n = x.Length;
                double[] yM = Mult(y, M);
                double[] xHada = Hada(x, yM);
                double[] result = new double[n];
                double sumValue = Sum(xHada);

                for (int i = 0; i < n; i++)
                {
                    result[i] = x[i] - xHada[i] + sumValue * x_null[i];
                }
                return result;
            }

            // Generování matice
            static double[,] CirculantMatrix(double[] d)
            {
                int n = d.Length;
                double[,] T = new double[n, n];
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        T[i, j] = d[(j - i + n) % n];
                    }
                }
                return T;
            }

            // Akumulující matice
            static double[,] AccumulatingMatrix(double[] d)
            {
                int n = d.Length;
                double[,] T = new double[n, n];
                for (int i = 0; i < n; i++)
                {
                    for (int j = 0; j < n; j++)
                    {
                        if (i > j)
                        {
                            T[i, j] = 0;
                        }
                        else if (j == n - 1)
                        {
                            for (int k = 0; k <= i; k++)
                            {
                                T[i, j] += d[n - k - 1];
                            }
                        }
                        else
                        {
                            T[i, j] = d[(j - i + n) % n];
                        }
                    }
                }
                return T;
            }

            static void PrintMatrix(double[,] matrix)
            {
                int rows = matrix.GetLength(0);
                int cols = matrix.GetLength(1);

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        Console.WriteLine($"{i}  {j}  {Math.Sqrt(matrix[i, j]),0:F8}"); // format for alignment & precision
                    }
                    Console.WriteLine();
                }
            }

            static void PrintMatrixDouble(double[,] matrix, double[,] matrix2)
            {
                int rows = matrix.GetLength(0);
                int cols = matrix.GetLength(1);

                for (int i = 0; i < rows; i++)
                {
                    for (int j = 0; j < cols; j++)
                    {
                        Console.WriteLine($"{i}  {j}  {Math.Sqrt(matrix[i, j]),0:F8}  0  {Math.Sqrt(matrix2[i, j]),0:F8}"); // format for alignment & precision
                    }
                    Console.WriteLine();
                }
            }


            static double[] DiceMaker(int S, int dSize)
            {
                double[] dice = new double[S + 1];
                for (int i = 0; i <= dSize; i++)
                {
                    dice[i] = (1.0 / dSize);
                }
                dice[0] = 0;
                return dice;
            }

            static double[] GetAvrg(double[] d, int iterations)
            {

                // Generování cirkulantní matice z kostky
                double[,] T = AccumulatingMatrix(d);

                // Inicializace počátečních hodnot vektorů
                double[] a = new double[d.Length + 1];
                double[] b = new double[d.Length + 1];
                double[] a_null = new double[d.Length + 1];
                double[] b_null = new double[d.Length + 1];

                // Počítání průměrné potřeby tahů
                double[] a_hist = new double[iterations];
                double[] b_hist = new double[iterations];

                //Nastavení hodnot
                a[0] = 1;
                b[0] = 1;
                a_null[0] = 1;
                b_null[0] = 1;

                int step = 0;

                // Dostat T_a, T_b
                double[,] T_a = new double[d.Length + 1, d.Length + 1];
                double[,] T_b = new double[d.Length + 1, d.Length + 1];

                for (int i = 0; i < d.Length; i++)
                {
                    for (int j = 0; j < d.Length; j++)
                    {
                        T_a[i, j] = T[i, j];

                        if (j == d.Length - 1 && i == d.Length - 1)
                        {
                            T_b[i + 1, j + 1] = T[i, j];
                        }
                        else if (j == d.Length - 1)
                        {
                            T_b[i, j + 1] = T[i, j];
                        }
                        else if (i == d.Length - 1)
                        {
                            T_b[i + 1, j] = T[i, j];
                        }
                        else
                        {
                            T_b[i, j] = T[i, j];
                        }
                    }
                }


                // Hlavní loop
                for (int iter = 0; iter < iterations; iter++)
                {

                    a_hist[step] = a[d.Length - 1];
                    b_hist[step] = b[d.Length];

                    if (step % 2 == 0)
                    {
                        a = Mult(a, T_a);
                        b = Pass(b, a, b_null, T_a);
                    }
                    else
                    {
                        a = Pass(a, b, a_null, T_b);
                        b = Mult(b, T_b);
                    }
                    step++;
                }

                double a_avrg = 0;
                double b_avrg = 0;

                for (int i = 1; i < iterations; i++)
                {
                    a_avrg += (a_hist[i] - a_hist[i - 1]);
                    b_avrg += (b_hist[i] - b_hist[i - 1]);
                }

                double[] avrgs = new double[2];
                avrgs[0] = a_avrg;
                avrgs[1] = b_avrg;

                return avrgs;
            }

            static double[,] GetAB(double[] d, int iterations, bool isA)
            {

                // Generování cirkulantní matice z kostky
                double[,] T = AccumulatingMatrix(d);

                // Inicializace počátečních hodnot vektorů
                double[] a = new double[d.Length + 1];
                double[] b = new double[d.Length + 1];
                double[] a_null = new double[d.Length + 1];
                double[] b_null = new double[d.Length + 1];

                // Počítání průměrné potřeby tahů
                double[] a_hist = new double[iterations];
                double[] b_hist = new double[iterations];

                //Nastavení hodnot
                a[0] = 1;
                b[0] = 1;
                a_null[0] = 1;
                b_null[0] = 1;

                int step = 0;

                // Dostat T_a, T_b
                double[,] T_a = new double[d.Length + 1, d.Length + 1];
                double[,] T_b = new double[d.Length + 1, d.Length + 1];

                for (int i = 0; i < d.Length; i++)
                {
                    for (int j = 0; j < d.Length; j++)
                    {
                        T_a[i, j] = T[i, j];

                        if (j == d.Length - 1 && i == d.Length - 1)
                        {
                            T_b[i + 1, j + 1] = T[i, j];
                        }
                        else if (j == d.Length - 1)
                        {
                            T_b[i, j + 1] = T[i, j];
                        }
                        else if (i == d.Length - 1)
                        {
                            T_b[i + 1, j] = T[i, j];
                        }
                        else
                        {
                            T_b[i, j] = T[i, j];
                        }
                    }
                }


                double[,] hist_a = new double[iterations, d.Length + 1];
                double[,] hist_b = new double[iterations, d.Length + 1];

                // Hlavní loop
                for (int iter = 0; iter < iterations; iter++)
                {

                    for (int i = 0; i < d.Length + 1; i++)
                    {
                        hist_a[step, i] = a[i];
                        hist_b[step, i] = b[i];
                    }

                    a_hist[step] = a[d.Length - 1];
                    b_hist[step] = b[d.Length];

                    if (step % 2 == 0)
                    {
                        a = Mult(a, T_a);
                        b = Pass(b, a, b_null, T_a);
                    }
                    else
                    {
                        a = Pass(a, b, a_null, T_b);
                        b = Mult(b, T_b);
                    }
                    step++;
                }

                if (isA)
                {
                    return hist_a;
                } else
                {
                    return hist_b;
                }
            }

            // Hlavní loop hry
            // Tady je i počáteční nastavení
            static void Main(string[] args)
            {

                // Dostávání inputů
                // d = kostkový vektor
                int dSize;
                Console.Write("Určete velikost kostky: ");
                dSize = int.Parse(Console.ReadLine());

                // maxS - velikost pole
                Console.Write("Do jakého pole chceš výsledky: ");
                int maxS = int.Parse(Console.ReadLine());

                // iterations
                Console.Write("Kolik iterací mám udělat: ");
                int iterations = int.Parse(Console.ReadLine());

                bool isA = false;
                bool notDone = true;
                while (notDone)
                {
                    // A nebo B
                    Console.Write("A nebo B: ");
                    string anebob = Console.ReadLine();
                    if (anebob == "A")
                    {
                        isA = true;
                        notDone = false;
                    }
                    else if (anebob == "B")
                    {
                        isA = false;
                        notDone = false;
                    }
                    else
                    {
                        Console.WriteLine("Špatný input");
                    }
                }

                Console.Clear();
                double[,] a = GetAB(DiceMaker(maxS, dSize), iterations, true);
                double[,] b = GetAB(DiceMaker(maxS, dSize), iterations, false);

                PrintMatrixDouble(a, b);

                Console.ReadLine();
            }
        }
    }
}
