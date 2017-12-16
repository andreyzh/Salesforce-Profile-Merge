using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    class Utils
    {
        //Implemented based on interface, not part of algorithm
        public static string RemoveAllNamespaces(string xmlDocument)
        {
            XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(xmlDocument));

            return xmlDocumentWithoutNs.ToString();
        }

        public static string ConvertUnixPathToWindows(string path)
        {
            if (String.IsNullOrEmpty(path))
                return null;

            // Starting with '.' (dot) - strip the dot
            if (path.StartsWith("."))
                path = path.Remove(0, 1);
            
            // Replace Unix separators with windows
            path = path.Replace('/','\\');

            return path;
        }

        //Core recursion function
        private static XElement RemoveAllNamespaces(XElement xmlDocument)
        {
            if (!xmlDocument.HasElements)
            {
                XElement xElement = new XElement(xmlDocument.Name.LocalName);
                xElement.Value = xmlDocument.Value;

                foreach (XAttribute attribute in xmlDocument.Attributes())
                    xElement.Add(attribute);

                return xElement;
            }
            return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
        }

        /* Utility for writing out merge results
         * Done as csv with columns: Change type, Element Type, Element Name
        */
        public static void WriteMergeResultsReport(DifferenceStore changes, string path)
        {
            List<string> output = new List<string>();

            StringBuilder sb = new StringBuilder();
            string separator = ";";

            // Write header
            sb.Append("Change type");
            sb.Append(separator);
            sb.Append("Element type");
            sb.Append(separator);
            sb.Append("Element name");
            output.Add(sb.ToString());

            foreach (var change in changes.Diffs.Where(d => d.Merge))
            {
                sb.Clear();

                sb.Append(change.ChangeType.ToString());
                sb.Append(separator);
                sb.Append(change.ElementType);
                sb.Append(separator);
                sb.Append(change.Name);

                output.Add(sb.ToString());
            }

            using (StreamWriter sw = new StreamWriter(path + ".log.csv", false))
            {
                foreach(var line in output)
                {
                    sw.WriteLine(line);
                }
            }
        }
    }
}
