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

            // TODO: Fetch dynamically
            XNamespace ns = doc.Root.GetDefaultNamespace();

            // Outer loop: Go though all elements in the file
            foreach (var element in doc.Root.Elements())
            {
                // Inner loop - see if the component name matches
                foreach(var kvp in ComponentDefinitions)
                {
                    XElement searchResult;

                    //TODO
                    // Get element type
                    string localName = element.Name.LocalName;
                    // Figure out sub element name to look for - doesn't seem to work 'application'

                    // Key here is the type of permission e.g. AppVisisbility or apexVisibility
                    if (localName == kvp.Key)
                    {
                        string searchTerm = null;
                        foreach (var subelement in element.Elements())
                        {
                            if (subelement.Name.LocalName == kvp.Value)
                                searchTerm = subelement.Value;
                        }

                        var target =
                            from el in doc2.Root.Elements(ns + localName)
                            where (string)el.Element(ns + kvp.Value) == searchTerm
                            select el;

                        foreach (XElement el in target)
                        {
                            string t = el.Name.LocalName;
                        }

                        // Check that we have unique return
                        if (target.Count() == 1)
                        {
                            searchResult = target.Single();

                            //FIXME: very rough comparison
                            if (element.Value != searchResult.Value)
                                diffStore.Add(element, searchResult, DiffStore.Kind.Changed);
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
