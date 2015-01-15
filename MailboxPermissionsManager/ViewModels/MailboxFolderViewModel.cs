using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MailboxPermissionsManager.Model;
using MailboxPermissionsManager.PowerShellCommands;
using MailboxPermissionsManager.Utilities;
using System.Collections.ObjectModel;
using System.Management.Automation;
using System.Windows.Input;
using System.ComponentModel;

namespace MailboxPermissionsManager.ViewModels
{
    class MailboxFolderViewModel : ViewModelBase
    {
        private bool isSelected;
        private MailboxWrapper mailboxToGrantPermissionsTo;
        private string permissionToApply;

        public MailboxFolderViewModel(MailboxViewModel parentMailboxViewModel, MailboxFolderWrapper folderWrapper)
        {
            ParentMailboxViewModel = parentMailboxViewModel;
            FolderWrapper = folderWrapper;
            SubfolderViewModels = new ObservableCollection<MailboxFolderViewModel>();
            PermissionsViewModels = new ObservableCollection<FolderPermissionViewModel>();
            addPermissionLevels();
            defineCommands();
        }

        private void defineCommands()
        {
            SingleUserFolderOnlyCommand = new Command
            {
                CanExecuteDelegate = () => (MailboxToGrantPermissionsTo != null &&
                PermissionToApply != null && !worker.IsBusy),
                ExecuteDelegate = x => worker.RunWorkerAsync(new Action( () => 
                    applySinglePermission(this, PermissionToApply, MailboxToGrantPermissionsTo.PrimarySmtp, false)))
            };
            SingleUserSubfoldersCommand = new Command
            {
                CanExecuteDelegate = () =>
                    {
                        if (MailboxToGrantPermissionsTo == null || PermissionToApply == null || worker.IsBusy)
                            return false;
                        if (FolderWrapper.FolderPath == "/Top of Information Store" && PermissionToApply != "Remove")
                            return false;
                        return true;
                    },
                ExecuteDelegate = x => worker.RunWorkerAsync(new Action(() =>
                    applySinglePermission(this, PermissionToApply, MailboxToGrantPermissionsTo.PrimarySmtp, true)))
            };
            MultiUserFolderOnlyCommand = new Command
            {
                CanExecuteDelegate = () =>
                {
                    if (worker.IsBusy)
                        return false;
                    foreach (FolderPermissionViewModel permission in PermissionsViewModels)
                    {
                        if (permission.HasChanged)
                            return true;
                    }
                    return false;
                },
                ExecuteDelegate = x => worker.RunWorkerAsync(new Action(() =>
                    {
                        IEnumerable<FolderPermissionViewModel> changed = PermissionsViewModels.Where(y => y.HasChanged);
                        applyMultiPermission(this, changed, false);
                    }
                    ))
            };
            MultiUserSubfoldersCommand = new Command
            {
                CanExecuteDelegate = () => (!worker.IsBusy && FolderWrapper.FolderPath != "/Top of Information Store"),
                ExecuteDelegate = x => worker.RunWorkerAsync(new Action(() =>
                    {
                        IEnumerable<FolderPermissionViewModel> currentPermissions = PermissionsViewModels.ToList();
                        applyMultiPermission(this, currentPermissions, true);
                    }
                    ))
            };
        }
        
        protected override void configureWorker()
        {
            worker.DoWork += (sender, e) =>
            {
                ParentMailboxViewModel.SubfolderBusy = true;
                ((Action)e.Argument)();
            };
            worker.RunWorkerCompleted += (sender, e) =>
            {
                MailboxToGrantPermissionsTo = null;
                PermissionToApply = null;
                if (IsSelected)
                    getPermissions();
                ParentMailboxViewModel.Status = "";
                ParentMailboxViewModel.SubfolderBusy = false;
            };
        }

        private void getPermissions()
        {
            ParentMailboxViewModel.Status = "Retrieving permissions for " + this.FolderWrapper.Name;
            Collection<PSObject> psPermissions = Exchange.GetFolderPermissions(
                ParentMailboxViewModel.Mailbox.PrimarySmtp, FolderWrapper.CorrectedFolderPath);
            PermissionsViewModels.Clear();
            foreach (PSObject psPermission in psPermissions)
            {
                PermissionsViewModels.Add(new FolderPermissionViewModel(this, new FolderPermissionWrapper(psPermission)));
            }
            ParentMailboxViewModel.Status = "";
        }

