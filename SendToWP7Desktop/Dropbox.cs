using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DavuxLib2;
using System.IO;
using System.Diagnostics;
using System.Threading;

namespace SendToWP7Desktop
{
    class Dropbox
    {
        public static string URL = "http://dl.dropbox.com/u/{0}/{1}";

        public static bool IsConfigured
        {
            get
            {
                return Settings.Get("DropboxConfigured", false);
            }
        }

        public static string FullPath
        {
            get
            {
                return Settings.Get("DropboxPath", "");
            }
        }

        public static string ID
        {
            get
            {
                return Settings.Get("DropboxID", "");
            }
        }

        public static void Configure(Action Success, Action Cancel)
        {
            //var t = new Thread(() =>
            //{
                DropboxConfigure dc = new DropboxConfigure(Success, Cancel);
                dc.Show();
            //    System.Windows.Threading.Dispatcher.Run();
            //});
           // t.SetApartmentState(ApartmentState.STA);
           // t.Start();
        }

        internal static UploadResult UploadFile(string filename)
        {
            var ret = new UploadResult();

            // TODO copy file to public folder

            string newFileName = Path.GetFileName(filename);
            if (Settings.Get("ObfuscateFileName", true))
            {
                newFileName = "S2WP7_" + GetRandomHexNumber(2) + "_" + Path.GetFileName(filename);
            }

            string pubFolder = Path.Combine(Dropbox.FullPath, "Public",newFileName);

            try
            {
                Trace.WriteLine("Copying " + filename + " to " + pubFolder);
                File.Copy(filename, pubFolder, false);
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Error copying file to dropbox: " + ex);
                ret.Error = true;
            }


            
            ret.URL = string.Format(URL, ID, newFileName);
            ret.Icon = ""; // TODO icon
            return ret;
        }

        static Random random = new Random();
        public static string GetRandomHexNumber(int digits)
        {
            byte[] buffer = new byte[digits / 2];
            random.NextBytes(buffer);
            string result = String.Concat(buffer.Select(x => x.ToString("X2")).ToArray());
            if (digits % 2 == 0)
                return result;
            return result + random.Next(16).ToString("X");
        }
    }
}
