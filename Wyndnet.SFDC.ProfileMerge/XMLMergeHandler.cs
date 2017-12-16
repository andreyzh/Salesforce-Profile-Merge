using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// Handles merging of selected changes into the merge (local) file version
    /// </summary>
    class XMLMergeHandler
    {
        XDocument local; 
        XDocument remote;
        float mergeProgress;

        public XMLMergeHandler()
        {
            if (XMLHandlerBase.Local != null && XMLHandlerBase.Remote != null)
            {
                local = XMLHandlerBase.Local;
                remote = XMLHandlerBase.Remote;
            }
        }

        public void Merge(DifferenceStore diffStore, string path, object sender)
        {
            // Make a copy of the LOCAL XML - we will be merging into that one
            XDocument mergeDoc = new XDocument(local);
            XDocument mergeDoc1 = new XDocument(local);
            XNamespace ns = mergeDoc.Root.GetDefaultNamespace();

            // STAGE 1 - Merge selected changes
            foreach (DifferenceStore.Difference change in diffStore.Diffs.Where(chg => chg.ChangeType == DifferenceStore.ChangeType.Changed && chg.Merge))
            {
                var replacementTarget =
                            from el in mergeDoc.Root.Elements(ns + change.ElementType)
                            where (string)el.Element(ns + Config.ComponentDefinitions[change.ElementType]) == change.Name
                            select el;

                // Single() method already covers for cases when there would be more than one element
                XElement replacementTargetElement = replacementTarget.Single();

                replacementTargetElement.ReplaceWith(change.TargetElement);
            }

            // STAGE 2 - Remove elements marked for deletion
            // Merge marked deleted elements
            foreach (DifferenceStore.Difference change in diffStore.Diffs.Where(chg => chg.ChangeType == DifferenceStore.ChangeType.Deleted && chg.Merge))
            {
                var replacementTarget =
                            from el in mergeDoc.Root.Elements(ns + change.ElementType)
                            where (string)el.Element(ns + Config.ComponentDefinitions[change.ElementType]) == change.Name
                            select el;

                XElement replacementTargetElement = replacementTarget.Single();

                replacementTargetElement.Remove();
            }

            // STAGE 3 - Add new elements
            // Get all additions to the new list
            List<DifferenceStore.Difference> additions = new List<DifferenceStore.Difference>();

            foreach (DifferenceStore.Difference change in diffStore.Diffs.Where(chg => chg.ChangeType == DifferenceStore.ChangeType.New && chg.Merge))
            {
                additions.Add(change);
            }

            float additionsSum = additions.Count;

            // We should continue this exercise until we're done with all additions
            while (additions.Count != 0)
            {
                // Iterate though remaining changes
                foreach (var addition in additions.ToList())
                {
                    // Find previous node our change from remote in local. 
                    // We're looking only at target element for now since addition is incoming from remote.
                    var previousNode =
                        from el in mergeDoc.Root.Elements()
                        where XNode.DeepEquals(el, addition.TargetElement.PreviousNode)
                        select el;

                    // Stage 1 - previous node found
                    if (previousNode != null)
                    {
                        // FIXME: single throws exception, something fishy here
                        var node = previousNode.FirstOrDefault();
                        if (node != null)
                        {
                            // Insert after
                            node.AddAfterSelf(addition.TargetElement);
                            // Remove our addition since we have processed it and continue
                            additions.Remove(addition);
                            continue;
                        }
                    }

                    // No luck - previous known node was not found. Let's look for the next one
                    var nextNode =
                        from el in mergeDoc.Root.Elements()
                        where XNode.DeepEquals(el, addition.TargetElement.NextNode)
                        select el;

                    // Stage 2 - next node found
                    if (nextNode != null)
                    {
                        var node = nextNode.FirstOrDefault();
                        if (node != null)
                        {
                            // Insert before
                            node.AddBeforeSelf(addition.TargetElement);
                            // Remove our addition since we have processed it and continue
                            additions.Remove(addition);
                            continue;
                        }
                    }

                    
                    // Stage 3 - here things are starting to get complicated and we have to revert to the alphabetical sorting
                    // !!!TODO: we might need to call this only after we're approaching endless loop - the program seems to handle most of the elements nicely so far!!!
                    // TODO: this could use some caching of the element types already searched to improve performance
                    List<string> nodeNames = new List<string>();

                    var elements =
                            from el in mergeDoc.Root.Elements(ns + addition.ElementType)
                            select el;

                    foreach(var element in elements)
                    {
                        string value = Config.ComponentDefinitions[addition.ElementType];

                        var target =
                            from el in element.Elements(ns + value)
                            select el;

                        nodeNames.Add(target.Single().Value);
                    }

                    // Now try getting previous one alphabetically
                    string likelyPreviousNodeName = GetPreviousElementName(nodeNames, addition.Name);

                    // Handle case where we found one
                    if(likelyPreviousNodeName != null)
                    {
                        var likelyPreviousNode =
                            from el in mergeDoc.Root.Elements(ns + addition.ElementType)
                            where (string)el.Element(ns + Config.ComponentDefinitions[addition.ElementType]) == likelyPreviousNodeName
                            select el;

                        XElement node = likelyPreviousNode.Single();

                        node.AddAfterSelf(addition.TargetElement);
                        additions.Remove(addition);
                    }

                    mergeProgress = (1 - (additions.Count / additionsSum)) * 100;
                    (sender as BackgroundWorker).ReportProgress((int)mergeProgress);
                }
            } // Document assembly ends here

            // Save file
            string pth = Config.Merged + ".merged";
            XMLHandlerBase.WriteXml(mergeDoc, pth);
            // Wite log
            Utils.WriteMergeResultsReport(diffStore, Config.Merged);
        }

        private string GetPreviousElementName(List<string> nodeNames, string additionName)
        {
            // Adding element we're trying to insert to list of names and sorting it
            nodeNames.Add(additionName);
            nodeNames.Sort();

            /* Check the position of our addition.
             * If it happens to be 0 it means it's the first element of the collection 
             * If it's >0, we found a suitable previous node to insert after (in most cases)
             * 
             * This will not work 100% according to SalesForce, because they're using different sorting logic,
             * however this is good enough.
            */
            int index = nodeNames.IndexOf(additionName);

            if (index == 0)
                return null;
            else
                return nodeNames[nodeNames.IndexOf(additionName) - 1];
        }
    }
}
