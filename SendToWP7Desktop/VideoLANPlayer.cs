using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using Microsoft.Win32;

namespace SendToWP7Desktop
{
    public class VideoLANPlayer
    {
        public static bool IsInstalled
        {
            get
            {
                return ExePath != null;
            }
        }

        public static string ExePath
        {
            get
            {
                try
                {
                    RegistryKey regKey = Registry.LocalMachine.OpenSubKey(@"Software\Classes\Applications\vlc.exe\shell\Open\Command");
                    if (regKey != null)
                    {
                        var commandLine = regKey.GetValue("") as string;
                        // "C:\Program Files (x86)\VideoLAN\VLC\vlc.exe" --started-from-file "%1"
                        var m = Regex.Match(commandLine, "\"(.*?)\"(.*?)");
                        if (m.Success) return m.Groups[1].Value;
                    }

                    var guessPath1 = @"C:\Program Files (x86)\VideoLAN\VLC\vlc.exe";
                    var guessPath2 = @"C:\Program Files\VideoLAN\VLC\vlc.exe";

                    if (File.Exists(guessPath1)) return guessPath1;
                    if (File.Exists(guessPath2)) return guessPath2;
                }
                catch (Exception ex)
                {
                    Trace.WriteLine("Couldn't VLC/ExePath: " + ex);
                }
                return "";
            }
        }
    }
}
