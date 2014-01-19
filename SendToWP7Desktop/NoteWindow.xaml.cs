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
    /// Interaction logic for NoteWindow.xaml
    /// </summary>
    public partial class NoteWindow : Window
    {
        public NoteWindow()
        {
            InitializeComponent();

            SourceInitialized += (_, __) =>
                {
                    DavuxLib2.Platform.DwmApi.DwmExtendFrameIntoClientArea(this, new Thickness(0, 0, 0, btnSend.ActualHeight + 9 * this.GetDPI()));
                };
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            string text = txtInput.Text;
            new Thread(() =>
                {
                    var ret = App.ViewModel.API.SendMessage("note", "Note", "", text);
                    this.Invoke(() =>
                            {
                                if (ret.Error)
                                {
                                    btnSend.IsEnabled = true;
                                    txtInput.IsEnabled = true;
                                    // display error
                                }
                                else
                                {
                                    Close();
                                }
                            });
                }).Start();
            btnSend.IsEnabled = false;
            txtInput.IsEnabled = false;
        }
    }
}
