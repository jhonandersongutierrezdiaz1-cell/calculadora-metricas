
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace BadCalcVeryBad
{
    public class Calculator
    {
        private static readonly Random rnd = new Random();

        /// <summary>
        /// Realiza la operación solicitada. Devuelve double.NaN si la operación no es válida.
        /// </summary>
        public static double Execute(string aStr, string bStr, string op)
        {
            if (!TryParseInvariant(aStr, out double a)) a = 0;
            if (!TryParseInvariant(bStr, out double b)) b = 0;

            switch (op)
            {
                case "+":
                    return a + b;
                case "-":
                    return a - b;
                case "*":
                    return a * b;
                case "/":
                    if (Math.Abs(b) < double.Epsilon) return double.NaN; // división por cero -> NaN
                    return a / b;
                case "^":
                    // usar Math.Pow para exponentes reales
                    return Math.Pow(a, b);
                case "%":
                    if (Math.Abs(b) < double.Epsilon) return double.NaN;
                    return a % b;
                case "sqrt":
                    if (a < 0) return double.NaN;
                    return Math.Sqrt(a);
                default:
                    return double.NaN;
            }
        }

        private static bool TryParseInvariant(string s, out double value)
        {
            value = 0;
            if (s == null) return false;
            s = s.Trim().Replace(',', '.'); // aceptar coma o punto
            return double.TryParse(s, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out value);
        }
    }

    class Program
    {
        private const string HistoryFile = "history.txt";
        private const long MaxHistoryBytes = 1_000_000; // 1 MB

        static void Main(string[] args)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var history = LoadHistory();

            Console.WriteLine("BAD CALC - Fixed & safe edition");
            Console.WriteLine("Opciones: 1)add  2)sub  3)mul  4)div  5)pow  6)mod  7)sqrt  8)show history 0)exit");

            while (true)
            {
                Console.Write("opt: ");
                var opt = Console.ReadLine()?.Trim();
                if (string.IsNullOrEmpty(opt)) continue;

                if (opt == "0") break;

                try
                {
                    if (opt == "8")
                    {
                        ShowHistory(history);
                        continue;
                    }

                    string a = "0", b = "0", op = "";

                    if (opt == "7") // sqrt -> solo un operando (a)
                    {
                        Console.Write("a: ");
                        a = Console.ReadLine();
                        op = "sqrt";
                    }
                    else
                    {
                        Console.Write("a: ");
                        a = Console.ReadLine();
                        Console.Write("b: ");
                        b = Console.ReadLine();

                        op = opt switch
                        {
                            "1" => "+",
                            "2" => "-",
                            "3" => "*",
                            "4" => "/",
                            "5" => "^",
                            "6" => "%",
                            _ => ""
                        };
                    }

                    if (string.IsNullOrEmpty(op))
                    {
                        Console.WriteLine("Opción no válida.");
                        continue;
                    }

                    double result = Calculator.Execute(a, b, op);

                    if (double.IsNaN(result) || double.IsInfinity(result))
                    {
                        Console.WriteLine("Resultado inválido (NaN o infinito). Revisa los operandos y la operación.");
                    }
                    else
                    {
                        var resultStr = result.ToString("G17", CultureInfo.InvariantCulture);
                        Console.WriteLine("= " + resultStr);

                        var entry = $"{DateTime.UtcNow:O} | {a} | {b} | {op} | {resultStr}";
                        history.Add(entry);
                        AppendHistory(entry);
                    }
                }
                catch (Exception ex)
                {
                    // Mostramos el error para depuración; no ocultamos excepciones silenciosamente.
                    Console.WriteLine("Ocurrió un error: " + ex.Message);
                }
            }

            Console.WriteLine("Saliendo...");

            // Guardar historial final (opcional)
            TryRotateHistoryIfNeeded();
        }

        static List<string> LoadHistory()
        {
            try
            {
                if (!File.Exists(HistoryFile)) return new List<string>();
                var lines = File.ReadAllLines(HistoryFile, Encoding.UTF8);
                return new List<string>(lines);
            }
            catch
            {
                // Si hay problema leyendo el archivo, devolvemos historial vacío
                return new List<string>();
            }
        }

        static void ShowHistory(List<string> history)
        {
            if (history.Count == 0)
            {
                Console.WriteLine("[No hay historial todavía]");
                return;
            }

            Console.WriteLine("---- Historial (más reciente abajo) ----");
            foreach (var line in history)
            {
                Console.WriteLine(line);
            }
            Console.WriteLine("----------------------------------------");
        }

        static void AppendHistory(string line)
        {
            try
            {
                File.AppendAllText(HistoryFile, line + Environment.NewLine, Encoding.UTF8);
                TryRotateHistoryIfNeeded();
            }
            catch
            {
                // No hacemos throw para no romper la UI, pero informamos por consola
                Console.WriteLine("[Advertencia] No se pudo escribir en el historial.");
            }
        }

        static void TryRotateHistoryIfNeeded()
        {
            try
            {
                if (!File.Exists(HistoryFile)) return;
                var fi = new FileInfo(HistoryFile);
                if (fi.Length > MaxHistoryBytes)
                {
                    var backup = "history_" + DateTime.UtcNow.ToString("yyyyMMddHHmmss") + ".bak";
                    File.Move(HistoryFile, backup);
                    // dejamos un archivo nuevo vacío
                    File.WriteAllText(HistoryFile, string.Empty, Encoding.UTF8);
                    Console.WriteLine($"[Historial rotado a '{backup}']");
                }
            }
            catch
            {
                // ignorar problemas de rotación
            }
        }
    }
}
