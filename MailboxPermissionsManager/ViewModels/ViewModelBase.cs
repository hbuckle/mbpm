using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MailboxPermissionsManager.Model;
using MailboxPermissionsManager.PowerShellCommands;
using MailboxPermissionsManager.Utilities;
using System.ComponentModel;
using System.Collections.ObjectModel;

namespace MailboxPermissionsManager.ViewModels
{
    class ViewModelBase : Notifier
    {
        private string status;
        protected BackgroundWorker worker;
        protected static System.Management.Automation.PSCredential credentials;

        public string Status
        {
            get { return status; }
            set
            {
                status = value;
                OnPropertyChanged("Status");
            }
        }

        public BackgroundWorker Worker
        { get { return worker; } }

        protected virtual void configureWorker()
        {
            worker.DoWork += (sender, e) =>
                {
                    //BackgroundWorker bw = sender as BackgroundWorker;
                    e.Result = ((Func<DoWorkEventArgs, Action<RunWorkerCompletedEventArgs>>)e.Argument)(e);
                };
            worker.RunWorkerCompleted += (sender, e) =>
                {
                    ((Action<RunWorkerCompletedEventArgs>)e.Result)(e);
                };
        }

        public static List<MailboxWrapper> MasterAddressList
        { get; set; }

        public ViewModelBase()
        {
            worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            configureWorker();
            if (MasterAddressList == null)
                MasterAddressList = new List<MailboxWrapper>();
        }
    }
}