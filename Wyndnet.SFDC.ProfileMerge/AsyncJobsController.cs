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
            worker.DoWork += AnalyzeDiffsWork;
            worker.RunWorkerCompleted += WorkerRunCompleted;
            worker.RunWorkerAsync();
        }

        public void RunMerge()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += MergeXmlWork;
            worker.RunWorkerCompleted += WorkerRunCompleted;
            worker.ProgressChanged += MergeXmlProgressChanged;
            worker.RunWorkerAsync();
        }

        protected virtual void AsyncProcessingCompleted(AsyncJobCompletedEventArgs e)
        {
            // This replaces null check if eventHandler != null -> execute eventHandler
            Completed?.Invoke(this, e);
        }
        
        private void AnalyzeDiffsWork(object sender, DoWorkEventArgs e)
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

        void MergeXmlWork(object sender, DoWorkEventArgs e)
        {
            XMLMergeHandler mergeHandler = new XMLMergeHandler();
            try
            {
                mergeHandler.Merge(diffStore, null);
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // This assembles the data for the return to the caller
        private void WorkerRunCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AsyncJobCompletedEventArgs eventArgs = new AsyncJobCompletedEventArgs();
            eventArgs.DiffStore = diffStore;
            eventArgs.AsyncAction = AsyncAction.Analyse;
            AsyncProcessingCompleted(eventArgs);
        }       

        // Not in use
        void MergeXmlProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //progressBar.Value = e.ProgressPercentage;
        }
    }

    /// <summary>
    /// Holds data returned by async job
    /// </summary>
    class AsyncJobCompletedEventArgs : EventArgs
    {
        public DifferenceStore DiffStore { get; set; }
        public AsyncAction AsyncAction { get; set; }
        public Exception Exception { get; set; }
    }

    enum AsyncAction { Analyse, Merge }
}
