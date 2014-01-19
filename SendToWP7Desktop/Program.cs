using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DavuxLib2;
using System.Diagnostics;
using System.IO;
using HttpServer;
using HttpServer.Messages;
using HttpServer.Headers;
using System.Threading;


namespace SendToWP7Desktop
{
    static class Program
    {

        public static HttpServer.Server HTTP = null;
        public static Dictionary<string, string> LocalFileStorage = null;

        public static event Action<JumpListItem> JumpListItemClicked = delegate { };
        public static AppJumpList JumpList = null;

        [System.STAThreadAttribute()]
        public static void Main()
        {
            JumpList = new AppJumpList();

            var jli = new JumpListItem();
            jli.Title = "Quit";
            jli.IconIndex = 3;
            jli.Clicked += item => Quit();
            JumpList.Items.Add(jli);

            JumpList.Items.Add(JumpListItem.SeperatorItem);

            jli = new JumpListItem();
            jli.Title = "Set Transfer Destination";
            jli.IconIndex = 4;
            jli.Clicked += new Action<JumpListItem>(jli_Clicked);
            JumpList.Items.Add(jli);

            JumpList.Items.Add(JumpListItem.SeperatorItem);

            jli = new JumpListItem();
            jli.Title = "Send Note";
            jli.IconIndex = 2;
            jli.Clicked += new Action<JumpListItem>(jli_Clicked);
            JumpList.Items.Add(jli);

            jli = new JumpListItem();
            jli.Title = "Send SMS";
            jli.IconIndex = 5;
            jli.Clicked += new Action<JumpListItem>(jli_Clicked);
            JumpList.Items.Add(jli);

            jli = new JumpListItem();
            jli.Title = "Send Mail";
            jli.IconIndex = 1;
            jli.Clicked += new Action<JumpListItem>(jli_Clicked);
            JumpList.Items.Add(jli);

            jli = new JumpListItem();
            jli.Title = "Send Clipboard";
            jli.IconIndex = 0;
            jli.Clicked += new Action<JumpListItem>(jli_Clicked);
            JumpList.Items.Add(jli);

            /*
            jli = new JumpListItem();
            jli.Title = "Send Search";
            jli.Clicked += new Action<JumpListItem>(jli_Clicked);
            JumpList.Items.Add(jli);

            jli = new JumpListItem();
            jli.Title = "Send Marketplace Search";
            jli.Clicked += new Action<JumpListItem>(jli_Clicked);
            JumpList.Items.Add(jli);

            jli = new JumpListItem();
            jli.Title = "Schedule Reminder";
            jli.Clicked += new Action<JumpListItem>(jli_Clicked);
            JumpList.Items.Add(jli);
            */

            JumpList.Ready();

            DavuxLib2.App.Init("SendToWP7Desktop");

            if (DavuxLib2.App.IsAllowedToExecute(DavuxLib2.LicensingMode.Free) != DavuxLib2.LicenseValidity.OK)
            {
                Environment.Exit(0);
            }


            if (DavuxLib2.App.IsAppAlreadyRunning())
            {
                Quit();
            }

            // ready to start, make sure we were called by ClickOnce
            // but only if we don't have a command line from explorer
            if (Environment.GetCommandLineArgs().Length == 1)
            {
                try
                {
                    var run_file = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SendToWP7Desktop", "launcher");
                    if (File.Exists(run_file))
                    {
                        // OK to run, reset for next time
                        File.Delete(run_file);
                    }
                    else
                    {
                        var app_file = Path.Combine(System.Windows.Forms.Application.StartupPath, "SendToWP7Launcher.exe");
                        // call ClickOnce
                        if (File.Exists(app_file))
                        {
                            System.Diagnostics.Process.Start(app_file, "--restart");
                            Environment.Exit(3);
                        }
                        // else we're not under ClickOnce, so continue
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex);
                    if (Debugger.IsAttached) Debugger.Break();
                }
            }
            new Thread(() =>
            {
                try
                {
                    DavuxLib2.App.SubmitCrashReports();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Crash Reporter Error: " + ex);
                }
            }).Start();

            StartHTTP();

            /*
            try
            {
                HttpServer = new DavuxLib2.HTTP.Server(Settings.Get("HttpPort", 7780));
                HttpServer.OnRequest += (request, server) =>
                    {
                        try
                        {
                            request.Response.Code = DavuxLib2.HTTP.Server.StatusCodes.BAD_REQUEST;
                            request.Response.MimeType = "text/plain";
                            if (request.Headers.URL == "/")
                            {
                                if (request.Headers.QueryString.ContainsKey("item"))
                                {
                                    Trace.WriteLine("HTTP item: " + request.Headers.QueryString["item"]);
                                    if (LocalFileStorage.ContainsKey(request.Headers.QueryString["item"]))
                                    {
                                        try
                                        {
                                            Trace.WriteLine("HTTP request for file " + LocalFileStorage[request.Headers.QueryString["item"]]);
                                            request.Response.AddHeader("Last-Modified", DateTime.Now.ToString("r"));
                                            request.Response.AddHeader("Date", DateTime.Now.ToString("r"));
                                            request.Response.AddHeader("Server", "DIS/1.0 libDx/1.0");
                                            request.Response.AddHeader("etag", request.Headers.QueryString["item"]);

                                            // request.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + Path.GetFileName(LocalFileStorage[request.Headers.QueryString["item"]]) + "\"");
                                            // request.Response.AddHeader("accept-ranges", "bytes");
                                            request.Response.Code = DavuxLib2.HTTP.Server.StatusCodes.OK;
                                            request.Response.MimeType = Mime.MimeFromRegistry(Path.GetExtension(LocalFileStorage[request.Headers.QueryString["item"]]));
                                            request.Response.Body = File.ReadAllBytes(LocalFileStorage[request.Headers.QueryString["item"]]);
                                        }
                                        catch (Exception ex)
                                        {
                                            request.Response.Code = DavuxLib2.HTTP.Server.StatusCodes.NOT_FOUND;
                                            request.Response.MimeType = "text/plain";
                                            request.Response.BodyString = "File not found: " + ex.Message;
                                        }
                                        return; // don't give invalid request, the user has "authenticated"
                                    }
                                }
                            }
                            request.Response.BodyString = "Invalid Request";
                        }
                        catch (Exception ex)
                        {
                            Trace.WriteLine("HTTP Request Failed: " + ex);
                        }
                    };
                HttpServer.Start();

                LocalFileStorage = Settings.Get("LocalFiles", new Dictionary<string, string>());
                LocalFileStoragePW = Settings.Get("LocalFilesPW", new Dictionary<string, string>());
            }
            catch (Exception ex)
            {
                Trace.WriteLine("HTTP Failed to start: " + ex);
                HttpServer = null;
            }
            */


            SendToWP7Desktop.App app = new SendToWP7Desktop.App();
            app.InitializeComponent();
            app.Run();
        }

