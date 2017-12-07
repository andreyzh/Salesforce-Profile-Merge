using System;
using System.Collections.Generic;
using System.Linq;

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
                ((candidate.ChangeType == DifferenceStore.ChangeType.New)
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
            }

            var local = DifferenceStore.ChangeSource.Local;
            var remote = DifferenceStore.ChangeSource.Remote;

            foreach (var change in candidates)
            {
                var obj = handler.Objects.Find(o => o.Name == change.ParentObject);

                // CASE 3
                if(obj.Fields.Contains(change.FieldName) && change.ChangeSource == local)
                {
                    change.ChangeType = DifferenceStore.ChangeType.Deleted;
                    change.Merge = false;
                }

                // CASE 4
                else if(!obj.Fields.Contains(change.FieldName) && change.ChangeSource == local)
                {
                    change.ChangeType = DifferenceStore.ChangeType.Deleted;
                    change.Merge = true;
                }

                // CASE 5
                if (obj.Fields.Contains(change.FieldName) && change.ChangeSource == remote)
                {
                    change.ChangeType = DifferenceStore.ChangeType.New;
                    change.Merge = true;
                }

                // CASE 6
                else if (!obj.Fields.Contains(change.FieldName) && change.ChangeSource == remote)
                {
                    change.ChangeType = DifferenceStore.ChangeType.Deleted;
                    change.Merge = false;
                }
            }

            return diffStore;
        }
    }
}
