using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace MailboxPermissionsManager.Utilities
{
    class PermissionSelector
    {
        public PermissionSelector()
        { IsSelected = false; }

        public string AccessRight
        { get; set; }

        public bool IsSelected
        { get; set; }

        public override string ToString()
        { return AccessRight; }
    }
}
