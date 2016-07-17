using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace MySqlDumpAnalyzer.Shared
{
    public static class Analyser
    {

        public enum StatementType
        {
            DROPTABLE,
            CREATETABLE,
            INSERT
        }

        static byte[] Bytes(string str)
            => str.ToCharArray().Select(c => (byte)c).ToArray();

        static readonly byte[] dropTerm = Bytes("DROP TABLE");
        static readonly byte[] createTerm = Bytes("CREATE TABLE");
        static readonly byte[] insertTerm = Bytes("INSERT INTO");

        public struct FileRange
        {
            public FileStream fs;
            public long start;
            public long end;
        }
        static FileRange Range(FileStream fs, long start, long end)
            => new FileRange { fs = fs, start = start, end = end };

        public class Statement
        {
            public StatementType Type { get; }
            public string TableName { get; }
            public long Start { get; }
            public long End { get; }

            public Statement(StatementType type, string tableName, long start, long end)
            {
                Type = type;
                TableName = tableName;
                Start = start;
                End = end;
            }
        }

        public class RangeTree
        {
            public long start;
            public long end;

            public RangeTree Left { get; set; }
            public RangeTree Right { get; set; }
            public Statement FirstStatement { get; set; }
        }

        public static RangeTree AnalyseDumpFile(string filename)
        {
            using (var fs = File.OpenRead(filename)) {

                long filesize = fs.Seek(0L, SeekOrigin.End);
                fs.Seek(0L, SeekOrigin.Begin);

                return AnalyseRange(Range(fs, 0L, filesize));
            }
        }

        internal static void VisitTree(RangeTree node, Action<RangeTree> visitor)
        {
            if (node != null) {
                visitor(node);
                VisitTree(node.Left, visitor);
                VisitTree(node.Right, visitor);
            }
        }

        public static void PrintRangeTree(RangeTree node, string indentation = "")
        {
            if (node != null) {
                var stmt = node.FirstStatement;
                Console.WriteLine($"{indentation}({node.start},{node.end}) {stmt?.Start}:{stmt?.Type} {stmt?.TableName} (");
                PrintRangeTree(node.Left, indentation + "  ");
                PrintRangeTree(node.Right, indentation + "  ");
                Console.WriteLine($"{indentation})");
            }
        }

        private static RangeTree AnalyseRange(FileRange range, Statement stmt1 = null)
        {
            Debug.Print("Analysing range ({0},{1})", range.start, range.end);
            // 1. Find first statement of range: stmt1
            // 2. Bissect
            // 3. Find first statement from halfway through range: stmt2
            // 4. if stmt1.TableName != stmt2.TableName:
            //   4.1 AnalyseRange(stmt1.start, stmt2.start)
            // 5. AnalyseRange(stmt2.start, end)

            var result = new RangeTree { start = range.start, end = range.end };

            if (stmt1 == null)
                stmt1 = FindFirstStatement(range);
            if (stmt1 != null) {

                var middle = stmt1.Start + (range.end - stmt1.Start) / 2L;
                Debug.Print("middle=" + middle);

                var stmt2 = FindFirstStatement(Range(range.fs, start: middle, end: range.end));
                if (stmt2 != null) {
                    if (stmt1.TableName != stmt2.TableName) {
                        result.Left = AnalyseRange(Range(range.fs, stmt1.Start, stmt2.Start), stmt1);
                    }
                    result.Right = AnalyseRange(Range(range.fs, stmt2.Start, range.end));
                } else if (middle > stmt1.End) {
                    result.Left = AnalyseRange(Range(range.fs, stmt1.Start, middle), stmt1);
                }
            }
            if (result.Left == null)
                result.FirstStatement = stmt1;

            // Tidy up tree a bit
            if (result.Left != null && result.Right == null && result.Left.FirstStatement == result.FirstStatement) {
                return result.Left;
            } else {
                return result;
            }
        }

#if DEBUG
        // Useful to call from the immediates window 
        private static string NextNChars(FileStream fs, int numChars)
        {
            var chars = new byte[numChars];
            string result = Encoding.UTF8.GetString(chars, 0, fs.Read(chars, 0, numChars));
            fs.Seek(-numChars, SeekOrigin.Current);
            return result;
        }
#endif

        private static Statement FindFirstStatement(FileRange range)
        {
            var fs = range.fs;
            // start 1 character before to check for line feed
            var start = Math.Max(range.start - 1, 0L);
            fs.Seek(start, SeekOrigin.Begin);

            // Always have a line feed before a statement, so scan forward to that
            // cuts out some edge cases where there is DML in stored_proc
            int c;
            while ((c = fs.ReadByte()) != -1 && c != '\n');

            int termPos = 0;
            byte[] term = null;
            while ((c = fs.ReadByte()) != -1) {
                switch (c) {
                    case 'D': // DROP TABLE
                        term = dropTerm;
                        break;
                    case 'C': // CREATE TABLE
                        term = createTerm;
                        break;
                    case 'I': // INSERT INTO
                        term = insertTerm;
                        break;
                    default:
                        continue;
                }
                termPos = 1;
                while (termPos < term.Length && (c = fs.ReadByte()) != -1) {
                    if (c != term[termPos])
                        break;
                    termPos += 1;
                }
                if (termPos == term.Length) {
                    goto FoundTerm;
                }
            }
            Debug.Print("Reached end of file");
            return null;
        FoundTerm:
            long statementStart = fs.Position - term.Length;
            if (statementStart >= range.end) {
                Debug.Print("No Statements in Range ({0},{1})", range.start, range.end);
                return null;
            }

            // Get table name

            var tableName = new List<byte>();
            // Scan to starting back tick
            while ((c = fs.ReadByte()) != -1 && c != '`');
            // Copy bytes into list until we reach the next backtick
            while ((c = fs.ReadByte()) != -1 && c != '`') {
                tableName.Add((byte)c);
            }
            if (c == -1) {
                Debug.Print("Reached end of file before finding table name");
                return null;
            }

            var tableNameStr = Encoding.UTF8.GetString(tableName.ToArray());

            bool inQuote = false;
            int quoteType = -1;
            bool escaped = false;
            // Find statement end
            while ((c = fs.ReadByte()) != -1) {
                if (!inQuote) {
                    if (c == '`' || c == '\'' || c == '"') {
                        // enter quote
                        inQuote = true;
                        quoteType = c;
                    } else if (c == ';') {
                        // found statement end
                        break;
                    }
                } else {
                    if (escaped) {
                        escaped = false;
                    } else if (c == quoteType) {
                        // break out of quotes
                        inQuote = false;
                        quoteType = -1;
                    } else if (c == '\\') {
                        escaped = true;
                    }
                }
            }
            if (c == -1) {
                Debug.Print("Reached end of file before end of statement");
                return null;
            }

            {
                var termString = new string(term.Select(b => (char)b).ToArray());
                Debug.Print("Found " + termString + " statement for " + tableNameStr + " at " + statementStart);
            }

            StatementType stType =
                (term == dropTerm) ? StatementType.DROPTABLE :
                (term == createTerm) ? StatementType.CREATETABLE :
                (term == insertTerm) ? StatementType.INSERT :
                default(StatementType);

            return new Statement(
                type: stType,
                tableName: tableNameStr,
                start: statementStart,
                end: fs.Position
            );
        }

    }
}
