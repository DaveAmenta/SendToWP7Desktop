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
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Taskbar;
using System.Runtime.InteropServices;
using System.Windows.Interop;
using System.Diagnostics;
using System.Windows.Threading;
using DavuxLib2.Extensions;
using System.Threading;
using System.IO;
using Microsoft.WindowsAPICodePack.Dialogs;
using DavuxLib2;
using System.Net;

namespace SendToWP7Desktop
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            App.ViewModel = new AppViewModel();
            DataContext = App.ViewModel;

            WindowState = System.Windows.WindowState.Minimized;

            Loaded += (_, __) =>
                {

                    if (!AppJumpList.RunningOnWin7)
                    {
                        MessageBox.Show("This application requires Windows 7", "Windows 7 Required", MessageBoxButton.OK);
                        Environment.Exit(1);
                    }

                    Program.JumpList.SetJumpList();

                    Program.JumpList.OtherCommandLineReceived += line =>
                        {
                            // from explorer
                            Trace.WriteLine("CLI from explorer: " + line);

                            var list = new List<string>();
                            list.Add(line);
                            UploadFileList(list);
                        };

                    Program.SetMenu(true);

                    if (Program.HTTP == null)
                    {
                        FlashProgress(TaskbarProgressBarState.Error);
                    }
                };

            Closing += (_, e) =>
                {
                    WindowState = System.Windows.WindowState.Minimized;
                    e.Cancel = true;
                };

            Program.JumpListItemClicked += item =>
                {
                    this.Invoke( () =>
                        {
                            Window w = null;
                            switch (item.Title)
                            {
                                case "Send Note":
                                    w = new NoteWindow();
                                    break;
                                case "Send SMS":
                                    w = new MessageWindow(MessageWindowType.SMS);
                                    break;
                                case "Send Mail":
                                    w = new MessageWindow(MessageWindowType.EMail);
                                    break;
                                case "Send Clipboard":
                                    SendClipboard();
                                    break;
                                case "Set Transfer Destination":
                                    Uploader.PresentOptions(() => {
                                        FlashProgress(TaskbarProgressBarState.Normal);
                                    }, () => {
                                        FlashProgress(TaskbarProgressBarState.Paused);
                                    }, IntPtr.Zero);
                                    break;
                                default:
                                    Trace.WriteLine("Invalid jumplist item: " + item);
                                    break;
                            }
                            if (w != null)
                            {
                                w.Show();
                                w.Topmost = true;
                                w.Activate();
                                this.InvokeDelay(1000, () => w.Topmost = false);
                            }
                        });
                };
        }

        private void SendClipboard()
        {
            if (Clipboard.ContainsText())
            {
                var clipText = Clipboard.GetText();
                TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate);
                new Thread(() =>
                {
                    App.ViewModel.API.SendMessage("clip", "Clipboard Text", "", clipText);
                    FlashProgress(TaskbarProgressBarState.Normal);
                }).Start();
            }
            else if (Clipboard.ContainsImage())
            {
                try
                {
                    TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate);
                    
                    var clipimg = Clipboard.GetImage();
                    string path = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                       "S2WP7_" + new Random().Next(0, int.MaxValue) + "_Clipboard");

                    if (Settings.Get("Clipboard_PNG", true))
                    {
                        path += ".png";
                    }
                    else
                    {
                        path += ".jpg";
                    }

                    using (FileStream stream = new FileStream(path, FileMode.Create))
                    {
                        BitmapEncoder encoder;
                        if (Settings.Get("Clipboard_PNG", true))
                        {
                            encoder = new PngBitmapEncoder();
                            (encoder as PngBitmapEncoder).Interlace = PngInterlaceOption.On;
                        }
                        else
                        { 
                            encoder = new JpegBitmapEncoder();
                            (encoder as JpegBitmapEncoder).QualityLevel = Settings.Get("JpegQuality", 95);
                        }
                        encoder.Frames.Add(BitmapFrame.Create(clipimg));
                        encoder.Save(stream);
                    }
                    Trace.WriteLine("Image saved at: " + path);

                    var t = new Thread(() =>
                    {
                        try
                        {
                            Uploader.UploadFile(path, ret =>
                            {
                                if (ret.URL == null)
                                {
                                    Trace.WriteLine("*** URL is null");
                                    FlashProgress(TaskbarProgressBarState.Error);
                                }
                                else
                                {
                                    App.ViewModel.API.SendMessage("img", "Clipboard Image", ret.URL, "");
                                    FlashProgress(TaskbarProgressBarState.Normal);
                                }
                            }, () =>
                            {
                                FlashProgress(TaskbarProgressBarState.Paused);
                            });
                        }
                        catch (Exception ex)
                        {
                            FlashProgress(TaskbarProgressBarState.Error);
                            Trace.WriteLine("Error uploading file: " + ex);
                        }
                    });
                    t.SetApartmentState(ApartmentState.STA);
                    t.Start();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Error saving image:" + ex);
                }
            }
            else if (Clipboard.ContainsFileDropList())
            {
                UploadFileList(GetFilesFromDropList(Clipboard.GetFileDropList()));
            }
            else
            {
                FlashProgress(TaskbarProgressBarState.Error);
                Trace.WriteLine("Clipboard content is not valid");
            }
        }

        private void UploadFileList(List<string> files)
        {
            this.Invoke(() =>
            {
                try
                {
                    this.Invoke(() => TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate));
                    foreach (var file in files)
                    {
                        Uploader.UploadFile(file, ret =>
                            {
                                Trace.WriteLine("Upload result: " + ret);
                                if (ret.URL == null)
                                {
                                    Trace.WriteLine("*** URL is null");
                                    FlashProgress(TaskbarProgressBarState.Error);
                                }
                                else
                                {
                                    App.ViewModel.API.SendMessage("file", "File " + System.IO.Path.GetFileName(file), ret.URL, "");
                                    FlashProgress(TaskbarProgressBarState.Normal);
                                }
                            }, () =>
                            {
                                FlashProgress(TaskbarProgressBarState.Paused);
                            });
                    }
                }
                catch (Exception ex)
                {
                    FlashProgress(TaskbarProgressBarState.Error);
                    Trace.WriteLine("Error uploading file: " + ex);
                }
            });
        }

        private List<string> GetFilesFromDropList(System.Collections.Specialized.StringCollection list)
        {
            List<string> ret = new List<string>();
            foreach (var file in list)
            {
                Trace.WriteLine("File in drop list: " + file);

                GetFiles(file, ref ret);
            }
            return ret;
        }

        private void GetFiles(string file_or_folder, ref List<string> RealFiles)
        {
            if (Directory.Exists(file_or_folder))
            {

                foreach (var f in Directory.GetFiles(file_or_folder))
                {
                    RealFiles.Add(f);
                }

                foreach (var fo in Directory.GetDirectories(file_or_folder))
                {
                    GetFiles(fo, ref RealFiles);
                }
            }
            else
            {
                RealFiles.Add(file_or_folder);
            }
        }

        private void FlashProgress(TaskbarProgressBarState state)
        {
            this.Invoke(() =>
            {
                TaskbarManager.Instance.SetProgressState(state);
                TaskbarManager.Instance.SetProgressValue(100, 100);
            });
            this.InvokeDelay(5000, () => TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress));
        }

        private void HelpAndSuport_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            this.Try(() => System.Diagnostics.Process.Start("http://www.daveamenta.com/wp7"));
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            Program.Quit();
        }

        private void FindPairCode_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            this.Try(() => System.Diagnostics.Process.Start("http://www.daveamenta.com/wp7"));
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            App.ViewModel.PairCode = txtPairCode.Text;

            ValidatePairCode();
        }

        private void ValidatePairCode()
        {
            if (!string.IsNullOrWhiteSpace(App.ViewModel.PairCode))
            {
                new Thread(() =>
                    {
                        var ret = new WebClient().DownloadString(
                            "http://www.daveamenta.com/wp7api/pair_test.php?passcode=" + App.ViewModel.PairCode);
                        if (ret == "OK")
                        {
                            this.Invoke(() =>
                                {
                                    lblPairCodeValidate.Text = "Pair code is OK.  You are ready to start using Send to WP7 Desktop";
                                    lblPairCodeValidate.Foreground = Brushes.DarkGreen;
                                });
                        }
                        else  // NotFound
                        {
                            this.Invoke(() =>
                            {
                                lblPairCodeValidate.Text = "Pair code is not valid.  Check pair code and try again.";
                                lblPairCodeValidate.Foreground = Brushes.DarkRed;
                            });
                        }
                    }).Start();
            }
            else
            {
                lblPairCodeValidate.Text = "Enter a pair code";
                lblPairCodeValidate.Foreground = Brushes.DarkRed;
            }
        }

        private void ConfigureBrowser_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            this.Try(() => System.Diagnostics.Process.Start("http://www.daveamenta.com/wp7"));
        }

        private void ConfigureTransferDestination_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Uploader.PresentOptions(() =>
            {
                FlashProgress(TaskbarProgressBarState.Normal);
            }, () =>
            {
                FlashProgress(TaskbarProgressBarState.Paused);
            }, new WindowInteropHelper(this).Handle);
        }
    }
}
