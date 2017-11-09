using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
using System.Xml.Linq;
using System.Xml.XPath;
using static Wyndnet.SFDC.ProfileMerge.DifferenceStore;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private XMLHandler xmlHandler = new XMLHandler();

        private ICollectionView DiffView { get; set; }
        private DifferenceStore diffStore = new DifferenceStore();
        private ObservableCollection<Difference> diffs = new ObservableCollection<Difference>();        
        
        public MainWindow()
        {
            InitializeComponent();
            xmlHandler.ComponentDefinitions = Config.LoadComponentDefinitions();
            xmlHandler.DiffStore = diffStore;

            xmlHandler.LoadXml(Config.Remote, "target");
            xmlHandler.LoadXml(Config.Local, "source");
        }

        // Click handler to load source and target XML files
        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string name = button.Name;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if(openFileDialog.ShowDialog() == true)
            {
                if(name == "LoadSourceXml")
                    xmlHandler.LoadXml(openFileDialog.FileName, "source");
                if (name == "LoadTargetXml")
                    xmlHandler.LoadXml(openFileDialog.FileName, "target");
            }
        }

        // Click handler to start analysis of differences
        private void AnalyseButton_Click_(object sender, RoutedEventArgs e)
        {
            // Clear diffstore
            diffStore.Clear();
            diffs.Clear();

            // Calculate the differences
            xmlHandler.Analyze();
            
            // Populate observable collection
            foreach(Difference change in diffStore.Diffs)
            {
                diffs.Add(change);
            }

            DiffView = CollectionViewSource.GetDefaultView(diffs);
            
            dataGrid.ItemsSource = DiffView;
        }

        // Grid element selection handler - displays XML content of nodes
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(sender != null)
            {
                if (sender is DataGrid grid && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                    if (dgr.Item is DifferenceStore.Difference obj)
                        DisplayDifference(obj);
                }
            }
        }

        // Filter new items
        private void ShowAdditionsButton_Click(object sender, RoutedEventArgs e)
        {
            DiffView.Filter = new Predicate<object>(item =>
            {
                Difference change = item as Difference;
                return change.ChangeType == ChangeType.New;
            });
        }
        
        // Filter changed items
        private void ShowChangesButton_Click(object sender, RoutedEventArgs e)
        {
            DiffView.Filter = new Predicate<object>(item =>
            {
                Difference change = item as Difference;
                return change.ChangeType == ChangeType.Changed;
            });
        }

        // Filter deleted items
        private void ShowDeletionsButton_Click(object sender, RoutedEventArgs e)
        {
            DiffView.Filter = new Predicate<object>(item =>
            {
                Difference change = item as Difference;
                return change.ChangeType == ChangeType.Deleted;
            });
        }

        //Show all items
        private void ShowAllButton_Click(object sender, RoutedEventArgs e)
        {
            DiffView.Filter = null;
        }

        // Display side-by-side XML from source and taget (if present)
        private void DisplayDifference(DifferenceStore.Difference change)
        {
            // Clear blocks
            textBlock.Text = "";
            textBlock_Copy.Text = "";

            if(change.ChangeType != ChangeType.Deleted)
                textBlock.Text = Utils.RemoveAllNamespaces(change.OriginElement.ToString());
            else
            {
                textBlock.Text = "Deleted";
                textBlock_Copy.Text = Utils.RemoveAllNamespaces(change.OriginElement.ToString());
            }
            // For new items - don't display target
            if (change.TargetElement != null)
                textBlock_Copy.Text = Utils.RemoveAllNamespaces(change.TargetElement.ToString());
        }

        // Merge button handler
        private void MergeButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += MergeXml;
            worker.RunWorkerCompleted += MergeXmlCompleted;
            worker.ProgressChanged += mergeXmlProgressChanged;
            progressBar.Visibility = Visibility.Visible;

            worker.RunWorkerAsync(); 
        }

        private void MergeXmlCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Visibility = Visibility.Hidden;
            MessageBox.Show("Merge Completed");
        }

        void MergeXml(object sender, DoWorkEventArgs e)
        {
            xmlHandler.Merge(diffStore, sender);
        }

        void mergeXmlProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

        private void SelectAllCheckboxChecked(object sender, RoutedEventArgs e)
        {
            dataGrid.Items.OfType<Difference>().ToList().ForEach(x => x.Merge = true);
            DiffView.Refresh();
        }

        private void selectAllCheckboxUnchecked(object sender, RoutedEventArgs e)
        {
            dataGrid.Items.OfType<Difference>().ToList().ForEach(x => x.Merge = false);
            DiffView.Refresh();
        }

        // Handle multiple selections
        private void DataGrid_KeyDown(object sender, KeyEventArgs e)
        {
            if (sender != null && sender is DataGrid grid && grid.SelectedItems != null && grid.SelectedItems.Count > 1)
            {
                // Set multiple merge items
                if (e.Key == Key.A)
                {
                    foreach(var item in grid.SelectedItems)
                    {
                        var change = item as DifferenceStore.Difference;
                        change.Merge = !change.Merge;
                    }

                    DiffView.Refresh();
                }
            }      
        }
    }
}

/*
 * applicationVisibilities -- application
 * classAccesses -- apexClass
 * custom
 * fieldPermissions -- field
 * layoutAssignments -- layout
 * loginIpRanges -- ?
 * objectPermissions -- object
 * pageAccesses -- apexPage
 * recordTypeVisibilities -- recordType
 * tabVisibilities -- tab
 * userLicense -- ?
 * userPermissions -- name
*/
