using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.IO;
using System.Diagnostics;
using System.Windows;

namespace SendToWP7Launcher
{
    static class Program
    {


        [System.STAThreadAttribute()]
        public static void Main()
        {
            var args = Environment.GetCommandLineArgs();
            if (args.Length == 2)
            {
                if (args[1] == "--restart")
                {
                    var shortcutName = Path.Combine(System.Windows.Forms.Application.StartupPath, "launcher.appref-ms");
                    System.Diagnostics.Process.Start(shortcutName);
                    return;
                }
            }


            var run_file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), 
                "SendToWP7Desktop", "launcher");
            try
            {
                File.WriteAllText(run_file, "OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                if (Debugger.IsAttached) Debugger.Break();
            }

            System.Diagnostics.Process.Start("SendToWP7Desktop.exe");
        }

    }
}
