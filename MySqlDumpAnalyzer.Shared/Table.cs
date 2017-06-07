using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static MySqlDumpAnalyzer.Shared.Analyser;

namespace MySqlDumpAnalyzer.Shared
{
    public class Table
    {
        public long Start { get; private set; }
        public long End { get; private set; }

        public string Name { get; private set; }
        public CreateTable CreateTable { get; private set; }

        public Inserts Inserts { get; private set; }


        public static List<Table> GetTableList(RangeTree tree)
        {
            var tables = new List<Table>();

            VisitTree(tree, node => {
                if (node.FirstStatement != null && node.FirstStatement.Type == StatementType.DROPTABLE) {
                    var stmt = node.FirstStatement;

                    tables.Add(new Table {
                        Name = stmt.TableName,
                        CreateTable = new CreateTable {
                            Start = stmt.Start
                            // TODO: work out end
                        },
                        Start = stmt.Start
                        // TODO: work out end
                    });

                }
            });

            for (int i = 1; i < tables.Count; i++) {
                tables[i - 1].End = tables[i].Start;
            }

            return tables;
        }


    }
}
