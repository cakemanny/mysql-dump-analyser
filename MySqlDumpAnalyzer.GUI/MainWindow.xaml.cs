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
using System.Diagnostics;

namespace MySqlDumpAnalyzer.GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Modelly stuff
        private string dumpFilePath;
        private readonly BackgroundWorker worker = new BackgroundWorker();
        private Analyser.RangeTree tree;
        private List<Shared.Table> tables = new List<Shared.Table>();
        private List<FileSection> sections = new List<FileSection>();

        // Elements from our GUI which we can't access because they are in templates
        // (but really there is only one of)
        private CheckBox cbExportDefsAll;
        private CheckBox cbExportDataAll;


        class FileSection
        {
            public string Name { get; set; }
            public string Type { get; set; }
            public long Start { get; set; }
            public long End { get; set; }
            public bool ExportDefinitions { get; set; }
            public bool ExportData { get; set; }
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
                End = t.End,
                ExportDefinitions = true,
                ExportData = true
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

        // Handlers for Export Definitions Checkboxes

        private void cbExportDefsAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool newValue = ((CheckBox)sender).IsChecked ?? false;
            sections.ForEach(s => s.ExportDefinitions = newValue);
            lvTables.Items.Refresh();
        }

        private void cbExportDefs_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool newValue = ((CheckBox)sender).IsChecked ?? false;
            FileSection section = (FileSection)((CheckBox)sender).DataContext;
            section.ExportDefinitions = newValue;
            if (sections.All(s => s.ExportDefinitions))
            {
                cbExportDefsAll.IsChecked = true;
            }
            else if (sections.All(s => !s.ExportDefinitions))
            {
                cbExportDefsAll.IsChecked = false;
            }
            else
            {
                cbExportDefsAll.IsChecked = null;
            }
        }

        private void cbExportDefsAll_Initialized(object sender, EventArgs e)
        {
            cbExportDefsAll = (CheckBox)sender;
        }


        // Handlers for Exports Data Checkboxes

        private void cbExportDataAll_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool newValue = ((CheckBox)sender).IsChecked ?? false;
            sections.ForEach(s => s.ExportData = newValue);
            lvTables.Items.Refresh();
        }

        private void cbExportData_CheckedChanged(object sender, RoutedEventArgs e)
        {
            bool newValue = ((CheckBox)sender).IsChecked ?? false;
            FileSection section = (FileSection)((CheckBox)sender).DataContext;
            section.ExportData = newValue;
            if (sections.All(s => s.ExportData))
            {
                cbExportDataAll.IsChecked = true;
            }
            else if (sections.All(s => !s.ExportData))
            {
                cbExportDataAll.IsChecked = false;
            }
            else
            {
                cbExportDataAll.IsChecked = null;
            }
        }

        private void cbExportDataAll_Initialized(object sender, EventArgs e)
        {
            cbExportDataAll = (CheckBox)sender; 
        }

    }
}
