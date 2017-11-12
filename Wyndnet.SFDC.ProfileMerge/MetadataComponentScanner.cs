using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// Collects the component names availabe though project (filesystem) scan for file names.
    /// This is appliable for e.g. classes, pages, custom objects
    /// </summary>
    class MetadataComponentScanner : IComponentScaner
    {
        // Key is type e.g. apexclass, value is name e.g. Utils
        Dictionary<string, string> components;
        string projectPath = null;

        public MetadataComponentScanner(string projectPath)
        {
            this.projectPath = projectPath;
            components = new Dictionary<string, string>();
        }

        public DifferenceStore Scan(DifferenceStore diffStore)
        {
            // DiffStore Contains what we need to check for. Now we need to get some deletions
            var deletionDiffs = diffStore.Diffs.Where(deleted => deleted.ChangeType == DifferenceStore.ChangeType.Deleted);
            List<string> types = new List<string>();

            // Get the types of the deletions
            foreach(var del in deletionDiffs)
            {
                if (!types.Contains(del.ElementType))
                    types.Add(del.ElementType);
            }

            // Store list of paths to scan
            List<string> paths = new List<string>();
            //TODO: We're getting too many entries which we don't need - meta.xml definitions
            Dictionary<string, List<string>> componentTypeMap = new Dictionary<string, List<string>>();
            foreach (string type in types)
            {
                string path;
                Config.ComponentFolderMap.TryGetValue(type, out string pth);
                if(!String.IsNullOrEmpty(pth))
                { 
                    path = Environment.CurrentDirectory + "\\src\\" + pth;
                    //TODO: this needs to be wrapped
                    List<string> filenames = Directory.GetFiles(path).ToList<string>();
                    componentTypeMap.Add(type, filenames);
                }
            }
            
            foreach(var path in paths)
            {
                var filenames = Directory.GetFiles(path);
                // How to we now know which type are we getting for?
            }

            return diffStore;
        }
    }
}
