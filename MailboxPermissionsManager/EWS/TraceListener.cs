using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Microsoft.Exchange.WebServices.Data;

namespace MailboxPermissionsManager.EWS
{
    class TraceListener : ITraceListener
    {
        public void Trace(string traceType, string traceMessage)
        {
            CreateXMLTextFile(traceType, traceMessage.ToString());
        }

        private void CreateXMLTextFile(string fileName, string traceContent)
        {
            try
            {
                XmlDocument xmlDoc = new XmlDocument();
                xmlDoc.Load(traceContent);
                xmlDoc.Save(fileName + DateTime.Now.TimeOfDay.ToString(@"hh\-mm\-ss\-ffffff") + ".xml");

            }
            catch
            {
                System.IO.File.WriteAllText(fileName + DateTime.Now.TimeOfDay.ToString(@"hh\-mm\-ss\-ffffff") + ".txt", traceContent);

            }
        }
    }
}
