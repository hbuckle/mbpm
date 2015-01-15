using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;

namespace MailboxPermissionsManager.Model
{
    class FolderPermissionWrapper : PSObjectWrapper
    {
        public FolderPermissionWrapper(PSObject psObject)
            : base(psObject)
        { }

        public string User
        { get { return psObject.Properties["User"].Value.ToString(); } }

        public string AccessRights
        { get { return psObject.Properties["AccessRights"].Value.ToString(); } }
    }
}
