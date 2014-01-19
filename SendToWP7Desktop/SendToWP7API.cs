using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Diagnostics;

namespace SendToWP7Desktop
{
    public class SendToWP7API
    {

        public class WP7PushResult
        {
            public string NotificationConnectionStatus { get; set; }
            public string DeviceConnectionStatus { get; set; }
            public string SubscriptionStatus { get; set; }

            public bool Error { get; set; }
        }

        public string PairCode { get; set; }

        private string URL = "http://daveamenta.com/wp7api/com.davux.ChromeToWindowsPhone/api2.php?passcode={0}&type={1}&title={2}&url={3}&sel={4}";

        public event Action<WP7PushResult> RequestFinished = delegate { };
        public event Action RequestInProgress = delegate { };

        public WP7PushResult SendMessage(string type, string title, string url, string text)
        {
            Trace.WriteLine("Sending message " + type + " with url " + url);
            RequestInProgress();

            var ret = new WP7PushResult();
            try
            {
                var uri = string.Format(URL,
                    Uri.EscapeDataString(App.ViewModel.PairCode),
                    Uri.EscapeDataString(type),
                    Uri.EscapeDataString(title),
                    Uri.EscapeDataString(url),
                    Uri.EscapeDataString(text));
                Trace.WriteLine("Request: " + uri);
                string sret = new WebClient().DownloadString(new Uri(uri));
                Trace.WriteLine("Request Result: " + sret);
            }
            catch (Exception ex)
            {
                ret.Error = true;
                Trace.WriteLine("Request Error: " + ex);
            }
            RequestFinished(ret);
            return ret;
        }
    }
}
