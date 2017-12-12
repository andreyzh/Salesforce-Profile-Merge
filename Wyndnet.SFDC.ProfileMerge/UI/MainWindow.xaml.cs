using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using static Wyndnet.SFDC.ProfileMerge.DifferenceStore;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private XMLPermissionsHandler xmlPermissionsHandler;

        private ICollectionView DiffView { get; set; }
        private DifferenceStore diffStore = new DifferenceStore();
        private ObservableCollection<Difference> diffs = new ObservableCollection<Difference>();        
        
        public MainWindow(bool mergeMode)
        {
            InitializeComponent();

            InitMergeMode();
        }

        private void InitMergeMode()
        {
            XMLHandlerBase.Init(Config.Local, Config.Remote);
            xmlPermissionsHandler = new XMLPermissionsHandler();
            xmlPermissionsHandler.DiffStore = diffStore;

            // Calculate the differences
            xmlPermissionsHandler.Analyze();

            // Scan for deletions and valid additions
            MetadataComponentScanner scanner = new MetadataComponentScanner(Environment.CurrentDirectory);
            InnerXmlComponentScanner scanner1 = new InnerXmlComponentScanner(Environment.CurrentDirectory);
            scanner.Scan(diffStore);
            scanner1.Scan(diffStore);

            // Populate observable collection
            foreach (Difference change in diffStore.Diffs)
            {
                diffs.Add(change);
            }

            DiffView = CollectionViewSource.GetDefaultView(diffs);
            dataGrid.ItemsSource = DiffView;
        }

        /* Click handler to load source and target XML files
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
        }*/

        /* Click handler to start analysis of differences
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
        }*/

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
        // TODO: Needs to be refactored
        private void DisplayDifference(DifferenceStore.Difference change)
        {
            // Clear blocks
            textBlock_Local.Text = "";
            textBlock_Remote.Text = "";

            if (change.OriginElement != null)
                textBlock_Local.Text = change.OriginElement.ToString();
            else
                textBlock_Local.Text = "None";
            if (change.TargetElement != null)
                textBlock_Remote.Text = change.TargetElement.ToString();
            else
                textBlock_Remote.Text = "None";

            /*
            if(change.ChangeType != ChangeType.Deleted)
                textBlock_Local.Text = Utils.RemoveAllNamespaces(change.OriginElement.ToString());
            else
            {
                textBlock_Local.Text = "Deleted";
                textBlock_Remote.Text = Utils.RemoveAllNamespaces(change.OriginElement.ToString());
            }
            // For new items - don't display target
            if (change.TargetElement != null)
                textBlock_Remote.Text = Utils.RemoveAllNamespaces(change.TargetElement.ToString());
            */
        }

        // Merge button handler
        private void MergeButton_Click(object sender, RoutedEventArgs e)
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += MergeXml;
            worker.RunWorkerCompleted += MergeXmlCompleted;
            worker.ProgressChanged += MergeXmlProgressChanged;
            progressBar.Visibility = Visibility.Visible;

            worker.RunWorkerAsync(); 
        }

        // FIXME: Temporary merge handler
        private void MergeButton_Click1(object sender, RoutedEventArgs e)
        {
            XMLMergeHandler mergeHandler = new XMLMergeHandler();

            mergeHandler.Merge(diffStore, "path", sender);
        }

        private void MergeXmlCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            progressBar.Visibility = Visibility.Hidden;
            MessageBox.Show("Merge Completed");
        }

        void MergeXml(object sender, DoWorkEventArgs e)
        {
            XMLMergeHandler mergeHandler = new XMLMergeHandler();
            mergeHandler.Merge(diffStore, "path", sender);
            //xmlPermissionsHandler.Merge(diffStore, Config.Merged, sender);
        }

        void MergeXmlProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            progressBar.Value = e.ProgressPercentage;
        }

#region DataGrid UI Controls
        private void SelectAllCheckboxChecked(object sender, RoutedEventArgs e)
        {
            dataGrid.Items.OfType<Difference>().ToList().ForEach(x => x.Merge = true);
            DiffView.Refresh();
        }

        private void SelectAllCheckboxUnchecked(object sender, RoutedEventArgs e)
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
#endregion
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
