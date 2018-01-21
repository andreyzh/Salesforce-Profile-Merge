using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Wyndnet.SFDC.ProfileMerge
{
    class AsyncJobsController
    {
        public EventHandler<AsyncJobCompletedEventArgs> Completed;

        private bool mergeMode;
        private DifferenceStore diffStore;
        private XMLPermissionsHandler xmlPermissionsHandler;

        public AsyncJobsController(DifferenceStore differenceStore, bool mergeMode)
        {
            if(diffStore == null)
                diffStore = new DifferenceStore();

            this.mergeMode = mergeMode;
        }

        public void RunAnalyis()
        {
            if(xmlPermissionsHandler == null)
                xmlPermissionsHandler = new XMLPermissionsHandler();
            xmlPermissionsHandler.DiffStore = diffStore;

            BackgroundWorker worker = new BackgroundWorker();
            worker.DoWork += AnalyzeDiffs;
            worker.RunWorkerCompleted += WorkerRunCompleted;
            worker.RunWorkerAsync();
        }

        public DifferenceStore RunMerge()
        {
            return diffStore;
        }

        protected virtual void AsyncProcessingCompleted(AsyncJobCompletedEventArgs e)
        {
            // This replaces null check if eventHandler != null -> execute eventHandler
            Completed?.Invoke(this, e);
        }

        
        private void AnalyzeDiffs(object sender, DoWorkEventArgs e)
        {
            // Calculate the differences
            xmlPermissionsHandler.Analyze();

            // Scan for deletions and valid additions if we're in merge mode
            if (mergeMode)
            {
                MetadataComponentScanner scanner = new MetadataComponentScanner(Environment.CurrentDirectory);
                InnerXmlComponentScanner scanner1 = new InnerXmlComponentScanner(Environment.CurrentDirectory);
                scanner.Scan(diffStore);
                scanner1.Scan(diffStore);
            }
        }

        private void WorkerRunCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AsyncJobCompletedEventArgs eventArgs = new AsyncJobCompletedEventArgs();
            eventArgs.DiffStore = diffStore;
            AsyncProcessingCompleted(eventArgs);
        }
    }

    /// <summary>
    /// ?
    /// </summary>
    class AsyncJobCompletedEventArgs : EventArgs
    {
        public DifferenceStore DiffStore { get; set; }
    }
}
