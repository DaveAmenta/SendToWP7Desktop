using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Diagnostics;
using System.IO;
using DavuxLib2;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Runtime.InteropServices;

namespace SendToWP7Desktop
{
    public class UploadResult
    {
        public string Icon { get; set; }
        public string URL { get; set; }
        public bool Error { get; set; }

        public UploadResult(string Icon, string URL)
        {
            this.Icon = Icon;
            this.URL = URL;
        }

        public UploadResult() 
        {
        }

        public override string ToString()
        {
            return "URet: " + URL;
        }
    }

    public enum UploadOptions
    {
        Localhostr,
        LocalWebServer,
        Dropbox,
        NotSet
    }

    class Uploader
    {
        public static void ShowUploadDialog(string DisplayFileName, Action LocalhostrCallback,
            Action LocalWebHostCallback, Action DropboxCallback, Action CancelCallback, IntPtr owner)
        {
            var success = false;

            TaskDialog confirm = new TaskDialog();
            
            confirm.FooterText = "Read the <a href=\"http://localhostr.com/tos\">terms of service</a> for localhostr.com.  Your files are not in a public listing, but anyone knowing the URL may access them directly.  You are never able to delete uploaded content.";
            confirm.Cancelable = true;
            confirm.Caption = "Options for accessing your content";
            confirm.InstructionText = "How would you like to transfer " + DisplayFileName + " to your phone?";

            confirm.HyperlinksEnabled = true;
            confirm.HyperlinkClick += (s, e) => System.Diagnostics.Process.Start("http://localhostr.com/tos");
            confirm.StandardButtons = TaskDialogStandardButtons.Cancel;

            TaskDialogCommandLink localhostr = new TaskDialogCommandLink();
            localhostr.Text = "Upload to localhostr.com";
            localhostr.Instruction = "Files are accessible over WiFi and 3G, but may not be secure.  Files are uploaded to the internet.";
            localhostr.Click += (s, e) =>
            {
                success = true;
                confirm.Close();
                LocalhostrCallback();
            };
            confirm.Controls.Add(localhostr);

            TaskDialogCommandLink localserver = new TaskDialogCommandLink();
            localserver.Default = true;
            localserver.Text = "Host directly on my PC";
            localserver.Enabled = Program.LocalFileStorage != null;
            localserver.Instruction = "Files are accessible at high speed, only over WiFi, but never transferred over the internet.  Files will not be copied, so if the file is removed from your computer, it will not be accessible to the phone.";
            localserver.Click += (s, e) =>
            {
                success = true;
                confirm.Close();
                LocalWebHostCallback();
            };
            confirm.Controls.Add(localserver);

            TaskDialogCommandLink dropbox = new TaskDialogCommandLink();
            dropbox.Text = "Host using my Dropbox account";
            dropbox.Instruction = "Files are accessible over WiFi and 3G, and are under your control.  Files are copied to your Dropbox Public folder.";
            dropbox.Click += (s, e) =>
            {
                success = true;
                confirm.Close();
                DropboxCallback();
            };
            confirm.Controls.Add(dropbox);

            TaskDialogRadioButton askEachTime = new TaskDialogRadioButton();
            askEachTime.Default = Settings.Get("AskEachTime", true);
            askEachTime.Text = "Ask me how to transfer files each time";
            askEachTime.Click += (_, __) =>
            {
                Settings.Set("AskEachTime", true);
            };
            confirm.Controls.Add(askEachTime);
            TaskDialogRadioButton dontAsk = new TaskDialogRadioButton();
            dontAsk.Default = false == Settings.Get("AskEachTime", true);
            dontAsk.Text = "Always use the option I select below";
            dontAsk.Click += (_, __) =>
            {
                Settings.Set("AskEachTime", false);
            };
            confirm.Controls.Add(dontAsk);

            confirm.Closing += (s, e) =>
                {
                    // this is always returning Cancel even if CommandLink is clicked
                    if (e.TaskDialogResult == TaskDialogResult.Cancel && !success)
                    {
                         CancelCallback();
                    }
                };

            var t = new Thread(() =>
                {
                    int x = 0;

                    while (x < 25)
                    {
                        if (confirm.Handle != IntPtr.Zero)
                        {
                            Trace.WriteLine("Handle: " + confirm.Handle);
                            Thread.Sleep(200);
                            BringWindowToTop(confirm.Handle);
                            return;
                        }
                        Thread.Sleep(50);
                        x++;
                    }
                });
            t.SetApartmentState(ApartmentState.STA);
            t.Start();
            Trace.WriteLine("UPloader Handle: " + confirm.Handle);
            confirm.OwnerWindowHandle = owner;
            confirm.Show();
        }

        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BringWindowToTop(IntPtr hWnd);

