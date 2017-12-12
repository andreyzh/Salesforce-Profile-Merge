using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// Orchestrate Operations on XML Files
    /// </summary>
    // Currently it's multi-purpose class meant to do everything related to XML analysis
    //TODO: Refactor
    class XMLPermissionsHandler
    {
        public DifferenceStore DiffStore { get; set; }

        XDocument local;
        XDocument remote;
        float mergeProgress;

        public XMLPermissionsHandler()
        {
            if(XMLHandlerBase.Local != null && XMLHandlerBase.Remote != null)
            {
                local = XMLHandlerBase.Local;
                remote = XMLHandlerBase.Remote;
            }
        }

        // Loads XMLs from a given path
        public void LoadXml(string path, Config.Source source)
        {
            if(source == Config.Source.LOCAL)
                local = XDocument.Load(path);
            if (source == Config.Source.REMOTE)
                remote = XDocument.Load(path);
        }

        // Analyse differences between the input files, add to diff holder as new or changed
        public void Analyze()
        {
            //FIXME: Temp handler for null results
            if (local == null || remote == null)
                return;

            XNamespace ns = local.Root.GetDefaultNamespace();

            // Outer loop: Go though all elements in the file
            foreach (var element in local.Root.Elements())
            {
                // Inner loop - see if the component name matches
                foreach(var kvp in Config.ComponentDefinitions)
                {
                    XElement searchResult;

                    // Get element type
                    string permissionType = element.Name.LocalName;

                    // Key here is the type of permission e.g. applicationVisibilities or classAccesses 
                    if (permissionType == kvp.Key)
                    {
                        string searchTerm = null;

                        // Fetch the node with name of the component and assign as search term.
                        // Search term for example above is e.g. application Customer_Service_Excellence
                        foreach (var subelement in element.Elements())
                        {
                            if (subelement.Name.LocalName == kvp.Value)
                                searchTerm = subelement.Value;
                        }

                        // Search for the same component in the other XML
                        // LocalName is the type e.g. ApplicationVisibilities
                        // SearchTerm is the unqiue name of the component
                        var target =
                            from el in remote.Root.Elements(ns + permissionType)
                            where (string)el.Element(ns + kvp.Value) == searchTerm
                            select el;

                        // Check that we have unique return 
                        if (target.Count() == 1)
                        {
                            searchResult = target.Single();

                            // Case for non-layout changes
                            if (element.Value != searchResult.Value && permissionType != "layoutAssignments")
                                // We've found an element in local and remote - add as change
                                DiffStore.Add(element, searchResult, DifferenceStore.ChangeType.Changed);
                            // For now we can only add new layout assignments
                            if(element.Value != searchResult.Value && permissionType == "layoutAssignments")
                                DiffStore.Add(element, null, DifferenceStore.ChangeType.New);
                        }
                        // If we have no return it means that the item is not present in remote XML and we mark it as new
                        if(target.Count() == 0)
                        {
                            DiffStore.Add(element, null, DifferenceStore.ChangeType.New);
                        }
                    }
                }
            }

            // Go though all elements, but this time scan remote
            // We are now looking at remote file and checking if it doen't have something present in source
            //TODO: Refactor as subroutine because here we're mostly copy-pasting upper section
            foreach (var element in remote.Root.Elements())
            {
                // Inner loop - see if the component name matches
                foreach (var kvp in Config.ComponentDefinitions)
                {
                    // Get element type e.g. classAccesses
                    string premissionType = element.Name.LocalName;

                    // Key here is the type of permission e.g. AppVisisbility or apexVisibility
                    if (premissionType == kvp.Key)
                    {
                        string searchTerm = null;

                        // Fetch the node with name of the component and assign as search term
                        foreach (var subelement in element.Elements())
                        {
                            if (subelement.Name.LocalName == kvp.Value)
                                searchTerm = subelement.Value;
                        }

                        // Search for the same component in the other XML
                        // LocalName is the type e.g. ApplicationVisibilities
                        // SearchTerm is the unqiue name of the component
                        var target =
                            from el in local.Root.Elements(ns + premissionType)
                            where (string)el.Element(ns + kvp.Value) == searchTerm
                            select el;

                        // If we have no return it means that the item is not present in local XML, so we mark it as new
                        if (target.Count() == 0)
                        {
                            DiffStore.Add(null, element, DifferenceStore.ChangeType.New);
                        }
                    }
                }
            }
        }

        // Merge marked changes
        public void Merge(DifferenceStore diffStore, string path, object sender)
        {
            // We don't want anything to happen to originals
            XDocument mergeDoc = new XDocument(remote);
            XNamespace ns = mergeDoc.Root.GetDefaultNamespace();

            // Merge marked changed elements
            foreach (DifferenceStore.Difference change in diffStore.Diffs.Where(chg => chg.ChangeType == DifferenceStore.ChangeType.Changed && chg.Merge))
            {
                var replacementTarget =
                            from el in mergeDoc.Root.Elements(ns + change.ElementType)
                            where (string)el.Element(ns + Config.ComponentDefinitions[change.ElementType]) == change.Name
                            select el;

                //FIXME: Are we sure there's only one element?
                XElement replacementTargetElement = replacementTarget.Single();

                replacementTargetElement.ReplaceWith(change.OriginElement);
            }

            // Merge marked deleted elements
            foreach (DifferenceStore.Difference change in diffStore.Diffs.Where(chg => chg.ChangeType == DifferenceStore.ChangeType.Deleted && chg.Merge))
            {
                var replacementTarget =
                            from el in mergeDoc.Root.Elements(ns + change.ElementType)
                            where (string)el.Element(ns + Config.ComponentDefinitions[change.ElementType]) == change.Name
                            select el;

                //FIXME: Are we sure there's only one element?
                XElement replacementTargetElement = replacementTarget.Single();

                replacementTargetElement.Remove();
            }


            // Merge marked added elements
            List<DifferenceStore.Difference> additions = new List<DifferenceStore.Difference>();

            foreach (DifferenceStore.Difference change in diffStore.Diffs.Where(chg => chg.ChangeType == DifferenceStore.ChangeType.New && chg.Merge))
            {
                additions.Add(change);
            }
            float additionsSum = additions.Count;

            while (additions.Count != 0)
            {                
                foreach (var addition in additions.ToList())
                {
                    // Find previous node
                    var previousNode =
                        from el in mergeDoc.Root.Elements()
                        where XNode.DeepEquals(el, addition.OriginElement.PreviousNode)
                        select el;

                    // Previous node was found - insert after
                    if(previousNode != null)
                    {
                        var node = previousNode.Single();
                        if (node != null)
                        {
                            node.AddAfterSelf(addition.OriginElement);
                            // Remove our addition
                            additions.Remove(addition);
                        }
                        //TODO: We did not find where to put it - remove for now
                        else
                            additions.Remove(addition);

                        mergeProgress = (1 - (additions.Count / additionsSum)) * 100;
                        (sender as BackgroundWorker).ReportProgress((int)mergeProgress);
                    }
                }
            }


            XmlWriterSettings settings = new XmlWriterSettings
            {
                Encoding = new UTF8Encoding(false),
                Indent = true,
                IndentChars = "    ",
                NewLineChars = "\n"
            };

            using (var writer = XmlWriter.Create("merged.xml", settings))
            {
                mergeDoc.Save(writer);
            }
        }

        private void GetApplicationTypes()
        {
            List<string> values = new List<string>();

            foreach (var type in local.Root.Elements())
            {
                values.Add(type.Name.LocalName.ToString());
            }

            var valuesDistinct = values.Distinct();
        }

    }
}
