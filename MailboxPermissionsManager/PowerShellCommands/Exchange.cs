using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using System.Collections.ObjectModel;
using MailboxPermissionsManager.Utilities;
using MailboxPermissionsManager.Views;
using System.ComponentModel;

namespace MailboxPermissionsManager.PowerShellCommands
{
    public static class Exchange
    {
        private static WSManConnectionInfo connection;
        private static RunspacePool pool;

        public static void OpenRunspace(PSCredential credentials)
        {
            connection = getConnection(credentials);
            pool = RunspaceFactory.CreateRunspacePool(1, 5, connection);
            pool.Open();
        }

        private static WSManConnectionInfo getConnection(PSCredential credentials)
        {
            string shellUri = Settings.ShellUri;
            Uri serverUri = new Uri(Settings.ServerUri);
            
            WSManConnectionInfo connection = new WSManConnectionInfo(serverUri, shellUri, credentials);

            if (Settings.UseDefaultCredentials)
                connection.AuthenticationMechanism = AuthenticationMechanism.Kerberos;
            else
                connection.AuthenticationMechanism = AuthenticationMechanism.Basic;

            return connection;
        }

        private static Collection<PSObject> runPowershellCommands(PSCommand commandsToRun)
        {
            if (pool.RunspacePoolStateInfo.State != RunspacePoolState.Opened)
            {
                pool.Close();
                pool = RunspaceFactory.CreateRunspacePool(1, 5, connection);
                pool.Open();
            }
            PowerShell powershell = PowerShell.Create();
            powershell.Commands = commandsToRun;
            powershell.RunspacePool = pool;
            Collection<PSObject> results = powershell.Invoke();
            powershell.Dispose();
            return results;
        }

        public static Collection<PSObject> GetMailboxes()
        {
            PSCommand commands = new PSCommand();
            commands.AddCommand("Get-Mailbox");
            commands.AddParameter("RecipientTypeDetails", "UserMailbox");
            commands.AddParameter("SortBy", "Name");
            return runPowershellCommands(commands);
        }

        public static Collection<PSObject> GetGroups()
        {
            PSCommand commands = new PSCommand();
            commands.AddCommand("Get-Group");
            commands.AddParameter("RecipientTypeDetails", "MailUniversalSecurityGroup");
            commands.AddParameter("SortBy", "Name");
            return runPowershellCommands(commands);
        }

        public static Collection<PSObject> GetMailboxFolders(string mailbox)
        {
            PSCommand commands = new PSCommand();
            commands.AddCommand("Get-MailboxFolderStatistics");
            commands.AddParameter("Identity", mailbox);
            return runPowershellCommands(commands);
        }

        public static Collection<PSObject> GetFolderPermissions(string mailbox, string folder)
        {
            PSCommand commands = new PSCommand();
            if (folder == "\\Top of Information Store")
            {
                folder = "\\";
            }
            string folderPath = mailbox + ":" + folder;
            commands.AddCommand("Get-MailboxFolderPermission");
            commands.AddParameter("Identity", folderPath);
            return runPowershellCommands(commands);
        }

        public static void RemoveAllFolderPermissions(string mailbox, string folder)
        {
            PSCommand commands = new PSCommand();
            if (folder == "\\Top of Information Store")
            {
                folder = "\\";
            }
            string folderPath = mailbox + ":" + folder;
            SwitchParameter noConfirm = new SwitchParameter();
            Collection<PSObject> currentPermissions = GetFolderPermissions(mailbox, folder);
            foreach (PSObject permission in currentPermissions)
            {
                if (permission.Properties["User"].Value.ToString() != "Default" && permission.Properties["User"].Value.ToString() != "Anonymous")
                {
                    commands.Clear();
                    commands.AddCommand("Remove-MailboxFolderPermission");
                    commands.AddParameter("Identity", folderPath);
                    commands.AddParameter("User", permission.Properties["User"].Value.ToString());
                    commands.AddParameter("Confirm", noConfirm);
                    runPowershellCommands(commands);
                }
            }
        }

        public static void SetFolderPermissions(string mailbox, string folder, string user, string accessRights)
        {
            PSCommand commands = new PSCommand();
            if (folder == "\\Top of Information Store")
            {
                folder = "\\";
            }
            string folderPath = mailbox + ":" + folder;
            commands.AddCommand("Set-MailboxFolderPermission");
            commands.AddParameter("Identity", folderPath);
            commands.AddParameter("User", user);
            commands.AddParameter("AccessRights", accessRights);
            Collection<PSObject> result1 = runPowershellCommands(commands);
            if (result1.Count == 0)
            {
                commands.Clear();
                commands.AddCommand("Add-MailboxFolderPermission");
                commands.AddParameter("Identity", folderPath);
                commands.AddParameter("User", user);
                commands.AddParameter("AccessRights", accessRights);
                Collection<PSObject> result2 = runPowershellCommands(commands);
            }
        }

        public static void RemoveFolderPermissions(string mailbox, string folder, string user)
        {
            PSCommand commands = new PSCommand();
            if (folder == "\\Top of Information Store")
            {
                folder = "\\";
            }
            string folderPath = mailbox + ":" + folder;
            SwitchParameter noConfirm = new SwitchParameter();
            commands.AddCommand("Remove-MailboxFolderPermission");
            commands.AddParameter("Identity", folderPath);
            commands.AddParameter("User", user);
            commands.AddParameter("Confirm", noConfirm);
            Collection<PSObject> result = runPowershellCommands(commands);
        }

        public static void AddMailboxPermission(string mailbox, string user, string accessRights)
        {
            PSCommand commands = new PSCommand();
            commands.AddCommand("Add-MailboxPermission");
            commands.AddParameter("Identity", mailbox);
            commands.AddParameter("User", user);
            commands.AddParameter("AccessRights", accessRights);
            Collection<PSObject> result = runPowershellCommands(commands);
        }

        public static void CloseRunspace()
        {
            if (pool != null)
            {
                if (pool.RunspacePoolStateInfo.State == RunspacePoolState.Opened)
                    pool.Close();
            }
        }
    }
}
