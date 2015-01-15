using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;


namespace MailboxPermissionsManager.Model
{
    class MailboxFolderWrapper : PSObjectWrapper
    {
        public MailboxFolderWrapper(PSObject psObject)
            : base(psObject)
        { }

        public string Name
        { get { return GetPSPropertyString("Name"); } }

        public string FolderPath
        { get { return GetPSPropertyString("FolderPath"); } }

        public string FolderType
        { get { return GetPSPropertyString("FolderType"); } }

        public string CorrectedFolderPath
        {
            get
            {
                string correctedPath = FolderPath.Replace("/", "\\");
                char c = (char)63743;
                correctedPath = correctedPath.Replace(c, '/');
                return correctedPath;
            }
        }
    }
}
