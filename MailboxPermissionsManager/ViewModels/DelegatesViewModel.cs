using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MailboxPermissionsManager.EWS;
using Microsoft.Exchange.WebServices.Data;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.ComponentModel;
using MailboxPermissionsManager.Utilities;
using MailboxPermissionsManager.Model;
using System.Net;
using System.Windows.Data;

namespace MailboxPermissionsManager.ViewModels
{
    class DelegatesViewModel : ViewModelBase
    {
        private DelegateInformation delegateInfo;
        private MailboxWrapper mailboxToAddAsDelegate;
        private string schedulerOption;
        private string originalSchedulerOption;

        public DelegatesViewModel(MailboxViewModel parentMailboxViewModel)
        {
            ParentMailboxViewModel = parentMailboxViewModel;
            DelegateUsers = new ObservableCollection<DelegateUserViewModel>();

            AddDelegateCommand = new Command
            {
                CanExecuteDelegate = () => (MailboxToAddAsDelegate != null && !worker.IsBusy),
                ExecuteDelegate = x => AddDelegate()
            };
            ChangeSchedulerDeliveryCommand = new Command
            {
                CanExecuteDelegate = () => (SchedulerOption != OriginalSchedulerOption && DelegateUsers.Count != 0 && !worker.IsBusy),
                ExecuteDelegate = x => ChangeSchedulerDelivery()
            };

            ConnectToEWS();

            NewDelegatesView = new ListCollectionView(MasterAddressList);
            NewDelegatesView.Filter = new Predicate<object>(filterDelegatesList);
        }

        private bool filterDelegatesList(object obj)
        {
            MailboxWrapper mailbox = obj as MailboxWrapper;
            //You can't add groups as delegates.
            if (mailbox.RecipientType.Contains("Group"))
                return false;
            //You can't add the user as a delegate to their own mailbox.
            if (mailbox.DistinguishedName == ParentMailboxViewModel.Mailbox.DistinguishedName)
                return false;
            //Neither can you add users who are already delegates.
            DelegateUserViewModel dup = DelegateUsers.SingleOrDefault(x =>
                x.EwsDelegateUser.UserId.PrimarySmtpAddress == mailbox.PrimarySmtp);
            if (dup != null)
                return false;
            return true;
        }

        protected override void configureWorker()
        {
            worker.DoWork += (sender, e) =>
            {
                //BackgroundWorker bw = sender as BackgroundWorker;
                e.Result = ((Func<DoWorkEventArgs, DelegateInformation>)e.Argument)(e);
            };

            worker.RunWorkerCompleted += (sender, e) =>
            {
                delegateInfo = (DelegateInformation)e.Result;
                SchedulerOption = delegateInfo.MeetingRequestsDeliveryScope.ToString();
                OriginalSchedulerOption = SchedulerOption;
                MailboxToAddAsDelegate = null;
                NewDelegateReceivesSchedulers = false;
                OnPropertyChanged("NewDelegateReceivesSchedulers");
                NewDelegateViewPrivateItems = false;
                OnPropertyChanged("NewDelegateViewPrivateItems");
                DelegateUsers.Clear();
                foreach (DelegateUserResponse response in delegateInfo.DelegateUserResponses)
                {
                    if (response.ErrorMessage == null)
                        DelegateUsers.Add(new DelegateUserViewModel(response.DelegateUser, this));
                }
                Status = "";
                NewDelegatesView.Refresh();
            };
        }

        public void ConnectToEWS()
        {
            worker.RunWorkerAsync(new Func<DoWorkEventArgs, DelegateInformation>((e) =>
            {
                Status = "Connecting to EWS";
                if (Settings.UseDefaultCredentials)
                    EwsCommands = new EWSCommands(ParentMailboxViewModel.Mailbox.PrimarySmtp);
                else
                    EwsCommands = new EWSCommands(ParentMailboxViewModel.Mailbox.PrimarySmtp,
                        new NetworkCredential(credentials.UserName, credentials.Password));
                Status = "Retrieving delegates";
                return EwsCommands.GetDelegates();
            }));
        }

