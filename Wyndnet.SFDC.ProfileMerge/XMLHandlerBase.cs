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


        public static void Init(string localPath, string remotePath)
        {
            if (localPath != null && remotePath != null)
            {
                 Local = XDocument.Load(localPath);
                 Remote = XDocument.Load(remotePath);
            }
            else throw new ArgumentException("Unable to load XMLs");
        }

        public static void WriteXml(XDocument xml, string filePath)
        {
            using (var writer = XmlWriter.Create(filePath, settings))
            {
                xml.Save(writer);
            }
        }
    }
}
