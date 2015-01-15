using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Exchange.WebServices.Data;
using MailboxPermissionsManager.Utilities;
using System.Windows.Input;

namespace MailboxPermissionsManager.ViewModels
{
    class DelegateUserViewModel : Notifier
    {
        public DelegateUserViewModel(DelegateUser ewsDelegateUser, DelegatesViewModel parentDelegatesViewModel)
        {
            EwsDelegateUser = ewsDelegateUser;
            ParentDelegatesViewModel = parentDelegatesViewModel;
            RemoveDelegateCommand = new Command
            {
                CanExecuteDelegate = () => (!parentDelegatesViewModel.Worker.IsBusy),
                ExecuteDelegate = x => ParentDelegatesViewModel.RemoveDelegate(EwsDelegateUser)
            };
        }

        public DelegateUser EwsDelegateUser
        { get; private set; }

        public DelegatesViewModel ParentDelegatesViewModel
        { get; private set; }

        public bool ReceiveSchedulers
        {
            get { return EwsDelegateUser.ReceiveCopiesOfMeetingMessages; }
            set
            {
                if (!ParentDelegatesViewModel.Worker.IsBusy)
                {
                    EwsDelegateUser.ReceiveCopiesOfMeetingMessages = value;

                    //Delegate must have at least editor on the calendar to receive schedulers.
                    if (value)
                        EwsDelegateUser.Permissions.CalendarFolderPermissionLevel = DelegateFolderPermissionLevel.Editor;

                    ParentDelegatesViewModel.UpdateExistingDelegate(EwsDelegateUser);
                }
            }
        }

        public bool ViewPrivate
        {
            get { return EwsDelegateUser.ViewPrivateItems; }
            set
            {
                if (!ParentDelegatesViewModel.Worker.IsBusy)
                {
                    EwsDelegateUser.ViewPrivateItems = value;

                    ParentDelegatesViewModel.UpdateExistingDelegate(EwsDelegateUser);
                }
            }
        }

        public ICommand RemoveDelegateCommand
        { get; private set; }
    }
}
