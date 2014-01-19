using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Diagnostics;
using Newtonsoft.Json;

namespace SendToWP7Desktop
{
    class Localhostr
    {
        public class PostData
        {
            // Change this if you need to, not necessary
            public static string boundary = "----------20534987AaB03x";

            public List<PostDataParam> Params { get; set; }

            public PostData()
            {
                Params = new List<PostDataParam>();
            }

            private static string MimeFromRegistry(string ext)
            {
                // TODO security issue with passing ext into registry?
                string mimeType = "application/octet-stream";
                try
                {
                    Microsoft.Win32.RegistryKey regKey = Microsoft.Win32.Registry.ClassesRoot.OpenSubKey(ext);
                    if (regKey != null && regKey.GetValue("Content Type") != null)
                    {
                        mimeType = regKey.GetValue("Content Type").ToString();
                    }
                }
                catch
                {

                }
                return mimeType;
            }

            private byte[] concat(byte[] front, byte[] back)
            {
                byte[] combined = new byte[front.Length + back.Length];
                Array.Copy(front, combined, front.Length);
                Array.Copy(back, 0, combined, front.Length, back.Length);
                return combined;
            }

            /// <summary>
            /// Returns the parameters array formatted for multi-part/form data
            /// </summary>
            /// <returns></returns>
            public byte[] GetPostData()
            {
                StringBuilder sb = new StringBuilder();
                byte[] ret = null;
                foreach (PostDataParam p in Params)
                {
                    sb.AppendLine("--" + boundary);

                    if (p.Type == PostDataParamType.File)
                    {
                        sb.AppendLine(string.Format("Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"", p.Name, p.FileName));
                        sb.AppendLine("Content-Type: " + MimeFromRegistry(Path.GetExtension(p.FileName)));
                        sb.AppendLine();
                        ret = Encoding.UTF8.GetBytes(sb.ToString());
                        Trace.WriteLine("Request: " + sb.ToString());
                        ret = concat(ret, p.Value);
                        //sb.AppendLine(p.Value);
                    }
                    else
                    {
                      //  sb.AppendLine(string.Format("Content-Disposition: form-data; name=\"{0}\"", p.Name));
                       // sb.AppendLine();
                       // sb.AppendLine(p.Value);
                    }
                }
                sb.Clear();
                sb.AppendLine();
                sb.AppendLine("--" + boundary + "--");
                ret = concat(ret, Encoding.UTF8.GetBytes(sb.ToString()));

                return ret; // sb.ToString();
            }
        }

        public enum PostDataParamType
        {
            Field,
            File
        }

        public class PostDataParam
        {
            public PostDataParam(string name, byte[] value, PostDataParamType type)
            {
                Name = name;
                Value = value;
                Type = type;
            }

            public PostDataParam(string name, string filename, byte[] value, PostDataParamType type)
            {
                Name = name;
                Value = value;
                FileName = filename;
                Type = type;
            }

            public string Name;
            public string FileName;
            public byte[] Value;
            public PostDataParamType Type;
        }

      

        public static UploadResult UploadFile(string filename)
        {
            Trace.WriteLine("Uploading " + filename + " to localhostr");
            System.Net.ServicePointManager.Expect100Continue = false;
            HttpWebRequest oRequest = null;
            oRequest = (HttpWebRequest)HttpWebRequest.Create("http://localhostr.com/api/json");
            oRequest.ContentType = "multipart/form-data; boundary=" + PostData.boundary;
            oRequest.Method = "POST";
            PostData pData = new PostData();
            Encoding encoding = Encoding.ASCII;
            Stream oStream = null;

            // oRequest.Headers.Add("User-Agent: localhostr uploadr v1.2.5p");

            pData.Params.Add(new PostDataParam("Filedata", Path.GetFileName(filename), File.ReadAllBytes(filename), PostDataParamType.File));

            /* ... set the parameters, read files, etc. IE:
               pData.Params.Add(new PostDataParam("email", "example@example.com", PostDataParamType.Field));
               pData.Params.Add(new PostDataParam("fileupload", "filename.txt", "filecontents" PostDataParamType.File));
            */

            byte[] buffer = pData.GetPostData();

            

            oRequest.ContentLength = buffer.Length;

            oStream = oRequest.GetRequestStream();
            oStream.Write(buffer, 0, buffer.Length);
            oStream.Close();

            HttpWebResponse oResponse = (HttpWebResponse)oRequest.GetResponse();


            using (var sw = new StreamReader(oResponse.GetResponseStream()))
            {
                var r = sw.ReadToEnd();
                Trace.WriteLine("Localhostr response: " + r);

                Dictionary<string, string> values = JsonConvert.DeserializeObject<Dictionary<string, string>>(r);

                return new UploadResult(values["icon"], values["path"]);
            }
        }
    }                       
}
