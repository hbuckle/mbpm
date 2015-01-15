using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Input;
using System.ComponentModel;
using MailboxPermissionsManager.Utilities;
using MailboxPermissionsManager.PowerShellCommands;

namespace MailboxPermissionsManager.ViewModels
{
    class AdvancedOptionsViewModel : ViewModelBase
    {
        public ICommand RemoveFromAllFoldersCommand
        { get; private set; }

        public MailboxViewModel ParentMailboxViewModel
        { get; private set; }

        public string UserToRemove
        { get; set; }

        public AdvancedOptionsViewModel(MailboxViewModel parentMailboxViewModel)
        {
            RemoveFromAllFoldersCommand = new Command
            {
                CanExecuteDelegate = () => (UserToRemove != null)
            };
        }

        protected override void configureWorker()
        {
            worker.DoWork += (sender, e) =>
                {
                    foreach (MailboxFolderViewModel folder in ParentMailboxViewModel.Folders)
                    {
                        Exchange.RemoveFolderPermissions(ParentMailboxViewModel.Mailbox.PrimarySmtp,
                            folder.FolderWrapper.CorrectedFolderPath, UserToRemove);
                    }
                };
        }
    }
}
