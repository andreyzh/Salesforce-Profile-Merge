﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// This class will parse Salesforce .object files
    /// </summary>
    class XMLObjectHandler
    {
        XDocument customObject;

        /// <summary>
        /// Returns list of SalesForce objects with fields and record types
        /// </summary>
        public List<SObject> Objects { get { return objects; } }

        List<SObject> objects = new List<SObject>();

        // Loads XMLs from a given path
        public void LoadXml(string path)
        {
            
        }

        /// <summary>
        /// Parse .object file
        /// </summary>
        /// <param name="path">path to the .object file</param>
        public void Analyze(string path)
        {
            try { customObject = XDocument.Load(path); }
            catch (Exception ex) { }

            List<string> results = new List<string>();

            SObject obj = new SObject
            {
                Name = Path.GetFileNameWithoutExtension(path)
            };

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

            objects.Add(obj);
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
