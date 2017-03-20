using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Xml;

namespace pt_lab1
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            //pobranie katalogu z parametru wywołania
            var di = new DirectoryInfo("C:\\Windows\\");
            try
            {
                di = new DirectoryInfo(args[0]);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            finally
            {
                Console.WriteLine("Użyto domyślnej ścieżki - C:\\Windows\\");
            }
            var pz = new PokazZawartosc();

            //pokazanie zawartości folderu
            pz.Zawartosc(di, 0);

            //pokazanie najstarszego elementu folderu
            var oldest = di.OldestFile();
            Console.WriteLine("Najstarszy plik: " + oldest + " " + oldest.LastWriteTime);

            //serializacja i deserializacja
            IFormatter formatter = new BinaryFormatter();
            try
            {
                Stream output = File.OpenWrite("kolekcja.bin");
                formatter.Serialize(output, pz.Kolekcja);
                output.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Serializacja się nie udała!");
                Console.WriteLine(e.Message);
            }
            try
            {
                Stream input = File.OpenRead("kolekcja.bin");

                //wypisanie posortowanych plików w folderze głównym
                var nowaKolekcja = (SortedDictionary<string, long>) formatter.Deserialize(input);
                foreach (var dic in nowaKolekcja)
                {
                    Console.WriteLine("{0} -> {1}", dic.Key, dic.Value);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Deserializacja się nie udała!");
                Console.WriteLine(e.Message);
            }

            Console.ReadKey();
        }
    }

    internal class PokazZawartosc
    {
        public readonly SortedDictionary<string, long> Kolekcja = new SortedDictionary<string, long>(new NameComparer());

        public void Zawartosc(DirectoryInfo di, int zaglebienie)
        {
            string nazwa;
            long dlugosc;
            try
            {
                nazwa = di.Name;
                dlugosc = di.EnumerateFileSystemInfos().Count();
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine("Napotkano element chroniony!\n" + e.Message);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("Napotkano błąd podzczas odczytu pliku!\n" + e.Message);
                return;
            }

            if (zaglebienie == 0)
            {
                Kolekcja.Add(nazwa, dlugosc);
            }

            var dirTekst = nazwa + " (" + dlugosc + ") " + di.Rahs();
            Console.WriteLine(dirTekst.PadLeft(dirTekst.Length + zaglebienie, '\t'));

            foreach (var dir in di.GetDirectories())
            {
                Zawartosc(new DirectoryInfo(di + "\\" + dir), zaglebienie + 1);
            }
            foreach (var fi in di.GetFiles())
            {
                nazwa = fi.ToString();
                dlugosc = fi.Length;
                if (zaglebienie == 0)
                {
                    Kolekcja.Add(nazwa, dlugosc);
                }

                var fileTekst = nazwa + " " + dlugosc + " bajtow " + fi.Rahs();
                Console.WriteLine(fileTekst.PadLeft(fileTekst.Length + zaglebienie, '\t'));
            }
        }

        [Serializable]
        private class NameComparer : IComparer<string>
        {
            public int Compare(string x, string y)
            {
                if (x.Length != y.Length)
                {
                    return x.Length - y.Length;
                }
                return string.Compare(x, y, StringComparison.Ordinal);
            }
        }
    }


    public static class Extensions
    {
        public static FileInfo OldestFile(this DirectoryInfo di)
        {
            FileInfo oldest = null;
            foreach (var fi in di.GetFiles())
            {
                if (oldest == null)
                {
                    oldest = fi;
                    continue;
                }
                if (DateTime.Compare(fi.LastWriteTime, oldest.LastWriteTime) < 0)
                {
                    oldest = fi;
                }
            }
            foreach (var dir in di.GetDirectories())
            {
                var diNew = new DirectoryInfo(di + "\\" + dir);
                var oldest2 = OldestFile(diNew);
                if (oldest != null && oldest2 != null &&
                    DateTime.Compare(oldest2.LastWriteTime, oldest.LastWriteTime) < 0)
                {
                    oldest = oldest2;
                }
            }
            return oldest;
        }


        public static string Rahs(this FileInfo fi)
        {
            var s = "";
            var att = fi.Attributes.ToString();

            s += att.Contains("ReadOnly") ? "r" : "-";
            s += att.Contains("Archive") ? "a" : "-";
            s += att.Contains("Hidden") ? "h" : "-";
            s += att.Contains("System") ? "s" : "-";

            return s;
        }

        public static string Rahs(this DirectoryInfo di)
        {
            var s = "";
            var att = di.Attributes.ToString();

            s += att.Contains("ReadOnly") ? "r" : "-";
            s += att.Contains("Archive") ? "a" : "-";
            s += att.Contains("Hidden") ? "h" : "-";
            s += att.Contains("System") ? "s" : "-";

            return s;
        }
    }
}