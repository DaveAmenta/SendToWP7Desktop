using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Reflection;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.IO.Pipes;
using System.IO;
using System.Threading;

namespace SendToWP7Desktop
{
    public class JumpListItem
    {
        public static JumpListItem SeperatorItem { get; set; }

        static JumpListItem()
        {
            SeperatorItem = new JumpListItem();
            SeperatorItem.Title = "-";
        }

        public static readonly int InvalidIconIndex = -1;

        public string Title { get; set; }
        public int IconIndex { get; set; }
        public string Argument
        {
            get
            {
                // Make Title safe to pass on the command line
                return Regex.Replace(Title, @"[^\w\.@-]", "");
            }
        }

        public event Action<JumpListItem> Clicked = delegate { };

        public JumpListItem()
        {
            Title = "";
            IconIndex = InvalidIconIndex;
        }

        public void OnClicked()
        {
            Clicked(this);
        }
    }

    public class AppJumpList
    {
        JumpList _jumplist = null;

        public event Action<string> OtherCommandLineReceived = delegate { };

        public List<JumpListItem> Items { get; set; }

        public string PipeName { get; set; }

        public AppJumpList()
        {
            Items = new List<JumpListItem>();
            PipeName = Regex.Replace(Assembly.GetExecutingAssembly().GetName().Name, @"[^\w\.@-]", "");
        }

        /// <summary>
        /// Called after the Items collection has been populated
        /// </summary>
        public void Ready()
        {
            if (!RunningOnWin7) return;

            Trace.WriteLine("App Ready: Pipe: " + PipeName + " || Cmd: " + Environment.CommandLine);
            var args = Environment.GetCommandLineArgs();
            if (args.Length > 1)
            {
                //if (args[1].StartsWith("--jump-"))
                //{
                    if (Send_Message(args[1])) // .Remove(0, "--jump-".Length)
                    {
                        Trace.WriteLine("Sent message, exiting");
                        Environment.Exit(0);
                    }
                    Trace.WriteLine("Could not contact running instance, spinning up a new instance");
               // }
            }
            else
            {
                Server_Start();
            }
        }

        private void OnCommandReceived(string line)
        {
            Trace.WriteLine("Pipe Command: " + line);

            foreach (var item in Items)
            {
                if ("--jump-" + item.Argument == line)
                {
                    item.OnClicked();
                    return;
                }
            }
            Trace.WriteLine("Invalid JumpList command: " + line);
            OtherCommandLineReceived(line);
        }

        private void Server_Start()
        {
            new Thread(() =>
            {
                Trace.WriteLine("Starting Pipe Server");
                int t = 0;
                while (true)
                {
                    try
                    {
                        using (NamedPipeServerStream pipeServer =
                new NamedPipeServerStream(PipeName, PipeDirection.In))
                        {
                            Trace.WriteLine("Waiting for pipe connection...");
                            pipeServer.WaitForConnection();

                            Trace.WriteLine("Client connected.");
                            try
                            {
                                // Read user input and send that to the client process.
                                using (StreamReader sr = new StreamReader(pipeServer))
                                {
                                    string line = sr.ReadLine();
                                    Trace.WriteLine("Read from pipe: " + line);
                                    OnCommandReceived(line);
                                    sr.Close();
                                }
                            }
                            // Catch the IOException that is raised if the pipe is broken
                            // or disconnected.
                            catch (IOException e)
                            {
                                Trace.WriteLine("Pipe I/O Error: {0}", e.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("*** Pipe Server Crashed: " + ex);
                        t++;
                        if (t > 1)
                        {
                            Trace.WriteLine("Pipe Server Crashed Max Times");
                            return;
                        }
                    }
                }
            }).Start();
        }

        private bool Send_Message(string message)
        {
            try
            {
                using (NamedPipeClientStream pipeClient = new NamedPipeClientStream(".", PipeName, PipeDirection.Out))
                {
                    Console.Write("Attempting to connect to pipe...");
                    pipeClient.Connect(500);
                    using (StreamWriter sw = new StreamWriter(pipeClient))
                    {
                        sw.WriteLine(message);
                        sw.Flush();
                        sw.Close();
                    }
                    pipeClient.Close();
                }
                return true;
            }
            catch (TimeoutException)
            {
                Trace.WriteLine("Pipe Timeout");
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Pipe Send Error: " + ex);
            }
            return false;
        }

        public void SetJumpList()
        {
            if (!RunningOnWin7) return;

            _jumplist = JumpList.CreateJumpList();
            _jumplist.JumpListItemsRemoved += (ss, ee) =>
            {

            };
            foreach (var x in _jumplist.RemovedDestinations)
            {
                
            }

            foreach (var item in Items)
            {
                AddJumpListItem(item);
            }

            _jumplist.Refresh();
        }

        private void AddJumpListItem(JumpListItem jli)
        {
            if (jli == JumpListItem.SeperatorItem)
            {
                _jumplist.AddUserTasks(new JumpListSeparator());
            }
            else
            {
                string fullpath = new Uri(Assembly.GetExecutingAssembly().GetName().CodeBase).LocalPath;

                var task = new JumpListLink(fullpath, jli.Title);
                task.Arguments = "--jump-" + jli.Argument;
                if (jli.IconIndex != JumpListItem.InvalidIconIndex)
                {
                    try
                    {
                        task.IconReference = new Microsoft.WindowsAPICodePack.Shell.IconReference(Path.Combine(System.Windows.Forms.Application.StartupPath, "IconPack.dll"), jli.IconIndex);
                    }
                    catch (Exception ex)
                    {
                        Trace.WriteLine("Error associating icon with jumplist item: " + ex);
                    }
                }
                _jumplist.AddUserTasks(task);
            }
        }

        public static bool RunningOnWin7
        {
            get
            {
                // Verifies that OS version is 6.1 or greater, and the Platform is WinNT.
                return Environment.OSVersion.Platform == PlatformID.Win32NT &&
                    Environment.OSVersion.Version.CompareTo(new Version(6, 1)) >= 0;
            }
        }
    }
}
