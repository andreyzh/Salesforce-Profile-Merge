﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Wyndnet.SFDC.ProfileMerge
{
    class AsyncJobsController
    {
        public EventHandler<AsyncJobCompletedEventArgs> Completed;

        private readonly bool mergeMode;
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
            worker.RunWorkerCompleted += AnalyzeWorkerRunCompleted;
            worker.RunWorkerAsync();
        }

        public void RunMerge()
        {
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += MergeXmlWork;
            worker.RunWorkerCompleted += MergeWorkerRunCompleted;
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
            // Scan for deletions and valid additions if we're in merge mode
            if (mergeMode)
            {
                // Calculate the differences
                xmlPermissionsHandler.Analyze();

                MetadataComponentScanner scanner = new MetadataComponentScanner(Environment.CurrentDirectory);
                InnerXmlComponentScanner scanner1 = new InnerXmlComponentScanner(Environment.CurrentDirectory);
                scanner.Scan(diffStore);
                scanner1.Scan(diffStore);
            }
            else
            {
                // Calculate the differences
                xmlPermissionsHandler.Analyze();
            }
        }

        // 
        void MergeXmlWork(object sender, DoWorkEventArgs e)
        {
            XMLMergeHandler mergeHandler = new XMLMergeHandler();

            mergeHandler.Merge(diffStore, null);
        }

        // This assembles the data for the return to the caller
        private void AnalyzeWorkerRunCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AsyncJobCompletedEventArgs eventArgs = new AsyncJobCompletedEventArgs();

            if (e.Error != null)
            {
                eventArgs.AsyncAction = AsyncAction.Error;
                eventArgs.Exception = e.Error;
                AsyncProcessingCompleted(eventArgs);
            }
            else
            { 
                eventArgs.DiffStore = diffStore;
                eventArgs.AsyncAction = AsyncAction.Analyse;
                eventArgs.Exception = e.Error;
                AsyncProcessingCompleted(eventArgs);
            }
        }

        // This assembles the data for the return to the caller
        private void MergeWorkerRunCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            AsyncJobCompletedEventArgs eventArgs = new AsyncJobCompletedEventArgs();

            if (e.Error != null)
            {
                eventArgs.AsyncAction = AsyncAction.Error;
                eventArgs.Exception = e.Error;
                AsyncProcessingCompleted(eventArgs);
            }
            else
            {
                eventArgs.DiffStore = diffStore;
                eventArgs.AsyncAction = AsyncAction.Merge;
                eventArgs.Exception = e.Error;
                AsyncProcessingCompleted(eventArgs);
            }
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

    enum AsyncAction { Analyse, Merge, Error }
}
