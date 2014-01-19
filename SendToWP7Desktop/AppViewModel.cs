using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DavuxLib2;
using System.ComponentModel;

namespace SendToWP7Desktop
{
    public class AppViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public SendToWP7API API { get; private set; }

        public string PairCode
        {
            get
            {
                return Settings.Get("PairCode", "");
            }
            set
            {
                if (value != PairCode)
                {
                    Settings.Set("PairCode", value);
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("PairCode"));
                }
            }
        }

        public bool UsePNG
        {
            get
            {
                return Settings.Get("Clipboard_PNG", true);
            }
            set
            {
                if (value != UsePNG)
                {
                    Settings.Set("Clipboard_PNG", value);
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("UsePNG"));
                        PropertyChanged(this, new PropertyChangedEventArgs("UseJPEG"));
                    }
                }
            }
        }

        public bool UseJPEG
        {
            get
            {
                return !UsePNG;
            }
            set
            {
                if (value != UseJPEG)
                {
                    UsePNG = !value;
                }
            }
        }

        public bool UseFixedIPAddress
        {
            get
            {
                return Settings.Get("UseFixedIPAddress", false);
            }
            set
            {
                if (value != UseFixedIPAddress)
                {
                    Settings.Set("UseFixedIPAddress", value);
                    if (PropertyChanged != null)
                    {
                        PropertyChanged(this, new PropertyChangedEventArgs("UseFixedIPAddress"));
                    }
                }
            }
        }


        public string FixedIPAddress
        {
            get
            {
                return Settings.Get("FixedIPAddress", "");
            }
            set
            {
                if (value != FixedIPAddress)
                {
                    Settings.Set("FixedIPAddress", value);
                    if (PropertyChanged != null)
                        PropertyChanged(this, new PropertyChangedEventArgs("FixedIPAddress"));
                }
            }
        }

        public AppViewModel()
        {
            API = new SendToWP7API();

            API.RequestFinished += ret =>
                {

                };
            API.RequestInProgress += () =>
                {

                };
        }
    }
}
