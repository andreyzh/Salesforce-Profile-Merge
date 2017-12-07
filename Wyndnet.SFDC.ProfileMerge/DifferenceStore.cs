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
            string type = null;
            // We don't really care which type we set as if this is a change they'll be equal
            if (originElement != null)
                type = originElement.Name.LocalName.ToString();
            else
                type = targetElement.Name.LocalName.ToString();

            Difference diff = new Difference()
            {
                ChangeType = ChangeType,
                ElementType = type,
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
                get { return GetComponentName(); }
            }
            public string ParentObject
            {
                get { return GetParentObject(); }
            }
            public string FieldName
            {
                get { return GetFieldName(); }
            }

            public ChangeType ChangeType { get; set;}
            public ChangeSource ChangeSource
            {
                get { return GetChangeSource(); }
            }
            // Type of the element as declared in metadata file
            public string ElementType { get; set; }
            /// <summary>
            /// XML Change node of the local version
            /// </summary>
            public XElement OriginElement { get; set; }
            /// <summary>
            /// XML Change node of the remote version
            /// </summary>
            public XElement TargetElement { get; set; }

            // Get the name of the component e.g. name of class or sObject
            private string GetComponentName()
            {
                XElement element = GetXElement();

                if (element == null)
                    return null;

                XNamespace ns = element.Document.Root.GetDefaultNamespace();//doc.Root.GetDefaultNamespace();
                string value = Config.ComponentDefinitions[ElementType];

                var target =
                    from el in element.Elements(ns + value)
                    select el;

                return target.Single().Value;
            }

            private string GetParentObject()
            {
                if (ElementType == "fieldPermissions" || ElementType == "recordTypeVisibilities")
                {
                    string componentName = GetComponentName();
                    char delimiter = '.';
                    return componentName.Split(delimiter)[0];
                }
                else
                    return null;
            }

            private ChangeSource GetChangeSource()
            {
                if (OriginElement == null && TargetElement != null)
                    return ChangeSource.Remote;
                if (OriginElement != null && TargetElement == null)
                    return ChangeSource.Local;
                else
                    return ChangeSource.None;
            }

            private string GetFieldName()
            {
                if (ElementType == "fieldPermissions" || ElementType == "recordTypeVisibilities")
                {
                    string componentName = GetComponentName();
                    char delimiter = '.';
                    return componentName.Split(delimiter)[1];
                }
                else
                    return null;
            }

            private XElement GetXElement()
            {
                if (OriginElement != null)
                    return OriginElement;
                else
                    return TargetElement;
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
        public enum ChangeSource
        {
            None,
            Local,
            Remote
        }
    }
}
