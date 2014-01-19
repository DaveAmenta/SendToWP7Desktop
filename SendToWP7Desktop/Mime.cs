using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;

namespace SendToWP7Desktop
{
    class Mime
    {
        public static string MimeFromRegistry(string ext)
        {
            // TODO security issue with passing ext into registry?
            string mimeType = "application/octet-stream";
            try
            {
                Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
                if (regKey != null && regKey.GetValue("Content Type") != null)
                {
                    mimeType = regKey.GetValue("Content Type").ToString();
                }
            }
            catch
            {

            }
            Trace.WriteLine("MIME Map: " + ext + " -> " + mimeType);

            return mimeType;
        }
    }
}
