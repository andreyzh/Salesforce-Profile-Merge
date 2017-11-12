using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// Collects the component names availabe though project (filesystem) scan for file names.
    /// This is appliable for e.g. classes, pages, custom objects
    /// </summary>
    class MetadataComponentScanner : IComponentScaner
    {
        // Key is type e.g. apexclass, value is name e.g. Utils
        Dictionary<string, string> components;
        string projectPath = null;

        public MetadataComponentScanner(string projectPath)
        {
            this.projectPath = projectPath;
            components = new Dictionary<string, string>();
        }

        public DifferenceStore Scan(DifferenceStore diffStore)
        {
            throw new NotImplementedException();
        }
    }
}
