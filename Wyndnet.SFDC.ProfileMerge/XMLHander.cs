using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// Orchestrate Operations on XML Files
    /// </summary>
    // Currently it's multi-purpose class meant to do everything related to XML analysis
    //TODO: Refactor
    class XMLHandler
    {
        public Dictionary<string, string> ComponentDefinitions { get; set; }
        public DiffStore DiffStore { get; set; }

        XDocument sourceDoc;
        XDocument targetDoc;
        float mergeProgress;

        // Loads XMLs from a given path
        public void LoadXml(string path, string targetSelection)
        {
            if(targetSelection == "source")
                sourceDoc = XDocument.Load(path);
            if (targetSelection == "target")
                targetDoc = XDocument.Load(path);
        }

        // Analyse differences between the input files, add to diff holder as new or changed
        public void Analyze()
        {
            //FIXME: Temp handler for null results
            if (sourceDoc == null || targetDoc == null)
                return;

            XNamespace ns = sourceDoc.Root.GetDefaultNamespace();

            // Outer loop: Go though all elements in the file
            foreach (var element in sourceDoc.Root.Elements())
            {
                // Inner loop - see if the component name matches
                foreach(var kvp in ComponentDefinitions)
                {
                    XElement searchResult;

                    // Get element type
                    string localName = element.Name.LocalName;

                    // Key here is the type of permission e.g. AppVisisbility or apexVisibility
                    if (localName == kvp.Key)
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
                            from el in targetDoc.Root.Elements(ns + localName)
                            where (string)el.Element(ns + kvp.Value) == searchTerm
                            select el;

                        // Check that we have unique return 
                        if (target.Count() == 1)
                        {
                            searchResult = target.Single();

                            // Case for non-layout changes
                            if (element.Value != searchResult.Value && localName != "layoutAssignments")
                                DiffStore.Add(element, searchResult, DiffStore.ChangeType.Changed);
                            // For now we can only add new layout assignments
                            if(element.Value != searchResult.Value && localName == "layoutAssignments")
                                DiffStore.Add(element, null, DiffStore.ChangeType.New);
                        }
                        // If we have no return it means that the item is not present in target XML and we mark it as new
                        if(target.Count() == 0)
                        {
                            DiffStore.Add(element, null, DiffStore.ChangeType.New);
                        }
                    }
                }
            }

            // Go though all elements, but this time scan for deletions
            // We are now looking at target document and checking if it doen't have something present in source
            //TODO: Refactor as subroutine because here we're mostly copy-pasting upper section
            foreach (var element in targetDoc.Root.Elements())
            {
                // Inner loop - see if the component name matches
                foreach (var kvp in ComponentDefinitions)
                {
                    // Get element type e.g. apexVisibility
                    string localName = element.Name.LocalName;

                    // Key here is the type of permission e.g. AppVisisbility or apexVisibility
                    if (localName == kvp.Key)
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
                            from el in sourceDoc.Root.Elements(ns + localName)
                            where (string)el.Element(ns + kvp.Value) == searchTerm
                            select el;

                        // If we have no return it means that the item is not present in source XML, so it must have been deleted
                        if (target.Count() == 0)
                        {
                            DiffStore.Add(element, null, DiffStore.ChangeType.Deleted);
                        }
                    }
                }
            }
        }

        // Merge marked changes
        public void Merge(DiffStore diffStore, object sender)
        {
            //XMLMerge xmlMerge = new XMLMerge();

            // We don't want anything to happen to originals
            XDocument mergeDoc = new XDocument(targetDoc);
            XNamespace ns = mergeDoc.Root.GetDefaultNamespace();

            // Merge marked changed elements
            foreach (DiffStore.Change change in diffStore.Diffs.Where(chg => chg.ChangeType == DiffStore.ChangeType.Changed && chg.Merge))
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
            foreach (DiffStore.Change change in diffStore.Diffs.Where(chg => chg.ChangeType == DiffStore.ChangeType.Deleted && chg.Merge))
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
            List<DiffStore.Change> additions = new List<DiffStore.Change>();

            foreach (DiffStore.Change change in diffStore.Diffs.Where(chg => chg.ChangeType == DiffStore.ChangeType.New && chg.Merge))
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
                        //FIXME - are we sure it's unique and first result
                        var node = previousNode.FirstOrDefault();
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
            

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.Indent = true;
            settings.IndentChars = "    ";
            settings.NewLineChars = "\n";

            using (var writer = XmlWriter.Create("merged.xml", settings))
            {
                mergeDoc.Save(writer);
            }
        }

        private void GetApplicationTypes()
        {
            List<string> values = new List<string>();

            foreach (var type in sourceDoc.Root.Elements())
            {
                values.Add(type.Name.LocalName.ToString());
            }

            var valuesDistinct = values.Distinct();
        }

    }
}
