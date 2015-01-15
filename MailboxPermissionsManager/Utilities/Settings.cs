using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace MailboxPermissionsManager.Utilities
{
    static class Settings
    {
        private static XmlDocument settingsXml = new XmlDocument();

        public static void Load()
        {
            settingsXml.Load("Settings.xml");
        }

        private static List<string> getList(string path)
        {
            List<string> list = new List<string>();
            XmlNode things = settingsXml.SelectSingleNode(path);
            foreach (XmlNode thing in things)
            {
                list.Add(thing.InnerText);
            }
            return list;
        }

        private static string getItem(string path)
        {
            XmlNode uri = settingsXml.SelectSingleNode(path);
            return uri.InnerText;
        }

        public static string ShellUri
        { get { return getItem("/Settings/ShellUri"); } }

        public static string ServerUri
        { get { return getItem("/Settings/ServerUri"); } }

        public static string AutodiscoverUrl
        { get { return getItem("/Settings/AutodiscoverUrl"); } }

        public static string ExchangeServiceVersion
        { get { return getItem("/Settings/ExchangeServiceVersion"); } }

        public static bool UseDefaultCredentials
        { get { return bool.Parse(getItem("/Settings/UseDefaultCredentials")); } }
    }
}
