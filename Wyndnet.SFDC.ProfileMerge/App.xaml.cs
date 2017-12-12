using System.Windows;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void ApplicationStart(object sender, StartupEventArgs e)
        {
            // Comparison mode
            if(e.Args.Length == 0 || e.Args == null)
            {
                MessageBox.Show("Starting in comparison mode is not supported in this version. Please use as merge tool.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
                return;
                //MainWindow window = new MainWindow(false);
                //window.Show();
            }
            // We're expecting 4 links to different file versions for merge mode
            if (e.Args.Length == 4)
            { 
                Config.SetPaths(e.Args[0], e.Args[1], e.Args[2], e.Args[3]);
                Config.SetComponentDefinitions();

                MainWindow window = new MainWindow(true);
                window.Show();
            }
            else
            {
                MessageBox.Show("Unable to start merge tool.\nExpecting 4 parameters: BASE, LOCAL, REMOTE, MERGED\nParameters found: " + e.Args.Length.ToString(), "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Current.Shutdown();
            }

            
        }
    }
}

//cmd = C:/Users/Andreyzh/Documents/visual\\ studio\\ 2017/Projects/Wyndnet.SFDC.ProfileMerge/Wyndnet.SFDC.ProfileMerge/bin/Debug/Wyndnet.SFDC.ProfileMerge.exe "$BASE" "$LOCAL" "$REMOTE" "$MERGED"
// \"$BASE\" \"$LOCAL\" \"$REMOTE\" \"$MERGED\"

//This works from command line: 
// C:/Users/Andreyzh/Documents/visual\ studio\ 2017/Projects/Wyndnet.SFDC.ProfileMerge/Wyndnet.SFDC.ProfileMerge/bin/Debug/Wyndnet.SFDC.ProfileMerge.exe