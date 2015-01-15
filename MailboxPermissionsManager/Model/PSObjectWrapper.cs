using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Management.Automation;

namespace MailboxPermissionsManager.Model
{
    abstract class PSObjectWrapper
    {
        protected PSObject psObject;

        public PSObjectWrapper(PSObject psObject)
        {
            this.psObject = psObject;
        }

        public string GetPSPropertyString(string propertyName)
        {
            if (psObject.Properties[propertyName].TypeNameOfValue == "System.String")
                return psObject.Properties[propertyName].Value.ToString();
            else
                throw new InvalidCastException("Property is not a string");
        }

        private void setPropertyString(string propertyName, string value)
        {
            if (!string.IsNullOrEmpty(value))
                psObject.Properties[propertyName].Value = value;
            else
                psObject.Properties[propertyName].Value = null;
        }
    }
}