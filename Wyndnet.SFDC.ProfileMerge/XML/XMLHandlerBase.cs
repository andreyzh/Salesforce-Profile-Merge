using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    static class XMLHandlerBase
    {
        public static XDocument Local { get; private set; }
        public static XDocument Remote { get; private set; }

        static XmlWriterSettings settings = new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = true,
            IndentChars = "    ",
            NewLineChars = "\n"
        };

        /// <summary>
        /// Initialize XMLs to compare in memory
        /// </summary>
        /// <param name="localPath">Path to XML file: Local for Merge Mode, Source in case of Compare Mode</param>
        /// <param name="remotePath">Path to XML file: Remote for Merge Mode, Target in Compare Mode</param>
        public static void Init(string localPath, string remotePath)
        {
            if (localPath != null && remotePath != null)
            {
                 Local = XDocument.Load(localPath);
                 Remote = XDocument.Load(remotePath);
            }
            else throw new ArgumentException("Unable to load XMLs");
        }

        /// <summary>
        /// Clears XDocuments in memory
        /// </summary>
        public static void Clear()
        {
            Local = null;
            Remote = null;
        }

        /// <summary>
        /// Write XML to a file
        /// </summary>
        /// <param name="xml">XML to write</param>
        /// <param name="filePath">Path to save the file</param>
        public static void WriteXml(XDocument xml, string filePath)
        {
            using (var writer = XmlWriter.Create(filePath, settings))
            {
                xml.Save(writer);
            }
        }
    }
}
