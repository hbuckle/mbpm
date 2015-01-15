using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MailboxPermissionsManager.Views;
using MailboxPermissionsManager.Model;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Management.Automation;
using MailboxPermissionsManager.PowerShellCommands;
using MailboxPermissionsManager.Utilities;
using System.Windows.Input;
using System.Windows.Data;

namespace MailboxPermissionsManager.ViewModels
{
    class MailboxViewModel : ViewModelBase
    {
        private BackgroundWorker getFoldersWorker;
        private bool isSelected;
        private ListCollectionView mailboxesAndGroupsView;

        public MailboxViewModel(MailboxWrapper mailbox)
        {
            Mailbox = mailbox;
            Folders = new ObservableCollection<MailboxFolderViewModel>();
            Delegates = new DelegatesViewModel(this);
            getFoldersWorker = new BackgroundWorker();
            getFoldersWorker.DoWork += getFoldersWorker_DoWork;
            getFoldersWorker.RunWorkerCompleted += getFoldersWorker_RunWorkerCompleted;
            getFoldersWorker.RunWorkerAsync();

            MailboxesAndGroupsView = new ListCollectionView(MasterAddressList);
            MailboxesAndGroupsView.Filter = obj =>(((MailboxWrapper)obj).DistinguishedName != Mailbox.DistinguishedName);
        }

        private void getFoldersWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Status = "Retrieving folder list";
            Collection<PSObject> psFolders = Exchange.GetMailboxFolders(Mailbox.DistinguishedName);
            Status = "Organising folder tree";
            MailboxFolderViewModel topLevelFolder = new MailboxFolderViewModel(this, new MailboxFolderWrapper(psFolders[0])) { IsExpanded = true };
            ObservableCollection<MailboxFolderViewModel> folders = new ObservableCollection<MailboxFolderViewModel>();
            folders.Add(topLevelFolder);
            //Add all the folders as a flat list of subfolders to the top of information store.
            for (int i = 1; i < psFolders.Count; i++)
            {
                topLevelFolder.SubfolderViewModels.Add(new MailboxFolderViewModel(this, new MailboxFolderWrapper(psFolders[i])));
            }
            //If the folder path contains a / then it must be a subfolder.
            IEnumerable<MailboxFolderViewModel> subFolders =
                (from folder in topLevelFolder.SubfolderViewModels
                 where folder.FolderWrapper.FolderPath.TrimStart('/').Contains('/')
                 select folder).ToList();

            foreach (MailboxFolderViewModel subFolder in subFolders)
            {
                //The subfolder's parent folder path is everything prior to the last occurrence of / in
                //the subfolder's folder path.
                int dividorIndex = subFolder.FolderWrapper.FolderPath.LastIndexOf('/');
                string parentPath = subFolder.FolderWrapper.FolderPath.Substring(0, dividorIndex);
                
                MailboxFolderViewModel parentFolder = topLevelFolder.SubfolderViewModels.Single(x => x.FolderWrapper.FolderPath == parentPath); 
                parentFolder.SubfolderViewModels.Add(subFolder);
            }
            //Once all subfolders have been added to the correct parent folder they need to be removed
            //from the top level.
            foreach (MailboxFolderViewModel folder in subFolders)
            {
                topLevelFolder.SubfolderViewModels.Remove(folder);
            }
            e.Result = folders;
        }

        private void getFoldersWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Folders = e.Result as ObservableCollection<MailboxFolderViewModel>;
            OnPropertyChanged("Folders");
            Status = "";
        }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                OnPropertyChanged("IsSelected");
            }
        }

        public DelegatesViewModel Delegates
        { get; private set; }

        public ObservableCollection<MailboxFolderViewModel> Folders
        { get; private set; }

        public MailboxWrapper Mailbox
        { get; private set; }

        public ListCollectionView MailboxesAndGroupsView
        {
            get { return this.mailboxesAndGroupsView; }
            private set
            {
                if (value == this.mailboxesAndGroupsView)
                {
                    return;
                }
                mailboxesAndGroupsView = value;
                OnPropertyChanged("MailboxesAndGroupsView");
            }
        }

        //A MailboxFolderViewModel sets this property to true when its worker
        //starts and false when it finishes. This avoids having to search all the
        //MailboxFolderViewModels to see if any are busy.
        public bool SubfolderBusy
        { get; set; }

        //Lets the MainWindowViewModel know whether it is ok to close this
        //Mailbox.
        public bool CanClose
        {
            get
            { return !getFoldersWorker.IsBusy && !Delegates.Worker.IsBusy && !SubfolderBusy; }
        }
    }
}