        private void applySinglePermission(MailboxFolderViewModel folderViewModel, string permission, string user, bool subfolders)
        {
            ParentMailboxViewModel.Status = "Applying permissions to " + folderViewModel.FolderWrapper.Name;

            if (permission == "Remove")
                Exchange.RemoveFolderPermissions(ParentMailboxViewModel.Mailbox.PrimarySmtp, folderViewModel.FolderWrapper.CorrectedFolderPath,
                    user);
            else
                Exchange.SetFolderPermissions(ParentMailboxViewModel.Mailbox.PrimarySmtp, folderViewModel.FolderWrapper.CorrectedFolderPath,
                    user, permission);
            if (subfolders)
            {
                foreach (MailboxFolderViewModel subfolder in folderViewModel.SubfolderViewModels)
                    applySinglePermission(subfolder, permission, user, true);
            }
        }

        private void applyMultiPermission(MailboxFolderViewModel folderViewModel, IEnumerable<FolderPermissionViewModel> permissions, bool subfolders)
        {
            ParentMailboxViewModel.Status = "Applying permissions to " + folderViewModel.FolderWrapper.Name;

            if (!subfolders)
            {
                foreach (FolderPermissionViewModel permission in permissions)
                {
                    if (permission.AccessRights == "Remove")
                        Exchange.RemoveFolderPermissions(ParentMailboxViewModel.Mailbox.PrimarySmtp, folderViewModel.FolderWrapper.CorrectedFolderPath,
                            permission.User);
                    else
                        Exchange.SetFolderPermissions(ParentMailboxViewModel.Mailbox.PrimarySmtp, folderViewModel.FolderWrapper.CorrectedFolderPath,
                            permission.User, permission.AccessRights);
                }
            }
            else
            {
                Exchange.RemoveAllFolderPermissions(ParentMailboxViewModel.Mailbox.PrimarySmtp, folderViewModel.FolderWrapper.CorrectedFolderPath);

                foreach (FolderPermissionViewModel permission in permissions)
                {
                    if (permission.AccessRights == "Remove")
                        Exchange.RemoveFolderPermissions(ParentMailboxViewModel.Mailbox.PrimarySmtp, folderViewModel.FolderWrapper.CorrectedFolderPath,
                            permission.User);
                    else
                        Exchange.SetFolderPermissions(ParentMailboxViewModel.Mailbox.PrimarySmtp, folderViewModel.FolderWrapper.CorrectedFolderPath,
                            permission.User, permission.AccessRights);
                }
                foreach (MailboxFolderViewModel subfolder in folderViewModel.SubfolderViewModels)
                    applyMultiPermission(subfolder, permissions, true);
            }
        }

        public ICommand SingleUserFolderOnlyCommand
        { get; private set; }

        public ICommand SingleUserSubfoldersCommand
        { get; private set; }

        public ICommand MultiUserFolderOnlyCommand
        { get; private set; }

        public ICommand MultiUserSubfoldersCommand
        { get; private set; }

        public MailboxWrapper MailboxToGrantPermissionsTo
        {
            get { return mailboxToGrantPermissionsTo; }
            set
            {
                mailboxToGrantPermissionsTo = value;
                OnPropertyChanged("MailboxToGrantPermissionsTo");
            }
        }

        public string PermissionToApply
        {
            get { return permissionToApply; }
            set
            {
                permissionToApply = value;
                OnPropertyChanged("PermissionToApply");
            }
        }
        public ObservableCollection<MailboxFolderViewModel> SubfolderViewModels
        { get; set; }

        public ObservableCollection<FolderPermissionViewModel> PermissionsViewModels
        { get; private set; }

        public MailboxViewModel ParentMailboxViewModel
        { get; private set; }

        public MailboxFolderWrapper FolderWrapper
        { get; private set; }

        public bool IsSelected
        {
            get { return isSelected; }
            set
            {
                isSelected = value;
                getPermissions();
            }
        }

        public bool IsExpanded
        { get; set; }

        public List<string> PermissionLevels
        { get; private set; }

        private void addPermissionLevels()
        {
            PermissionLevels = new List<string>();
            PermissionLevels.Add("Remove");
            PermissionLevels.Add("None");
            if (FolderWrapper.FolderPath.StartsWith("/Calendar"))
            {
                PermissionLevels.Add("AvailabilityOnly");
                PermissionLevels.Add("LimitedDetails");
            }
            PermissionLevels.Add("FolderVisible");
            PermissionLevels.Add("Reviewer");
            PermissionLevels.Add("Contributor");
            PermissionLevels.Add("NonEditingAuthor");
            PermissionLevels.Add("Author");
            PermissionLevels.Add("PublishingAuthor");
            PermissionLevels.Add("Editor");
            PermissionLevels.Add("PublishingEditor");
            PermissionLevels.Add("Owner");
        }
    }
}