        public void AddDelegate()
        {
            worker.RunWorkerAsync(new Func<DoWorkEventArgs, DelegateInformation>((e) =>
            {
                try
                {
                    Status = "Adding " + MailboxToAddAsDelegate.Name + " as delegate";
                    EwsCommands.AddDelegate(MailboxToAddAsDelegate.PrimarySmtp, NewDelegateReceivesSchedulers, NewDelegateViewPrivateItems, SchedulerOption);
                    System.Threading.Thread.Sleep(10000);
                    Status = "Retrieving delegates";
                    return EwsCommands.GetDelegates();
                }
                catch (EWSException ewsError)
                {
                    //Error occurs sometimes after you add a delegate and then retrieve delegates,
                    //waiting and trying again should work
                    if (ewsError.Message == "ErrorDelegateMissingConfiguration")
                    {
                        Status = "Problem retrieving delegates, retrying";
                        System.Threading.Thread.Sleep(10000);
                        return EwsCommands.GetDelegates();
                    }
                    else
                        throw ewsError;
                }
            }));
        }

        public void ChangeSchedulerDelivery()
        {
            worker.RunWorkerAsync(new Func<DoWorkEventArgs, DelegateInformation>((e) =>
            {
                Status = "Updating scheduler delivery option";
                EwsCommands.SetDeliverMeetingRequests(SchedulerOption, DelegateUsers[0].EwsDelegateUser);
                System.Threading.Thread.Sleep(10000);
                Status = "Retrieving delegates";
                return EwsCommands.GetDelegates();
            }));
        }

        public void UpdateExistingDelegate(DelegateUser delegateToUpdate)
        {
            worker.RunWorkerAsync(new Func<DoWorkEventArgs, DelegateInformation>((e) =>
            {
                Status = "Updating delegate " + delegateToUpdate.UserId.DisplayName;
                EwsCommands.UpdateDelegate(delegateToUpdate, SchedulerOption);
                System.Threading.Thread.Sleep(10000);
                Status = "Retrieving delegates";
                return EwsCommands.GetDelegates();
            }));
        }

        public void RemoveDelegate(DelegateUser delegateToRemove)
        {
            worker.RunWorkerAsync(new Func<DoWorkEventArgs, DelegateInformation>((e) =>
            {
                Status = "Removing delegate " + delegateToRemove.UserId.DisplayName;
                EwsCommands.RemoveDelegate(delegateToRemove);
                System.Threading.Thread.Sleep(10000);
                Status = "Retrieving delegates";
                return EwsCommands.GetDelegates();
            }));
        }

        public EWSCommands EwsCommands
        { get; private set; }

        public List<string> SchedulerOptions
        {
            get
            {
                return new List<string>(Enum.GetNames(typeof(MeetingRequestsDeliveryScope)));
            }
        }

        public ObservableCollection<DelegateUserViewModel> DelegateUsers
        { get; private set; }

        public ICommand ChangeSchedulerDeliveryCommand
        { get; private set; }

        public ICommand AddDelegateCommand
        { get; private set; }

        public string SchedulerOption
        {
            get { return schedulerOption; }
            set
            {
                schedulerOption = value;
                OnPropertyChanged("SchedulerOption");
            }
        }

        public ListCollectionView NewDelegatesView
        { get; private set; }

        public string OriginalSchedulerOption
        {
            get { return originalSchedulerOption; }
            private set
            {
                originalSchedulerOption = value;
                OnPropertyChanged("OriginalSchedulerOption");
            }
        }

        public MailboxWrapper MailboxToAddAsDelegate
        {
            get { return mailboxToAddAsDelegate; }
            set
            {
                mailboxToAddAsDelegate = value;
                OnPropertyChanged("MailboxToAddAsDelegate");
            }
        }

        public bool NewDelegateReceivesSchedulers
        { get; set; }

        public bool NewDelegateViewPrivateItems
        { get; set; }

        public MailboxViewModel ParentMailboxViewModel
        { get; private set; }
    }
}
