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
        static readonly byte[] dropTerm = Bytes("DROP TABLE");
        static readonly byte[] createTerm = Bytes("CREATE TABLE");
        static readonly byte[] insertTerm = Bytes("INSERT INTO");

        struct FileRange
        {
            public FileStream fs;
            public long start;
            public long end;
        }

        class Statement
        {
            public string tableName;
            public long start;
            public long end;
        }

        static FileRange Range(FileStream fs, long start, long end)
        {
            return new FileRange { fs = fs, start = start, end = end };
        }

        public static List<Table> AnalyseDumpFile(string filename)
        {

            using (var fs = File.OpenRead(filename)) {
                var result = new List<Table>();

                long filesize = fs.Seek(0L, SeekOrigin.End);
                fs.Seek(0L, SeekOrigin.Begin);

                AnalyseRange(Range(fs, 0L, filesize));

                return result;
            }
        }

        private static void AnalyseRange(FileRange range)
        {
            // 1. Find first statement of range: stmt1
            // 2. Bissect
            // 3. Find first statement from halfway through range: stmt2
            // 4. if stmt1.TableName != stmt2.TableName:
            //   4.1 AnalyseRange(stmt1.start, stmt2.start)
            // 5. AnalyseRange(stmt2.start, end)

            var stmt = FindFirstStatement(range);
            if (stmt != null) {
                Console.WriteLine("tableName: {0}", stmt.tableName);
                Console.WriteLine("start:     {0}", stmt.tableName);
                Console.WriteLine("end:       {0}", stmt.end);
            }
        }

        private static Statement FindFirstStatement(FileRange range)
        {
            var fs = range.fs;
            fs.Seek(range.start, SeekOrigin.Begin);

            long offset = 0;
            int c;
            int termPos = 0;
            byte[] term = null;
            while ((c = fs.ReadByte()) != -1) {
                offset += 1L;
                switch (c) {
                    case 'D': // DROP TABLE
                        termPos = 1;
                        while (termPos < dropTerm.Length && (c = fs.ReadByte()) != -1) {
                            offset += 1L;
                            if (c != dropTerm[termPos]) {
                                termPos = 0;
                                break;
                            }
                        }
                        if (termPos == dropTerm.Length) {
                            term = dropTerm;
                            goto FoundTerm;
                        }
                        break;
                    case 'C': // CREATE TABLE

                        break;
                    case 'I': // INSERT INTO

                        break;
                    default:
                        break;
                }
            }
            Debug.Print("Reached end of file");
            return null;
        FoundTerm:
            long statementStart = offset - term.Length;
            if (statementStart >= range.end) {
                Debug.Print("No Statements in Range");
                return null;
            }

            // Get table name

            int backTickCount = 0;
            var tableName = new List<byte>();
            while ((c = fs.ReadByte()) != -1) {
                offset += 1L;
                if (c == '`') {
                    backTickCount += 1;
                    if (backTickCount == 2) {
                        break;
                    }
                } else if (backTickCount == 1) {
                    tableName.Add((byte)c);
                }
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
                offset += 1L;
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

            return new Statement {
                tableName = tableNameStr,
                start = statementStart,
                end = offset - 1L
            };
        }

        static byte[] Bytes(string str)
        {
            return str.ToCharArray().Select(c => (byte)c).ToArray();
        }

    }
}
