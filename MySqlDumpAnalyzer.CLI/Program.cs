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
        static void Main(string[] args)
        {
            Analyser.AnalyseDumpFile(args[0]);
        }
    }
}
