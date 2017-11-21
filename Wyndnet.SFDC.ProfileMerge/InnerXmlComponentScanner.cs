using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Wyndnet.SFDC.ProfileMerge
{
    class InnerXmlComponentScanner : IComponentScaner
    {
        // Key is type e.g. apexclass, value is name e.g. Utils
        Dictionary<string, string> components;
        string projectPath = null;

        public InnerXmlComponentScanner(string projectPath)
        {
            this.projectPath = projectPath;
            components = new Dictionary<string, string>();
        }

        public DifferenceStore Scan(DifferenceStore diffStore)
        {
            List<string> objects = new List<string>();
            
            // Our candidates are new or deleted objects which refer to parent object
            var candidates = diffStore.Diffs.Where(
                candidate =>
                ((candidate.ChangeType == DifferenceStore.ChangeType.Deleted || candidate.ChangeType == DifferenceStore.ChangeType.New)
                && candidate.ParentObject != null
                ));

            // Get distinct objects to scan
            foreach(var candidate in candidates)
            {
                if (!objects.Contains(candidate.ParentObject))
                    objects.Add(candidate.ParentObject);
            }

            XMLObjectHandler handler = new XMLObjectHandler();

            // This part will be different
            foreach (string obj in objects)
            {
                string path = Environment.CurrentDirectory + "\\src\\objects\\" + obj + ".object";

                handler.Analyze(path);

                //break;
            }

            foreach(var candidate in candidates)
            {
                var obj = handler.Objects.Find(o => o.Name == candidate.ParentObject);

                // Field marked as addition and is present in metadata - must be a valid addition
                if(obj.Fields.Contains(candidate.FieldName) && candidate.ChangeType == DifferenceStore.ChangeType.New)
                {
                    candidate.Merge = true;
                }
                // Field marked as deletion and not present in metadada - must be a valid deletion
                else if(!obj.Fields.Contains(candidate.FieldName) && candidate.ChangeType == DifferenceStore.ChangeType.Deleted)
                {
                    candidate.Merge = true;
                }
            }

            return diffStore;
        }
    }
}
