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
            xmlHandler.Analyze();
        }
    }

    class XMLHandler
    {
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

        public void Analyze()
        {
            List<string> values = new List<string>();

            foreach (var xyz in doc.Root.Elements())
            {
                values.Add(xyz.Name.LocalName.ToString());
            }

            var distinct = values.Distinct();
        }
    }
}
