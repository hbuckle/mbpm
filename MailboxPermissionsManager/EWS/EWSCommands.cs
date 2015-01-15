using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Exchange.WebServices.Data;
using System.Net;
using System.Net.Security;
using MailboxPermissionsManager.Utilities;
using System.Collections.ObjectModel;

namespace MailboxPermissionsManager.EWS
{
    public class EWSCommands
    {
        private ExchangeService service;
        private Mailbox mailbox;

        public EWSCommands(string impersonatedUserSmtp)
        {
            this.service = getService();
            //service.TraceListener = new TraceListener();
            //service.TraceFlags = TraceFlags.EwsRequest | TraceFlags.EwsResponse;
            //service.TraceEnabled = true;
            service.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, impersonatedUserSmtp);
            mailbox = new Mailbox(impersonatedUserSmtp);
        }

        public EWSCommands(string impersonatedUserSmtp, NetworkCredential credentials)
        {
            this.service = getService(credentials);
            //service.TraceListener = new TraceListener();
            //service.TraceFlags = TraceFlags.EwsRequest | TraceFlags.EwsResponse;
            //service.TraceEnabled = true;
            service.ImpersonatedUserId = new ImpersonatedUserId(ConnectingIdType.SmtpAddress, impersonatedUserSmtp);
            mailbox = new Mailbox(impersonatedUserSmtp);
        }

        #region Connection
        private bool CertificateValidationCallBack(object sender, System.Security.Cryptography.X509Certificates.X509Certificate certificate,
            System.Security.Cryptography.X509Certificates.X509Chain chain, System.Net.Security.SslPolicyErrors sslPolicyErrors)
        {
            // If the certificate is a valid, signed certificate, return true.
            if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
            {
                return true;
            }

            // If there are errors in the certificate chain, look at each error to determine the cause.
            if ((sslPolicyErrors & System.Net.Security.SslPolicyErrors.RemoteCertificateChainErrors) != 0)
            {
                if (chain != null && chain.ChainStatus != null)
                {
                    foreach (System.Security.Cryptography.X509Certificates.X509ChainStatus status in chain.ChainStatus)
                    {
                        if ((certificate.Subject == certificate.Issuer) &&
                           (status.Status == System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.UntrustedRoot))
                        {
                            // Self-signed certificates with an untrusted root are valid. 
                            continue;
                        }
                        else
                        {
                            if (status.Status != System.Security.Cryptography.X509Certificates.X509ChainStatusFlags.NoError)
                            {
                                // If there are any other errors in the certificate chain, the certificate is invalid,
                                // so the method returns false.
                                return false;
                            }
                        }
                    }
                }

                // When processing reaches this line, the only errors in the certificate chain are 
                // untrusted root errors for self-signed certificates. These certificates are valid
                // for default Exchange server installations, so return true.
                return true;
            }
            else
            {
                // In all other cases, return false.
                return false;
            }
        }

        private bool RedirectionUrlValidationCallback(string redirectionUrl)
        {
            // The default for the validation callback is to reject the URL.
            bool result = false;

            Uri redirectionUri = new Uri(redirectionUrl);

            // Validate the contents of the redirection URL. In this simple validation
            // callback, the redirection URL is considered valid if it is using HTTPS
            // to encrypt the authentication credentials. 
            if (redirectionUri.Scheme == "https")
            {
                result = true;
            }
            return result;
        }

        private ExchangeService getService()
        {
            ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallBack;
            ExchangeVersion version = (ExchangeVersion)Enum.Parse(typeof(ExchangeVersion), Settings.ExchangeServiceVersion);
            ExchangeService service = new ExchangeService(version);
            service.UseDefaultCredentials = true;
            service.AutodiscoverUrl(Settings.AutodiscoverUrl, RedirectionUrlValidationCallback);

            return service;
        }

        private ExchangeService getService(NetworkCredential credentials)
        {
            ServicePointManager.ServerCertificateValidationCallback = CertificateValidationCallBack;
            ExchangeVersion version = (ExchangeVersion)Enum.Parse(typeof(ExchangeVersion), Settings.ExchangeServiceVersion);
            ExchangeService service = new ExchangeService(version);
            service.Credentials = credentials;
            service.AutodiscoverUrl(Settings.AutodiscoverUrl, RedirectionUrlValidationCallback);

            return service;
        }


        #endregion

        public DelegateInformation GetDelegates()
        {
            DelegateInformation result = service.GetDelegates(mailbox, true);
            foreach (DelegateUserResponse response in result.DelegateUserResponses)
            {
                if (response.Result != ServiceResult.Success)
                {
                    //ErrorDelegateNoUser means the delegate is a group, which the EWS API can't handle,
                    //so we ignore it.
                    if (response.ErrorCode.ToString() != "ErrorDelegateNoUser")
                        throw new EWSException(response.ErrorCode.ToString(), response);
                }
            }
            return result;
        }

        public void AddDelegate(string delegateSmtp, bool receiveSchedulers, bool viewPrivateItems, string currentSchedulerOption)
        {
            MeetingRequestsDeliveryScope scope = (MeetingRequestsDeliveryScope)Enum.Parse(typeof(MeetingRequestsDeliveryScope), currentSchedulerOption);
            DelegateUser delegateUser = new DelegateUser(delegateSmtp);
            delegateUser.Permissions.CalendarFolderPermissionLevel = DelegateFolderPermissionLevel.Editor;
            delegateUser.ReceiveCopiesOfMeetingMessages = receiveSchedulers;
            delegateUser.ViewPrivateItems = viewPrivateItems;
            Collection<DelegateUserResponse> result = service.AddDelegates(mailbox, scope, delegateUser);
            if (result[0].Result == ServiceResult.Success)
                return;
            else
                throw new EWSException(result[0].ErrorCode.ToString(), result[0]);
        }

        public void UpdateDelegate(DelegateUser delegateToUpdate, string currentSchedulerOption)
        {
            MeetingRequestsDeliveryScope scope = (MeetingRequestsDeliveryScope)Enum.Parse(typeof(MeetingRequestsDeliveryScope),currentSchedulerOption);
            Collection<DelegateUserResponse> result = service.UpdateDelegates(mailbox, scope,
                delegateToUpdate);
            if (result[0].Result == ServiceResult.Success)
                return;
            else
                throw new EWSException(result[0].ErrorCode.ToString(), result[0]);
        }

        public void SetDeliverMeetingRequests(string deliveryScope, DelegateUser randomUser)
        {
            MeetingRequestsDeliveryScope scope = (MeetingRequestsDeliveryScope)Enum.Parse(typeof(MeetingRequestsDeliveryScope),
                deliveryScope);
            Collection<DelegateUserResponse> result = service.UpdateDelegates(mailbox, scope, randomUser);
            if (result[0].Result == ServiceResult.Success)
                return;
            else
                throw new EWSException(result[0].ErrorCode.ToString(), result[0]);
        }

        public void RemoveDelegate(DelegateUser delegateToRemove)
        {
            Collection<DelegateUserResponse> result = service.RemoveDelegates(mailbox, delegateToRemove.UserId);
            if (result[0].Result == ServiceResult.Success)
                return;
            else
                throw new EWSException(result[0].ErrorCode.ToString(), result[0]);

        }
    }
}
