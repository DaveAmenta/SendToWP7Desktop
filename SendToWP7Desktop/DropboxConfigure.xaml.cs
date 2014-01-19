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
using System.IO;
using System.ComponentModel;
using Microsoft.WindowsAPICodePack.Dialogs;
using System.Text.RegularExpressions;
using DavuxLib2.Extensions;
using DavuxLib2;

namespace SendToWP7Desktop
{
    /// <summary>
    /// Interaction logic for DropboxConfigure.xaml
    /// </summary>
    public partial class DropboxConfigure : Window, INotifyPropertyChanged
    {
        Action SuccessCallback = null;
        Action CancelCallback = null;

        public DropboxConfigure(Action SuccessCallback, Action CancelCallback)
        {
            this.SuccessCallback = SuccessCallback;
            this.CancelCallback = CancelCallback;

            InitializeComponent();

            if (Dropbox.IsConfigured)
            {
                DropboxPath = Dropbox.FullPath;
                DropboxURL = string.Format(Dropbox.URL, Dropbox.ID, "AnyFileName");
                Validate();
            }
            else
            {
                // try and guess the path

                string guessPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                    "My Dropbox");

                if (Directory.Exists(guessPath))
                {
                    DropboxPath = guessPath;
                }

                Validate();
            }

            ObfuscateFileName = Settings.Get("ObfuscateFileName", true);

            DataContext = this;
        }

        private void Validate()
        {
            int points = 0;

            lblVPath.Foreground = Brushes.DarkRed;
            lblVURL.Foreground = Brushes.DarkRed;
            lblVPath.Text = "Dropbox folder is not set";
            lblVURL.Text = "Dropbox public URL is not set";

            if (!string.IsNullOrEmpty(DropboxPath))
            {
                if (Directory.Exists(DropboxPath))
                {
                    string publicPath = Path.Combine(DropboxPath, "Public");

                    if (Directory.Exists(publicPath))
                    {
                        lblVPath.Text = "OK";
                        lblVPath.Foreground = Brushes.DarkGreen;
                        points++;
                    }
                    else
                    {
                        lblVPath.Text = "Incorrect.  Missing 'Public' sub-folder";
                        // public folder is missing.
                    }
                }
                else
                {
                    lblVPath.Text = "Incorrect.  Folder does not exist";
                }
            }

            if (!string.IsNullOrEmpty(DropboxURL))
            {
                var match = Regex.Match(DropboxURL, Regex.Escape("http://dl.dropbox.com/u/") + "(.*?)/(.*?)");
                if (match.Success)
                {
                    DropboxID = match.Groups[1].Value;

                    lblVURL.Foreground = Brushes.DarkGreen;
                    lblVURL.Text = "OK";
                    points++;
                    
                }
                else
                {
                    lblVURL.Text = "URL is not for Dropbox";
                }
            }

            btnSave.IsEnabled = points == 2;
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            // TODO cancel handler
            Close();
            CancelCallback();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            // TODO save ID and path

            Settings.Set("DropboxConfigured", true);
            Settings.Set("DropboxPath", DropboxPath);
            Settings.Set("DropboxID", DropboxID);
            Settings.Set("ObfuscateFileName", ObfuscateFileName);
            // TODO success handler
            
            Close();
            SuccessCallback();
        }

        private void HelpAndSuport_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            this.Try(() => System.Diagnostics.Process.Start("http://www.daveamenta.com/"));
        }

        private void btnSelect_Click(object sender, RoutedEventArgs e)
        {
            CommonOpenFileDialog cfd = new CommonOpenFileDialog();
            
            cfd.EnsureReadOnly = true;
            cfd.IsFolderPicker = true;
            cfd.AllowNonFileSystemItems = false;

            if (cfd.ShowDialog(this) == CommonFileDialogResult.Ok)
            {
                DropboxPath = cfd.FileName;
            }
        }

        private string DropboxID { get; set; }

        private bool _ObfuscateFileName;
        public bool ObfuscateFileName
        {
            get
            {
                return _ObfuscateFileName;
            }
            set
            {
                if (value != _ObfuscateFileName)
                {
                    _ObfuscateFileName = value;
                    NotifyPropertyChanged("ObfuscateFileName");
                }
            }
        }        

        private string _DropboxPath;
        public string DropboxPath
        {
            get
            {
                return _DropboxPath;
            }
            set
            {
                if (value != _DropboxPath)
                {
                    _DropboxPath = value;
                    NotifyPropertyChanged("DropboxPath");
                }
            }
        }

        private string _DropboxURL;
        public string DropboxURL
        {
            get
            {
                return _DropboxURL;
            }
            set
            {
                if (value != _DropboxURL)
                {
                    _DropboxURL = value;
                    NotifyPropertyChanged("DropboxURL");
                }
            }
        }        


        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string prop)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }

        private void DropboxPath_KeyUp(object sender, KeyEventArgs e)
        {
            DropboxPath = (sender as TextBox).Text;
            Validate();
        }

        private void DropboxURL_KeyUp(object sender, KeyEventArgs e)
        {
            DropboxURL = (sender as TextBox).Text;
            Validate();
        }

        private void hOpen_Click(object sender, RoutedEventArgs e)
        {
            this.Try(() => System.Diagnostics.Process.Start(Path.Combine(DropboxPath, "Public")));
        }

    }
}
