using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;

namespace MailboxPermissionsManager.Model
{
    class MailboxWrapper : PSObjectWrapper
    {
        public MailboxWrapper(PSObject psObject)
            : base(psObject)
        { }

        public string PrimarySmtp
        {
            get
            {
                try
                {
                    return GetPSPropertyString("PrimarySmtpAddress");
                }
                catch (NullReferenceException)
                {
                    return GetPSPropertyString("WindowsEmailAddress");
                }
            }
        }

        public string DistinguishedName
        { get { return GetPSPropertyString("DistinguishedName"); } }

        public string Name
        { get { return GetPSPropertyString("Name"); } }

        public string RecipientType
        { get { return GetPSPropertyString("RecipientType"); } }

        public bool HiddenFromAddressListsEnabled
        { get { return (bool)psObject.Properties["HiddenFromAddressListsEnabled"].Value; } }

        public override string ToString()
        { return Name; }
    }
}