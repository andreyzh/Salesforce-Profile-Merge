using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    static class Config
    {
        public static Dictionary<string, string> LoadComponentDefinitions()
        {
            Dictionary<string, string> componentDefinitions = new Dictionary<string, string>();

            try
            { 
                XDocument doc = XDocument.Load("components.xml");
                var results = from component in doc.Root.Elements()
                              select new
                              {
                                  type = component.Attribute("type").Value.ToString(),
                                  name = component.Attribute("name").Value.ToString()
                              };

                foreach(var result in results)
                {
                    componentDefinitions.Add(result.type, result.name);
                }
            }
            catch(IOException ex)
            {
                MessageBox.Show("Unable to find component definitions file components.xml\nWill now quit.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                Application.Current.Shutdown();
            }

            return componentDefinitions;
        }
    }
}
