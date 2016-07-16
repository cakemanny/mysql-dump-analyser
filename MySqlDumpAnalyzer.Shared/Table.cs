using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MySqlDumpAnalyzer.Shared
{
    public class Table
    {
        ulong start;
        ulong end;

        public string Name { get; set; }
        public CreateTable CreateTable { get; set; }

        public Inserts Inserts { get; set; }

    }
}
