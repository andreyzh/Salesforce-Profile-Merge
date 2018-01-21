using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// Collects the component names availabe though project (filesystem) scan for file names.
    /// This is appliable for e.g. classes, pages, custom objects
    /// </summary>
    class MetadataComponentScanner : IComponentScaner
    {
        // Key is type e.g. apexclass, value is name e.g. Utils
        //Dictionary<string, string> components;
        string projectPath = null;

        public MetadataComponentScanner(string projectPath)
        {
            this.projectPath = projectPath;
            //components = new Dictionary<string, string>();
        }

        public DifferenceStore Scan(DifferenceStore diffStore)
        {
            // DiffStore Contains what we need to check for. Now we need to get some deletions and additions
            var candidates = diffStore.Diffs.Where(candidate => (candidate.ChangeType == DifferenceStore.ChangeType.New));
            //var additionDiffs = diffStore.Diffs.Where(added => added.ChangeType == DifferenceStore.ChangeType.New);

            List<string> types = new List<string>();

            // Get the types of the changes
            foreach(var change in candidates)
            {
                if (!types.Contains(change.ElementType))
                    types.Add(change.ElementType);
            }

            // Store list of paths to scan
            List<string> paths = new List<string>();
            // Component type - list of components of the type
            Dictionary<string, List<string>> componentTypeMap = new Dictionary<string, List<string>>();

            foreach (string type in types)
            {
                string path;
                Config.ComponentFolderMap.TryGetValue(type, out string pth);
                if(!String.IsNullOrEmpty(pth))
                { 
                    try
                    { 
                        // Path to component directory e.g. "classe"
                        path = Environment.CurrentDirectory + "\\src\\" + pth;

                        List<string> filepaths = Directory.EnumerateFiles(path).ToList<string>();
                        List<string> filenames = new List<string>();

                        // Don't get unnecessary meta definition files
                        foreach(var file in filepaths)
                        {
                            if (Path.GetExtension(file) != ".xml")
                                filenames.Add(Path.GetFileNameWithoutExtension(file));
                        }

                        componentTypeMap.Add(type, filenames);
                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK);
                    }
                }
            }

            var local = DifferenceStore.ChangeSource.Local;
            var remote = DifferenceStore.ChangeSource.Remote;

            // Check which referenced components are not present as metadata
            foreach (var change in candidates)
            {
                componentTypeMap.TryGetValue(change.ElementType, out List<string> components);

                if (components != null)
                {
                    // Mark as present for UI
                    // Note: there's a simiar thing to be used in innerxmlComponentScanner
                    if (components.Contains(change.Name))
                        change.InRepository = true;

                    /* CASE 3
                    * This means it's defined in local file and present in repo, but was not added in remote. 
                    * Valid local addition and should not even be considered as merge relevant */
                    if (components.Contains(change.Name) && change.ChangeSource == local)
                    {
                        change.Ignore = true;
                        change.ChangeType = DifferenceStore.ChangeType.None;
                        change.Merge = false;
                    }

                    // CASE 4
                    else if (!components.Contains(change.Name) && change.ChangeSource == local)
                    {
                        change.ChangeType = DifferenceStore.ChangeType.Deleted;
                        change.Merge = true;
                    }

                    // CASE 5
                    else if (components.Contains(change.Name) && change.ChangeSource == remote)
                    {
                        change.ChangeType = DifferenceStore.ChangeType.New;
                        change.Merge = true;
                    }

                    /* CASE 6
                     * This means that remote is referencing something that was deleted locally
                     * Valid local deletion and should not be considered as merge relevant */
                    else if (!components.Contains(change.Name) && change.ChangeSource == remote)
                    {
                        change.Ignore = true;
                        change.ChangeType = DifferenceStore.ChangeType.None;
                        change.Merge = false;
                    }
                }
            }

            return diffStore;
        }
    }
}

/* Present in REMOTE and absent in LOCAL, however present in the filesystem - must be valid addition
if (components.Contains(change.Name) && change.ChangeType == DifferenceStore.ChangeType.New)
{
    change.Merge = true;
}
// Present in REMOTE and absent in LOCAL, present in filesystem of type deletion = must be an addition :)
if(components.Contains(change.Name) && change.ChangeType == DifferenceStore.ChangeType.Deleted)
{
    change.Merge = true;
    change.ChangeType = DifferenceStore.ChangeType.New;
}
// Absent in LOCAL and present in REMOTE, however not present in filesystem. Must be a valid deletion
else if(!components.Contains(change.Name) && change.ChangeType == DifferenceStore.ChangeType.Deleted)
    change.Merge = true;*/

/* Check which additions are valid
foreach (var add in additionDiffs)
{
componentTypeMap.TryGetValue(add.ElementType, out List<string> components);

if (components != null)
{
    // This means that our addition candidate is present in actual metadata, so it's valid
    if (components.Contains(add.Name))
    {
        //MessageBox.Show("Bingo! Deletion " + del.Name + " is not a deletion :)");
    }
    // Not found - most likely was deleted
    else
        add.Merge = false;
}
}*/
