using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    // Currently it's multi-purpose class meant to do everything related to XML analysis
    //TODO: Refactor
    class XMLHandler
    {
        public Dictionary<string, string> ComponentDefinitions { get; set; }
        DiffStore diffStore;
        XDocument doc;
        XDocument doc2;

        // Loads XMLs from a given path
        public void LoadXml(string path)
        {
            if (doc == null)
            {
                doc = XDocument.Load(path);
            }
            else
            {
                doc2 = XDocument.Load(path);
            }
        }

        public void Analyze(DiffStore diffStore)
        {
            this.diffStore = diffStore;

            //FIXME: Temp handler for null results
            if (doc == null || doc2 == null)
                return;

            XNamespace ns = doc.Root.GetDefaultNamespace();

            // Outer loop: Go though all elements in the file
            foreach (var element in doc.Root.Elements())
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
                            from el in doc2.Root.Elements(ns + localName)
                            where (string)el.Element(ns + kvp.Value) == searchTerm
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
                        if(target.Count() == 0)
                        {
                            diffStore.Add(element, null, DiffStore.ChangeType.New);
                        }
                    }
                }
            }
        }

        private void GetApplicationTypes()
        {
            List<string> values = new List<string>();

            foreach (var type in doc.Root.Elements())
            {
                values.Add(type.Name.LocalName.ToString());
            }

            var valuesDistinct = values.Distinct();
        }

    }
}
