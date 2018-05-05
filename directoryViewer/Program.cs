using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security;
using System.Xml;

namespace directoryViewer
{
    internal static class Program
    {
        private static void Main(string[] args)
        {
            //Extract directory from start arguments
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
                Console.WriteLine("Default path used - C:\\Windows\\");
            }
            var dc = new DisplayContent();

            //Display directory content
            dc.Content(di, 0);

            //Display oldest element in directory
            var oldest = di.OldestFile();
            Console.WriteLine("Oldest file: " + oldest + " " + oldest.LastWriteTime);

            //serialization and deserialization
            IFormatter formatter = new BinaryFormatter();
            try
            {
                Stream output = File.OpenWrite("collection.bin");
                formatter.Serialize(output, dc.Collection);
                output.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Serialization failed!");
                Console.WriteLine(e.Message);
            }
            try
            {
                Stream input = File.OpenRead("collection.bin");

                //output sorted files in main directory
                var newCollection = (SortedDictionary<string, long>) formatter.Deserialize(input);
                foreach (var c in newCollection)
                {
                    Console.WriteLine("{0} -> {1}", c.Key, c.Value);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Deserialization failed!");
                Console.WriteLine(e.Message);
            }

            Console.ReadKey();
        }
    }

    internal class DisplayContent
    {
        public readonly SortedDictionary<string, long> Collection = new SortedDictionary<string, long>(new NameComparer());

        public void Content(DirectoryInfo di, int depth)
        {
            string name;
            long length;
            try
            {
                name = di.Name;
                length = di.EnumerateFileSystemInfos().Count();
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine("Protected content encountered\n" + e.Message);
                return;
            }
            catch (Exception e)
            {
                Console.WriteLine("Error encountered during file reading\n" + e.Message);
                return;
            }

            if (depth == 0)
            {
                Collection.Add(name, length);
            }

            var dirText = name + " (" + length + ") " + di.Rahs();
            Console.WriteLine(dirText.PadLeft(dirText.Length + depth, '\t'));

            foreach (var dir in di.GetDirectories())
            {
                Content(new DirectoryInfo(di + "\\" + dir), depth + 1);
            }
            foreach (var fi in di.GetFiles())
            {
                name = fi.ToString();
                length = fi.Length;
                if (depth == 0)
                {
                    Collection.Add(name, length);
                }

                var fileText = name + " " + length + " bytes " + fi.Rahs();
                Console.WriteLine(fileText.PadLeft(fileText.Length + depth, '\t'));
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