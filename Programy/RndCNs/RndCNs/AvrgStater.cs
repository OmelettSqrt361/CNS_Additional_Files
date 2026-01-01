using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace AvrgStater
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
                    if(i > j)
                    {
                        T[i, j] = 0;
                    } else if (j==n-1)
                    {
                        for (int k = 0; k <= i; k++)
                        {
                            T[i, j] += d[n-k-1];
                        }
                    } else
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

            Console.WriteLine("Matrix:");
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    Console.Write($"{matrix[i, j],8:F3} "); // format for alignment & precision
                }
                Console.WriteLine();
            }
        }


        static double[] GetAvrg(int S, double[] d, int iterations)
        {
            // Inicializace počátečních hodnot vektorů
            double[] a = new double[S + 1];
            double[] b = new double[S + 1];
            double[] a_null = new double[S + 1];
            double[] b_null = new double[S + 1];

            // Počítání průměrné potřeby tahů
            double[] a_hist = new double[iterations];
            double[] b_hist = new double[iterations];

            //Nastavení hodnot
            a[0] = 1;
            b[0] = 1;
            a_null[0] = 1;
            b_null[0] = 1;

            int step = 0;

            // Generování cirkulantní matice z kostky
            double[,] T = AccumulatingMatrix(d);

            // Dostat T_a, T_b
            double[,] T_a = new double[S + 1, S + 1];
            double[,] T_b = new double[S + 1, S + 1];

            for (int i = 0; i < S; i++)
            {
                for (int j = 0; j < S; j++)
                {
                    T_a[i, j] = T[i, j];

                    if (j == S - 1 && i == S - 1)
                    {
                        T_b[i + 1, j + 1] = T[i, j];
                    }
                    else if (j == S - 1)
                    {
                        T_b[i, j + 1] = T[i, j];
                    }
                    else if (i == S - 1)
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

                a_hist[step] = a[S - 1];
                b_hist[step] = b[S];

                Console.WriteLine("A: [" + string.Join(", ", a.Select(x => x.ToString("F3"))) + "]");
                Console.WriteLine("B: [" + string.Join(", ", b.Select(x => x.ToString("F3"))) + "]");
                Console.WriteLine("");

                output = output + string.Join(" ", a.Select(x => x.ToString("F3"))) + "\n" + string.Join(" ", b.Select(x => x.ToString("F3"))) + "\n";

                if (step % 2 == 0)
                {
                    a = Mult(a, T_a);
                    b = Pass(b, a, b_null, T_b);
                }
                else
                {
                    a = Pass(a, b, a_null, T_a);
                    b = Mult(b, T_b);
                }
                step++;
            }

            double a_avrg = 0;
            double b_avrg = 0;

            for (int i = 1; i < iterations; i++)
            {
                a_avrg += i * (a_hist[i] - a_hist[i - 1]);
                b_avrg += i * (b_hist[i] - b_hist[i - 1]);
            }
        }
 
        // Hlavní loop hry
        // Tady je i počáteční nastavení
        static void Main(string[] args)
        {

            // Dostávání inputů
            // d = kostkový vektor
            double[] d = new double[S];
            Console.WriteLine($"Vložte {S} hodnot pro kostkový vektor:");
            string[] parts = Console.ReadLine().Split(' ');
            for (int i = 0; i < S; i++)
            {
                if (parts.Length > i)
                {
                    d[i] = double.Parse(parts[i]);
                }
            }

            // iterations = Počet iterací, které kód vyvolá
            Console.Write("Do jakého pole chceš výsledky: ");
            int maxS = int.Parse(Console.ReadLine());


            
            Console.ReadLine();
        }
    }
}
