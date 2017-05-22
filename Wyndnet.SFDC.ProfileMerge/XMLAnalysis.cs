using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    class XMLAnalysis
    {
        Dictionary<string, string> componentDefinitions;
        XDocument sourceDoc;
        XDocument targetDoc;
        XNamespace nameSpace;

        public XMLAnalysis(Dictionary<string, string> componentDefinitions, XDocument sourceDoc, XDocument targetDoc)
        {
            if(componentDefinitions == null || sourceDoc == null || targetDoc == null)
                return;

            this.componentDefinitions = componentDefinitions;
            this.sourceDoc = sourceDoc;
            this.targetDoc = targetDoc;
            nameSpace = sourceDoc.Root.GetDefaultNamespace();
        }

        public DiffStore Analyse(DiffStore diffStore)
        {
            // Go though all elements and find changes and additions
            foreach (var element in sourceDoc.Root.Elements())
            {
                // Inner loop - see if the component name matches
                foreach (var kvp in componentDefinitions)
                {
                    XElement searchResult;

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
                            from el in targetDoc.Root.Elements(nameSpace + localName)
                            where (string)el.Element(nameSpace + kvp.Value) == searchTerm
                            select el;

                        // Check that we have unique return 
                        if (target.Count() == 1)
                        {
                            searchResult = target.Single();

                            //FIXME: very rough comparison
                            if (element.Value != searchResult.Value)
                                diffStore.Add(element, searchResult, DiffStore.ChangeType.Changed);
                        }
                        // If we have no return it means that the item is not present in target XML and we mark it as new
                        if (target.Count() == 0)
                        {
                            diffStore.Add(element, null, DiffStore.ChangeType.New);
                        }
                    }
                }
            }

            // Go though all elements, but this time scan for deletions
            // We are now looking at target document and checking if it doen't have something present in source
            foreach (var element in targetDoc.Root.Elements())
            {

            }
            return diffStore;
        }
    }
}
