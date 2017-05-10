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
using static Wyndnet.SFDC.ProfileMerge.DiffStore;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private ICollectionView diffView { get; set; }

        XMLHandler xmlHandler = new XMLHandler();
        // Contains diffs found from XMLs
        DiffStore diffStore = new DiffStore();
        // Holds view of the diffs from diffstore
        ObservableCollection<Change> diffs = new ObservableCollection<Change>();
        
        public MainWindow()
        {
            InitializeComponent();
            xmlHandler.ComponentDefinitions = Config.LoadComponentDefinitions();
        }

        // Click handler to load source and target XML files
        private void loadButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if(openFileDialog.ShowDialog() == true)
            {
                xmlHandler.LoadXml(openFileDialog.FileName);
            }
        }

        // Click handler to start analysis of differences
        private void analyseButton_Click_(object sender, RoutedEventArgs e)
        {
            // Calculate the differences
            xmlHandler.Analyze(diffStore);
            
            // Populate observable collection
            foreach(Change change in diffStore.Diffs)
            {
                diffs.Add(change);
            }

            diffView = CollectionViewSource.GetDefaultView(diffs);
            
            dataGrid.ItemsSource = diffView;
        }

        // Grid element selection handler - displays XML content of nodes
        private void dataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(sender != null)
            {
                if (sender is DataGrid grid && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                    DiffStore.Change obj = dgr.Item as DiffStore.Change;
                    if (obj != null)
                        DisplayDifference(obj);
                }
            }
        }

        // Filter new items
        private void showAdditionsButton_Click(object sender, RoutedEventArgs e)
        {
            diffView.Filter = new Predicate<object>(item =>
            {
                Change change = item as Change;
                return change.ChangeType == ChangeType.New;
            });
        }
        
        // Filter changed items
        private void showChangesButton_Click(object sender, RoutedEventArgs e)
        {
            diffView.Filter = new Predicate<object>(item =>
            {
                Change change = item as Change;
                return change.ChangeType == ChangeType.Changed;
            });
        }
        
        //Show all items
        private void showAllButton_Click(object sender, RoutedEventArgs e)
        {
            diffView.Filter = null;
        }

        // Display side-by-side XML from source and taget (if present)
        private void DisplayDifference(DiffStore.Change change)
        {
            // Clear blocks
            textBlock.Text = "";
            textBlock_Copy.Text = "";

            textBlock.Text = Utils.RemoveAllNamespaces(change.OriginElement.ToString());
            // For new items - don't display target
            if (change.TargetElement != null)
                textBlock_Copy.Text = Utils.RemoveAllNamespaces(change.TargetElement.ToString());
        }

        private void mergeButton_Click(object sender, RoutedEventArgs e)
        {
            xmlHandler.MergeChanges(diffStore);
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
