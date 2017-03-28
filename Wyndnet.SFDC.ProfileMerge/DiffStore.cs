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
        public List<Diff> Diffs { get { return diffs; } }

        List<Diff> diffs = new List<Diff>();

        // Add differing element
        public void Add(XElement originElement, XElement targetElement, Kind ChangeType)
        {
            Diff diff = new Diff()
            {
                Kind = ChangeType,
                Type = originElement.Name.LocalName.ToString(),
                OriginElement = originElement,
                TargetElement = targetElement
            };
            diffs.Add(diff);
        }

        internal class Diff
        {
            public Kind Kind { get; set;}
            public string Type { get; set; }
            public XElement OriginElement { get; set; }
            public XElement TargetElement { get; set; }
        }

        // Tells whether diff is for new item or changed item
        public enum Kind
        {
            New,
            Changed
        }
    }
}
