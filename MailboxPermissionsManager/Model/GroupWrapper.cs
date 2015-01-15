using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;

namespace MailboxPermissionsManager.Model
{
    class GroupWrapper : PSObjectWrapper
    {
        public GroupWrapper(PSObject psObject)
            : base(psObject)
        { }

        public string PrimarySmtp
        { get { return GetPSPropertyString("PrimarySmtpAddress"); } }

        public string DistinguishedName
        { get { return GetPSPropertyString("DistinguishedName"); } }

        public string Name
        { get { return GetPSPropertyString("Name"); } }

        public override string ToString()
        { return Name; }
    }
}
