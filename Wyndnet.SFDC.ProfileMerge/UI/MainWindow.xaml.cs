using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
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
        private bool mergeMode;
        private string sourcePath;
        private string targetPath;

        AsyncJobsController asyncController;
        private ICollectionView DiffView { get; set; }
        private DifferenceStore diffStore = new DifferenceStore();
        private ObservableCollection<Difference> diffs = new ObservableCollection<Difference>();        
        
        public MainWindow(bool mergeMode)
        {
            this.mergeMode = mergeMode;

            InitializeComponent();

            // What does this do?
            dataGrid.CellEditEnding += DataGrid_CellEditEnding;

            // Init async controller - this guy will take on analysis and merge actions
            asyncController = new AsyncJobsController(diffStore, mergeMode);

            if (mergeMode)
                InitMergeMode();
            else
                InitComparisonMode();
        }

        private void InitComparisonMode()
        {
            // Make sure buttons are enabled
            ButtonLoadSourceXml.IsEnabled = true;
            ButtonLoadTargetXml.IsEnabled = true;
            ButtonAnalyze.IsEnabled = true;
            LabelLocalSource.Content = "Source";
            LabelRemoteTarget.Content = "Target";
        }

        private void InitMergeMode()
        {
            // Disable buttons not used in this mode
            ButtonLoadSourceXml.IsEnabled = false;
            ButtonLoadTargetXml.IsEnabled = false;
            ButtonAnalyze.IsEnabled = false;

            XMLHandlerBase.Init(Config.Local, Config.Remote);

            progressBarControl.Visibility = Visibility.Visible;
            
            // Run async jobs handler
            asyncController.RunAnalyis();
            asyncController.Completed += AsyncJobCompleted;
        }

        // Click handler to load source and target XML files
        private void ButtonLoad_Click(object sender, RoutedEventArgs e)
        {
            Button button = sender as Button;
            string name = button.Name;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if(openFileDialog.ShowDialog() == true)
            {
                if (name == "ButtonLoadSourceXml")
                { 
                    sourcePath = openFileDialog.FileName;
                    LabelLocalSource.Content = Path.GetFileName(sourcePath);
                }
                if (name == "ButtonLoadTargetXml")
                {
                    targetPath = openFileDialog.FileName;
                    LabelRemoteTarget.Content = Path.GetFileName(targetPath);
                }
            }
        }

        // Click handler to start analysis of differences
        private void ButtonAnalyze_Click(object sender, RoutedEventArgs e)
        {
            diffStore.Clear();
            diffs.Clear();

            // Set paths
            Config.SetPaths(sourcePath, targetPath);
            XMLHandlerBase.Init(Config.Local, Config.Remote);

            progressBarControl.Visibility = Visibility.Visible;

            // Run async jobs handler
            asyncController.RunAnalyis();
            asyncController.Completed += AsyncJobCompleted;
        }

        // Merge button handler
        private void ButtonMerge_Click(object sender, RoutedEventArgs e)
        {
            ButtonMerge.IsEnabled = false;

            progressBarControl.Visibility = Visibility.Visible;

            // Run async jobs handler
            asyncController.RunMerge();
            asyncController.Completed += AsyncJobCompleted;
        }

        // Grid element selection handler - displays XML content of nodes
        private void DataGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if(sender != null)
            {
                if (sender is DataGrid grid && grid.SelectedItems != null && grid.SelectedItems.Count == 1)
                {
                    DataGridRow dgr = grid.ItemContainerGenerator.ContainerFromItem(grid.SelectedItem) as DataGridRow;
                    if (dgr.Item is Difference obj)
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
            FilterIgnored();
        }

        // TOOD: Doesn't work
        private void ShowIgnoredButton_Click(object sender, RoutedEventArgs e)
        {
            DiffView.Filter = null;
        }

        // Display side-by-side XML from source and taget (if present)
        private void DisplayDifference(DifferenceStore.Difference change)
        {
            // Clear blocks
            textBlock_Local_Source.Text = "";
            textBlock_Remote_Target.Text = "";

            // We have slightly different handling of the text boxes names and labels depending if we're in merge mode or not
            if(mergeMode)
            {
                if (change.OriginElement != null)
                    textBlock_Local_Source.Text = change.OriginElement.ToString();
                else
                    textBlock_Local_Source.Text = "None";
                if (change.TargetElement != null)
                    textBlock_Remote_Target.Text = change.TargetElement.ToString();
                else
                    textBlock_Remote_Target.Text = "None";
            }
            // For now same code until I figure out how to work with it
            else
            {
                // Need to consider if change is addition or deletion or change
                if (change.OriginElement != null)
                    textBlock_Remote_Target.Text = change.OriginElement.ToString();
                else
                    textBlock_Remote_Target.Text = "None";
                if (change.TargetElement != null)
                    textBlock_Local_Source.Text = change.TargetElement.ToString();
                else
                    textBlock_Local_Source.Text = "None";
            }

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

        private void FilterIgnored()
        {
            DiffView.Filter = new Predicate<object>(item =>
            {
                Difference change = item as Difference;
                return change.Ignore == false;
            });
        }

#region Analysis Handler
 
        private void AnalysisCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            // Populate observable collection
            foreach (Difference change in diffStore.Diffs)
            {
                diffs.Add(change);
            }

            DiffView = CollectionViewSource.GetDefaultView(diffs);
            dataGrid.ItemsSource = DiffView;

            FilterIgnored();

            progressBarControl.Visibility = Visibility.Hidden;
        }

        private void AsyncJobCompleted(object sender, AsyncJobCompletedEventArgs e)
        {
            switch(e.AsyncAction)
            {
                case AsyncAction.Analyse:
                    {
                        diffStore = e.DiffStore;

                        // Populate observable collection
                        foreach (Difference change in diffStore.Diffs)
                        {
                            diffs.Add(change);
                        }

                        DiffView = CollectionViewSource.GetDefaultView(diffs);
                        dataGrid.ItemsSource = DiffView;

                        FilterIgnored();

                        // Dispose of events
                        asyncController.Completed -= AsyncJobCompleted;

                        break;
                    }
                case AsyncAction.Merge:
                    {
                        ButtonMerge.IsEnabled = true;
                        progressBarControl.Visibility = Visibility.Hidden;
                        MessageBox.Show("Merge Completed", "Completed");

                        // Dispose of events
                        asyncController.Completed -= AsyncJobCompleted;

                        break;
                    }
                case AsyncAction.Error:
                    {
                        ButtonMerge.IsEnabled = true;
                        progressBarControl.Visibility = Visibility.Hidden;
                        MessageBox.Show(e.Exception.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                        break;
                    }
            }

            progressBarControl.Visibility = Visibility.Hidden;
        }

        #endregion

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

        // This does some strange thing, I cannot remember what anymore
        private void DataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if(e.EditAction == DataGridEditAction.Commit)
            {
                if (e.Column is DataGridBoundColumn column)
                {
                    var bindingPath = (column.Binding as Binding).Path.Path;
                    if (bindingPath == "Merge")
                    {
                        int rowIndex = e.Row.GetIndex();
                        var el = e.EditingElement as CheckBox;

                        if ((bool)el.IsChecked)
                        {
                            Difference ignoredChange = (Difference)dataGrid.SelectedItem;
                            if(ignoredChange.Ignore)
                            {
                                ignoredChange.ChangeType = ChangeType.New;
                                ignoredChange.Ignore = false;
                            }
                        }
                        
                    }
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
