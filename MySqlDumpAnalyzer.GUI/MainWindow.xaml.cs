using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using MySqlDumpAnalyzer.Shared;

namespace MySqlDumpAnalyzer.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        private string dumpFilePath;
        private readonly BackgroundWorker worker = new BackgroundWorker();
        private Analyser.RangeTree tree;
        private List<Shared.Table> tables;
        private List<FileSection> sections;

        class FileSection
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public long Start { get; set; }
            public long End { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            worker.DoWork += Worker_DoWork;
            worker.RunWorkerCompleted += Worker_RunWorkerCompleted;
        }

        private void Worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            grLoading.Visibility = Visibility.Hidden;
            tree = e.Result as Analyser.RangeTree;
            tables = Shared.Table.GetTableList(tree);
            sections = tables.Select(t => new FileSection {
                Name = t.Name,
                Type = "Table",
                Start = t.Start,
                End = t.End
            }).ToList();
            lvTables.ItemsSource = sections;
        }

        private void Worker_DoWork(object sender, DoWorkEventArgs e)
        {
            e.Result = Analyser.AnalyseDumpFile((string)e.Argument);
        }

        private void mnuExit_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void OpenCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = !worker.IsBusy;
        }

        private void OpenCommand_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "SQL Dump Files (*.sql;*.dump)|*.sql;*.dump|All Files (*.*)|*.*";
            if (ofd.ShowDialog() == true) {
                dumpFilePath = ofd.FileName;
                worker.RunWorkerAsync(dumpFilePath);
                grLoading.Visibility = Visibility.Visible;
            }
        }
    }
}
