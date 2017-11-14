using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    class XMLObjectHandler
    {
        XDocument customObject;

        public List<SObject> Objects { get { return objects; } }

        List<SObject> objects = new List<SObject>();

        // Loads XMLs from a given path
        public void LoadXml(string path)
        {
            
        }

        public void Analyze(string path)
        {
            try { customObject = XDocument.Load(path); }
            catch (Exception ex) { }

            List<string> results = new List<string>();

            SObject obj = new SObject();
            obj.Name = Path.GetFileNameWithoutExtension(path);

            XNamespace ns = customObject.Root.GetDefaultNamespace();

            // Outer loop: Go though all elements in the file
            foreach (var element in customObject.Root.Elements())
            {
                if(element.Name.LocalName == "fields")
                {
                    var field =
                            from el in element.Elements()
                            where el.Name.LocalName == "fullName"
                            select el;

                    if(field.Count() > 0)
                    {
                        XElement result = field.Single();
                        obj.Fields.Add(result.Value.ToString());
                    }
                    
                }
            }
        }

        internal class SObject
        {
            public string Name { get; set; } 
            public List<string> Fields { get { return fields; } }
            public List<string> RecordTypes { get; set; }

            private List<string> fields = new List<string>();
        }
    }
}
