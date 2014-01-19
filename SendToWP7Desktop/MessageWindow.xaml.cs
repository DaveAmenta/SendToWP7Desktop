using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DavuxLib2.Extensions;
using System.Threading;

namespace SendToWP7Desktop
{
    /// <summary>
    /// Interaction logic for MessageWindow.xaml
    /// </summary>
    public partial class MessageWindow : Window
    {
        public MessageWindowType Type { get; private set; }

        public MessageWindow(MessageWindowType Type)
        {
            InitializeComponent();

            this.Type = Type;

            switch (this.Type)
            {
                case MessageWindowType.EMail:
                    Title = "E-Mail";
                    break;
                case MessageWindowType.SMS:
                    Title = "SMS";
                    break;
            }

            SourceInitialized += (_, __) =>
            {
                DavuxLib2.Platform.DwmApi.DwmExtendFrameIntoClientArea(this, new Thickness(0, 0, 0, btnSend.ActualHeight + 9 * this.GetDPI()));
            };
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string text = txtInput.Text;
            string to = txtTo.Text;
            new Thread(() =>
            {
                var ret = App.ViewModel.API.SendMessage(
                    Type == MessageWindowType.SMS ? "sms" : "email", to, "", text);
                this.Invoke(() =>
                    {
                        if (ret.Error)
                        {
                            btnSend.IsEnabled = true;
                            txtInput.IsEnabled = true;
                            txtTo.IsEnabled = true;
                            // TODO display error
                        }
                        else
                        {
                            Close();
                        }
                    });
            }).Start();
            btnSend.IsEnabled = false;
            txtInput.IsEnabled = false;
            txtTo.IsEnabled = false;
        }
    }

    public enum MessageWindowType
    {
        SMS, EMail
    }
}
