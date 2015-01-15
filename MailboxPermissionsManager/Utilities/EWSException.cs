using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Exchange.WebServices.Data;

namespace MailboxPermissionsManager.Utilities
{
    class EWSException : SystemException
    {
        public EWSException()
        {
        }
        public EWSException(string message) : base(message)
        {
            // Add implementation.
        }
        public EWSException(string message, Exception inner) : base(message, inner)
        {
            // Add implementation.
        }

        public EWSException(string message, DelegateUserResponse response) : base(message)
        {
            DelegateResponse = response;
        }

        public DelegateUserResponse DelegateResponse
        { get; private set; }
    }
}
