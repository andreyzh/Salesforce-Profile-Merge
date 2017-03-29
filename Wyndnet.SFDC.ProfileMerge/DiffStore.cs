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
    /// This class is used to store differenced between files
    /// </summary>
    class DiffStore
    {
        public List<Change> Diffs { get { return diffs; } }

        List<Change> diffs = new List<Change>();

        // Add differing element
        public void Add(XElement originElement, XElement targetElement, ChangeType ChangeType)
        {
            Change diff = new Change()
            {
                ChangeType = ChangeType,
                ElementType = originElement.Name.LocalName.ToString(),
                OriginElement = originElement,
                TargetElement = targetElement
            };
            diffs.Add(diff);
        }

        internal class Change
        {
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
            New,
            Changed
        }
    }
}
