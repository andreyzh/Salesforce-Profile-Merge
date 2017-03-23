using System;
using System.Collections.Generic;
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
        List<Diff> diffs = new List<Diff>();

        // Add differing element
        public void Add(XElement originElement, XElement targetElement)
        {
            Diff diff = new Diff();
            diff.OriginElement = originElement;
            diff.TargetElement = targetElement;
            diffs.Add(diff);
        }

        class Diff
        {
            public string Type { get; set; }
            public XElement OriginElement { get; set; }
            public XElement TargetElement { get; set; }
        }
    }
}
