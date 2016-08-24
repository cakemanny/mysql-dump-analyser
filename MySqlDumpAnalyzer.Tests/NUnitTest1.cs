using System;
using System.IO;
using NUnit.Framework;
using MySqlDumpAnalyzer.Shared;

namespace MySqlDumpAnalyzer.Tests
{
    [TestFixture]
    public class NUnitTest1
    {
        string dumpPath;

        [SetUp]
        public void Startup()
        {
            dumpPath = Path.Combine(TestContext.CurrentContext.TestDirectory, "Dumps");
        }

        [Test]
        public void SingleTable()
        {

            var tree = Analyser.AnalyseDumpFile(Path.Combine(dumpPath, "database-001.sql"));
            var tables = Table.GetTableList(tree);

            Assert.That(tables, Has.Count.EqualTo(1));
            Assert.That(tables[0].Name, Is.EqualTo("singletable"));
        }

        [Test]
        public void TwoTables()
        {
            var tree = Analyser.AnalyseDumpFile(Path.Combine(dumpPath, "database-002.sql"));
            var tables = Table.GetTableList(tree);

            Assert.That(tables, Has.Count.EqualTo(2));
            Assert.That(tables[0].Name, Is.EqualTo("table1"));
            Assert.That(tables[1].Name, Is.EqualTo("table2"));
            Assert.That(tables[0].Start, Is.LessThan(tables[1].Start));
        }

        [Test]
        public void TwoTablesAndData()
        {
            var tree = Analyser.AnalyseDumpFile(Path.Combine(dumpPath, "database-003.sql"));
            var tables = Table.GetTableList(tree);

            Assert.That(tables, Has.Count.EqualTo(2));
            Assert.That(tables[0].Name, Is.EqualTo("table1"));
            Assert.That(tables[1].Name, Is.EqualTo("table2"));
            Assert.That(tables[0].Start, Is.LessThan(tables[1].Start));
        }


        [Test]
        public void TwoTableAndView()
        {
            var tree = Analyser.AnalyseDumpFile(Path.Combine(dumpPath, "database-004.sql"));
            var tables = Table.GetTableList(tree);

            Assert.That(tables, Has.Count.EqualTo(2));
            Assert.That(tables[0].Name, Is.EqualTo("table1"));
            Assert.That(tables[1].Name, Is.EqualTo("view1"));
            Assert.That(tables[0].Start, Is.LessThan(tables[1].Start));
        }
    }
}