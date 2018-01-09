using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    static class Config
    {
        public static string Base { get; private set; }
        public static string Local { get; private set; }
        public static string Remote { get; private set; }
        public static string Merged { get; private set; }

        public enum Source {BASE, LOCAL, REMOTE, MERGED}

        public static Dictionary<string, string> ComponentDefinitions { get { return componentDefinitions; } }
        public static Dictionary<string, string> ComponentFolderMap { get { return componentFolderMap; } }

        static Dictionary<string, string> componentDefinitions = new Dictionary<string, string>();
        static Dictionary<string, string> componentFolderMap = new Dictionary<string, string>();

        public static Dictionary<string, string> SetComponentDefinitions()
        {
            try
            {
                string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "components.xml");
                XDocument doc = XDocument.Load(path);
                var results = from component in doc.Root.Elements()
                              select new
                              {
                                  type = component.Attribute("type").Value.ToString(),
                                  name = component.Attribute("name").Value.ToString(),
                                  folder = component.Attribute("folder").Value.ToString() //?? string.Empty
                              };

                foreach(var result in results)
                {
                    componentDefinitions.Add(result.type, result.name);
                    componentFolderMap.Add(result.type, result.folder);
                }
            }
            catch(IOException ex)
            {
                MessageBox.Show("Unable to find component definitions file components.xml\nWill now quit.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            return componentDefinitions;
        }

        public static void SetPaths(string basePath, string localPath, string remotePath, string mergedPath)
        {
            // Should get the root directory of the project when called from Git client
            string currentDir = Environment.CurrentDirectory;

            Base = currentDir + "\\" + Utils.ConvertUnixPathToWindows(basePath);
            Local = currentDir + "\\" + Utils.ConvertUnixPathToWindows(localPath);
            Remote = currentDir + "\\" + Utils.ConvertUnixPathToWindows(remotePath);
            Merged = currentDir + "\\" + Utils.ConvertUnixPathToWindows(mergedPath);
        }

        public static void SetPaths(string sourcePath, string targetPath)
        {
            Local = targetPath;
            Remote = sourcePath;
            Merged = targetPath;
        }
    }
}
