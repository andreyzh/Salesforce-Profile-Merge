using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// This class is used to store differences between files
    /// </summary>
    class DifferenceStore
    {
        public List<Difference> Diffs { get { return diffs; } }

        List<Difference> diffs = new List<Difference>();

        // Add differing element
        public void Add(XElement originElement, XElement targetElement, ChangeType ChangeType)
        {
            Difference diff = new Difference()
            {
                ChangeType = ChangeType,
                ElementType = originElement.Name.LocalName.ToString(),
                OriginElement = originElement,
                TargetElement = targetElement
            };

            diffs.Add(diff);
        }

        public void Clear()
        {
            diffs.Clear();
        }

        internal class Difference
        {
            // Indidates whether or not change needs to be merged
            public bool Merge { get; set; }
            // Name of the element e.g. apex class name
            public string Name
            {
                get { return getComponentName(); }
            }
            public ChangeType ChangeType { get; set;}
            // Type of the element as declared in metadata file
            public string ElementType { get; set; }
            public XElement OriginElement { get; set; }
            public XElement TargetElement { get; set; }

            // Get the name of the component e.g. name of class or sObject
            private string getComponentName()
            {
                if (OriginElement == null)
                    return null;

                XNamespace ns = OriginElement.Document.Root.GetDefaultNamespace();//doc.Root.GetDefaultNamespace();
                string value = Config.ComponentDefinitions[ElementType];

                var target =
                    from el in OriginElement.Elements(ns + value)
                    select el;

                return target.Single().Value;
            }
        }

        // Tells whether diff is for new item or changed item
        public enum ChangeType
        {
            None,
            New,
            Changed,
            Deleted
        }
    }
}
