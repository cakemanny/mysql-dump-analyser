using MySqlDumpAnalyzer.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlDumpAnalyzer.CLI
{
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length < 1) {
                Console.Error.WriteLine("usage: MySqlDumpAnalyzer.CLI.exe <dumpfile>");
                return 1;
            }
            string filename = args[0];
            if (!System.IO.File.Exists(filename)) {
                return 1;
            }

            var tree = Analyser.AnalyseDumpFile(args[0]);
            Analyser.PrintRangeTree(tree);
            return 0;
        }
    }
}