        public static void PresentOptions(Action SuccessCallback, Action CancelCallback, IntPtr owner)
        {
            Action localhostrCallback = () =>
            {
                Settings.Set("UploadType", UploadOptions.Localhostr);
                SuccessCallback();
            };
            Action LocalWebServerCallback = () =>
            {
                Settings.Set("UploadType", UploadOptions.LocalWebServer);
                SuccessCallback();
            };
            Action DropboxCallback = () =>
            {
                Settings.Set("UploadType", UploadOptions.Dropbox);
                Dropbox.Configure(SuccessCallback, CancelCallback);
            };

            ShowUploadDialog("documents and photos", localhostrCallback, LocalWebServerCallback, DropboxCallback, CancelCallback, owner);
        }


        public static void UploadFile(string filename, Action<UploadResult> Callback, Action CancelCallback)
        {
            Action localhostrCallback = () =>
            {
                Settings.Set("UploadType", UploadOptions.Localhostr);
                Callback(Localhostr.UploadFile(filename));
            };
            Action LocalWebServerCallback = () =>
            {
                Settings.Set("UploadType", UploadOptions.LocalWebServer);

                var file_guid = Guid.NewGuid() + "." + Path.GetExtension(filename);

                Program.LocalFileStorage[file_guid] = filename;

                string addr = LocalEndPoint;

                if (Settings.Get("UseFixedIPAddress", false))
                {
                    Trace.Write("Using Fixed IP Address");
                    addr = Settings.Get("FixedIPAddress", "[UnknownIP]");
                }

                Callback(new UploadResult
                {
                    Icon = "",  // TODO icon
                    URL = string.Format("http://{0}:{1}/{2}",
                        addr,
                        Settings.Get("HttpPort", 7780),
                        file_guid)
                });
            };
            Action DropboxCallback = () =>
            {
                Settings.Set("UploadType", UploadOptions.Dropbox);

                Action upload = () =>
                    {
                        Callback(Dropbox.UploadFile(filename));
                    };

                if (Dropbox.IsConfigured)
                {
                    // configured, so do the uplod
                    upload();
                }
                else
                {
                    Dropbox.Configure(() =>
                        {
                            // configured, so do the upload
                            upload();
                        }, CancelCallback);
                }
            };

            if (Settings.Get("AskEachTime", true) || Settings.Get("UploadType", UploadOptions.NotSet) == UploadOptions.NotSet)
            {
                ShowUploadDialog(Path.GetFileName(filename), localhostrCallback, LocalWebServerCallback, DropboxCallback, CancelCallback, IntPtr.Zero);
            }
            else
            {
                switch (Settings.Get("UploadType", UploadOptions.NotSet))
                {
                    case UploadOptions.Localhostr:
                        localhostrCallback();
                        break;
                    case UploadOptions.LocalWebServer:
                        LocalWebServerCallback();
                        break;
                    case UploadOptions.Dropbox:
                        DropboxCallback();
                        break;
                    default:
                        Trace.WriteLine("Invalid upload option!");
                        CancelCallback();
                        break;
                }
            }
        }

        private static string LocalEndPoint
        {
            // _tcpListener.Server.LocalEndPoint
            get
            {
                IPHostEntry host;
                host = Dns.GetHostEntry(Dns.GetHostName());
                foreach (IPAddress ip in host.AddressList)
                {
                    if (ip.AddressFamily == AddressFamily.InterNetwork
                        && !ip.ToString().StartsWith("127.0")
                        && !ip.ToString().StartsWith("169.254"))
                    {
                        return ip.ToString();
                    }
                }
                return Dns.GetHostName();
            }
        }
    }
}