        private static void StartHTTP()
        {
            try
            {
                HttpServer.Logging.LogFactory.Assign(new HttpServer.Logging.ConsoleLogFactory(new HttpServer.Logging.LogFilter()));
                HTTP = new Server();
                HTTP.Add(HttpListener.Create(System.Net.IPAddress.Any, Settings.Get("HttpPort", 7780)));
                HTTP.Add(new HttpMod());
                HTTP.Start(32);

                LocalFileStorage = Settings.Get("LocalFiles", new Dictionary<string, string>());
            }
            catch (Exception ex)
            {
                Trace.WriteLine("HTTP Startup Error: " + ex.Message);
            }
        }

        static void jli_Clicked(JumpListItem item)
        {
            JumpListItemClicked(item);
        }

        internal static void Quit()
        {
            try
            {
                SetMenu(false);
                if (Settings.Initialized)
                {
                    Settings.Set("LocalFiles", LocalFileStorage);
                }
                
            }
            catch (Exception ex)
            {
                Trace.WriteLine("Exit error: " + ex);
            }
            Environment.Exit(2);
        }

        internal static void SetMenu(bool enabled)
        {
            ShellContextMenu.SetMenu("Send to WP7", "\"" + System.Windows.Forms.Application.ExecutablePath + "\"" + " \"%1\"", enabled);
        }
    }

    class HttpMod : HttpServer.Modules.IModule
    {

        #region IModule Members

        public ProcessingResult Process(RequestContext context)
        {
            IRequest request = context.Request;
            IResponse response = context.Response;
            string path = request.Uri.AbsolutePath;
            path = Uri.UnescapeDataString(path);
            Debug.WriteLine("Request for: " + path);
            try
            {
                response.Status = System.Net.HttpStatusCode.BadRequest;
                response.ContentType = new HttpServer.Headers.ContentTypeHeader("text/plain");

                var item = path.Remove(0, 1);

                Trace.WriteLine("HTTP item: " + item);
                if (Program.LocalFileStorage.ContainsKey(item))
                {
                    try
                    {
                        var file = Program.LocalFileStorage[item];
                        Trace.WriteLine("HTTP request for file " + file);
                        response.Add(new StringHeader("Content-Disposition", "attachment; filename=\"" + Path.GetFileName(file) + "\""));
                        response.Status = System.Net.HttpStatusCode.OK;
                        response.ContentType = new HttpServer.Headers.ContentTypeHeader(Mime.MimeFromRegistry(Path.GetExtension(file)));
                        using (var fo = File.OpenRead(file))
                        {
                            SendFile(context.HttpContext, response, fo);
                        }
                    }
                    catch (Exception ex)
                    {
                        response.Status = System.Net.HttpStatusCode.NotFound;
                        Trace.WriteLine(ex);
                       // SendFile(context.HttpContext, response, "File Not Found: " + ex.Message);
                    }
                    return ProcessingResult.Abort; // don't give invalid request, the user has "authenticated"
                }
            }
            catch (Exception ex)
            {
                SendFile(context.HttpContext, response, ex.Message);
            }
            SendFile(context.HttpContext, response, "Bad Request");

            return ProcessingResult.Abort;
        }



        private void SendFile(IHttpContext context, IResponse response, byte[] data)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream(data);
            SendFile(context, response, ms);
        }

        private void SendFile(IHttpContext context, IResponse response, string data)
        {
            System.IO.MemoryStream ms = new System.IO.MemoryStream(
                Encoding.UTF8.GetBytes(data));

            SendFile(context, response, ms);
        }

        private void SendFile(IHttpContext context, IResponse response, System.IO.Stream stream)
        {
            response.ContentLength.Value = stream.Length;

            var generator = HttpFactory.Current.Get<ResponseWriter>();
            generator.SendHeaders(context, response);
            generator.SendBody(context, stream);
        }


        #endregion
    }
}
