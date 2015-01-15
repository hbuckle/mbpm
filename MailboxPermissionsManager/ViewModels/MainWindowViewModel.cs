using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Collections.ObjectModel;
using MailboxPermissionsManager.Model;
using MailboxPermissionsManager.Views;
using MailboxPermissionsManager.PowerShellCommands;
using MailboxPermissionsManager.Utilities;
using System.ComponentModel;
using System.Management.Automation;
using System.Windows.Input;
using System.Windows.Data;

namespace MailboxPermissionsManager.ViewModels
{
    class MainWindowViewModel : ViewModelBase
    {
        private BackgroundWorker openRunspaceWorker;
        private BackgroundWorker getMailboxesWorker;
        private Collection<PSObject> psMailboxes;
        private Collection<PSObject> psGroups;
        private ListCollectionView userMailboxesView;

        public MainWindowViewModel()
        {
            psMailboxes = new Collection<PSObject>();
            psGroups = new Collection<PSObject>();
            MailboxViewModels = new ObservableCollection<MailboxViewModel>();
            UserMailboxesView = new ListCollectionView(MasterAddressList);
            UserMailboxesView.Filter = obj => (((MailboxWrapper)obj).RecipientType == "UserMailbox");
            openRunspaceWorker = new BackgroundWorker();
            getMailboxesWorker = new BackgroundWorker();
            
            SelectMailboxCommand = new Command
            {
                CanExecuteDelegate = () => UserMailboxesView.CurrentItem != null,
                ExecuteDelegate = x => addMailboxViewModelTab()
            };
            RefreshMailboxesCommand = new Command
            {
                CanExecuteDelegate = () => (!getMailboxesWorker.IsBusy && !openRunspaceWorker.IsBusy),
                ExecuteDelegate = getMailboxesWorker.RunWorkerAsync
            };
            CloseCommand = new Command
            {
                ExecuteDelegate = x =>
                    {
                        if (((MailboxViewModel)x).CanClose)
                        RemoveMailboxViewModelTab((MailboxViewModel)x);
                    }
            };
            openRunspaceWorker.DoWork += openRunspaceWorker_DoWork;
            openRunspaceWorker.RunWorkerCompleted += openRunspaceWorker_RunWorkerCompleted;
            getMailboxesWorker.DoWork += getMailboxesWorker_DoWork;
            getMailboxesWorker.RunWorkerCompleted += getMailboxesWorker_RunWorkerCompleted;
            establishCredentials();
        }

        private void establishCredentials()
        {
            try
            {
                Settings.Load();
                if (Settings.UseDefaultCredentials)
                    credentials = (PSCredential)null;
                else
                {
                    Credentials credsEntry = new Credentials();
                    credsEntry.ServerName.Text = Settings.ServerUri;
                    credsEntry.username.Focus();
                    Nullable<bool> result = credsEntry.ShowDialog();
                    if (result.Value)
                    {
                        credentials = new PSCredential(credsEntry.username.Text, credsEntry.password.SecurePassword);
                    }
                    else
                        throw new PSSecurityException();
                }
                openRunspaceWorker.RunWorkerAsync();
            }
            catch (System.IO.FileNotFoundException)
            {
                System.Windows.MessageBox.Show("Could not find Settings.xml");
            }
            catch (System.InvalidOperationException)
            {
                System.Windows.MessageBox.Show("Invalid credentials");
            }
            catch (PSSecurityException)
            {
                System.Windows.MessageBox.Show("Invalid credentials");
            }
        }

        private void openRunspaceWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Status = "Connecting to Exchange server";
            try
            {
                Exchange.OpenRunspace(credentials);
            }
            catch (System.Management.Automation.Remoting.PSRemotingTransportException E)
            {
                System.Windows.MessageBox.Show(E.Message);
                e.Cancel = true;
            }
            catch (PSInvalidOperationException E)
            {
                System.Windows.MessageBox.Show(E.Message);
                e.Cancel = true;
            }
        }

        private void openRunspaceWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled == false)
                getMailboxesWorker.RunWorkerAsync();
        }

        private void getMailboxesWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Status = "Retrieving mailboxes";
            openRunspaceWorker.Dispose();
            psMailboxes = Exchange.GetMailboxes();
            psGroups = Exchange.GetGroups();
        }

        private void getMailboxesWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            MasterAddressList.Clear();
            foreach (PSObject psMailbox in psMailboxes)
            {
                MasterAddressList.Add(new MailboxWrapper(psMailbox));
            }
            foreach (PSObject psGroup in psGroups)
            {
                MasterAddressList.Add(new MailboxWrapper(psGroup));
            }
            UserMailboxesView.Refresh();
            Status = "";
        }

        private void addMailboxViewModelTab()
        {
            MailboxWrapper selected = (MailboxWrapper)UserMailboxesView.CurrentItem;
            //Check if the selected mailbox is already open, and if it is make it the selected tab.
            MailboxViewModel dup = MailboxViewModels.SingleOrDefault(x => x.Mailbox.DistinguishedName == selected.DistinguishedName);

            if (dup == null)
            {
                MailboxViewModel newTab = new MailboxViewModel(selected);
                newTab.IsSelected = true;
                MailboxViewModels.Add(newTab);
            }
            else
                dup.IsSelected = true;
        }

        public void RemoveMailboxViewModelTab(MailboxViewModel tabToRemove)
        {
            MailboxViewModels.Remove(tabToRemove);
        }

        public ListCollectionView UserMailboxesView
        {
            get { return this.userMailboxesView; }
            private set
            {
                if (value == this.userMailboxesView)
                {
                    return;
                }
                userMailboxesView = value;
                OnPropertyChanged("UserMailboxesView");
            }
        }

        public ObservableCollection<MailboxViewModel> MailboxViewModels
        { get; private set; }

        public ICommand SelectMailboxCommand
        { get; private set; }

        public ICommand RefreshMailboxesCommand
        { get; private set; }

        public ICommand CloseCommand
        { get; private set; }
    }
}
