using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MailboxPermissionsManager.Model;
using MailboxPermissionsManager.Utilities;

namespace MailboxPermissionsManager.ViewModels
{
    class FolderPermissionViewModel
    {
        public FolderPermissionViewModel(MailboxFolderViewModel folder, FolderPermissionWrapper permission)
        {
            Permission = permission;
            User = Permission.User;
            FolderViewModel = folder;
            addPermissionLevels();
            OriginalAccessRights = Permission.AccessRights;
            AccessRights = OriginalAccessRights;
        }

        public string User
        { get; private set; }

        public MailboxFolderViewModel FolderViewModel
        { get; private set; }

        public FolderPermissionWrapper Permission
        { get; private set; }

        public string OriginalAccessRights
        { get; private set; }

        public string AccessRights
        { get; set; }

        public List<string> PermissionLevels
        { get; private set; }

        public bool HasChanged
        {
            get { return !(OriginalAccessRights == AccessRights); }
        }

        private void addPermissionLevels()
        {
            //You can't remove default permissions anyway and you shouldn't remove anonymous
            //permissions as stuff can break.
            PermissionLevels = new List<string>(FolderViewModel.PermissionLevels);
            if (User == "Default" || User == "Anonymous")
            {
                PermissionLevels.Remove("Remove");
            }
        }
    }
}
