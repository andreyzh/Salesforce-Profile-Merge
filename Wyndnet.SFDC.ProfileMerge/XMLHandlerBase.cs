using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    static class XMLHandlerBase
    {
        public static XDocument Local { get; private set; }
        public static XDocument Remote { get; private set; }
        //public static Dictionary<string, string> ComponentDefinitions { get; private set; }


        public static void Init(string localPath, string remotePath)
        {
            if (localPath != null && remotePath != null)
            {
                Local = XDocument.Load(localPath);
                Remote = XDocument.Load(remotePath);

            }
            else throw new ArgumentException("Unable to load XMLs");
        }
    }
}
