using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    // Currently it's multi-purpose class meant to do everything related to XML analysis
    //TODO: Refactor
    class XMLHandler
    {
        public Dictionary<string, string> ComponentDefinitions { get; set; }
        DiffStore diffStore;
        XDocument sourceDoc;
        XDocument targetDoc;

        // Loads XMLs from a given path
        public void LoadXml(string path)
        {
            if (sourceDoc == null)
            {
                sourceDoc = XDocument.Load(path);
            }
            else
            {
                targetDoc = XDocument.Load(path);
            }
        }

        // Analyse differences between the input files, add to diff holder as new or changed
        public void Analyze(DiffStore diffStore)
        {
            this.diffStore = diffStore;

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

        // TODO
        public void MergeChanges(DiffStore diffStore)
        {
            // We don't want anything to happen to originals
            XDocument mergeDoc = new XDocument(targetDoc);
            XNamespace ns = mergeDoc.Root.GetDefaultNamespace();

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

            XmlWriterSettings settings = new XmlWriterSettings();
            settings.Encoding = new UTF8Encoding(false);
            settings.Indent = true;
            settings.IndentChars = "    ";
            settings.NewLineChars = "\n";

            using (var writer = XmlWriter.Create("merged.xml", settings))
            {
                mergeDoc.Save(writer);
            }

            //using (var writer = new XmlTextWriter("merged.xml", new UTF8Encoding(false)))
            //{
                

            //    writer.Formatting = Formatting.Indented;
            //    writer.Indentation = 4;
            //    mergeDoc.Save(writer);
            //}
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
