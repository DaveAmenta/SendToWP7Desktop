using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Win32;

namespace SendToWP7Desktop
{
    class ShellContextMenu
    {
        //  System.Windows.Forms.Application.ExecutablePath + " \"%1\""
        public static void SetMenu(string app, string command, bool enabled = true)
        {
            if (enabled)
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Classes\\*\\shell\\" + app + "\\command");

                // always update the key!
                //if (key == null)
                //{
                    key = Registry.CurrentUser.CreateSubKey("Software\\Classes\\*\\shell\\" + app + "\\command");
                    key.SetValue("", command);
                //}
            }
            else
            {
                RegistryKey key = Registry.CurrentUser.OpenSubKey("Software\\Classes\\*\\shell\\" + app);

                if (key != null)
                {
                    Registry.CurrentUser.DeleteSubKeyTree("Software\\Classes\\*\\shell\\" + app);
                }
            }
        }
    }
}
