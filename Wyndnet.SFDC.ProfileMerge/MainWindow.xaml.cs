using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        XMLHandler xmlHandler = new XMLHandler();
        DiffStore diffStore = new DiffStore();
        
        public MainWindow()
        {
            InitializeComponent();
            xmlHandler.ComponentDefinitions = Config.LoadComponentDefinitions();
            DataContext = diffStore.Diffs;
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
            xmlHandler.Analyze(diffStore);
            dataGrid.ItemsSource = diffStore.Diffs;
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
            UpdateTable(DiffStore.ChangeType.New);
        }
        
        // Filter changed items
        private void showChangesButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateTable(DiffStore.ChangeType.Changed);
        }
        
        //Show all items
        private void showAllButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateTable(DiffStore.ChangeType.None);
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

        // Updates data grid based on selected change type
        private void UpdateTable(DiffStore.ChangeType changeType)
        {
            if (diffStore.Diffs.Count > 0)
            {
                if(changeType != DiffStore.ChangeType.None)
                    dataGrid.ItemsSource = diffStore.Diffs.Where(change => change.ChangeType == changeType);
                else
                    dataGrid.ItemsSource = diffStore.Diffs;
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
