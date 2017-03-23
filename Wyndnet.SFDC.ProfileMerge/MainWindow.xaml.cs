using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;

namespace Wyndnet.SFDC.ProfileMerge
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        XMLHandler xmlHandler = new XMLHandler();
        DiffStore diffStore = new DiffStore();

        public MainWindow()
        {
            InitializeComponent();
        }

        //TEMP: Click handler to load initial XML file
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            if(openFileDialog.ShowDialog() == true)
            {
                xmlHandler.LoadXml(openFileDialog.FileName);
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            
            xmlHandler.Analyze(diffStore);
        }
    }

    class XMLHandler
    {
        DiffStore diffStore;
        XDocument doc;
        XDocument doc2;

        public XMLHandler()
        {
        }

        public void LoadXml(string path)
        {
            if(doc == null)
            { 
                doc = XDocument.Load(path);
            }
            else
            {
                doc2 = XDocument.Load(path);
            }          
        }

        public void Analyze(DiffStore diffStore)
        {
            this.diffStore = diffStore;

            //FIXME: Temp handler for null results
            if (doc == null || doc2 == null)
                return;

            XNamespace ns = "http://soap.sforce.com/2006/04/metadata";
            
            foreach (var element in doc.Root.Elements())
            {
                XElement searchResult;

                //TODO
                // Get element type
                string localName = element.Name.LocalName;
                // Figure out sub element name to look for - doesn't seem to work 'application'

                if(localName == "applicationVisibilities")
                {
                    string searchTerm = null;
                    foreach(var subelement in element.Elements())
                    {
                        if (subelement.Name.LocalName == "application")
                            searchTerm = subelement.Value;
                    }

                    var target =
                        from el in doc2.Root.Elements(ns + localName)
                        where (string)el.Element(ns + "application") == searchTerm
                        select el;

                    foreach(XElement el in target)
                    {
                        string t = el.Name.LocalName;
                    }

                    // Check that we have unique return
                    if (target.Count() == 1)
                    { 
                        searchResult = target.Single();

                        //FIXME: very rough comparison
                        if (element.Value != searchResult.Value)
                            diffStore.Add(element, searchResult);
                    }
                    
                }
            }
        }
    }
}

/*
 * applicationVisibilities
 * classAccesses
 * custom
 * fieldPermissions
 * layoutAssignments
 * loginIpRanges
 * objectPermissions
 * pageAccesses
 * recordTypeVisibilities
 * tabVisibilities
 * userLicense
 * userPermissions
*/
