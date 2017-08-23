using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;

namespace SSGB
{
    class Utils
    {
        const string UA = "Mozilla/5.0 (Windows NT 6.1; rv:50.0) Gecko/20100101 Firefox/50.0";


        public static string SendPost(string req, string url, string refer, CookieContainer cookie)
        {

            var requestData = Encoding.UTF8.GetBytes(req);
            string content = string.Empty;

            try
            {
                var request = (HttpWebRequest)
                    WebRequest.Create(url);


                request.Method = "POST";

                //New
                request.Proxy = null;
                request.Timeout = 10000;
                request.ReadWriteTimeout = 10000;

                request.UserAgent = UA;

                request.AutomaticDecompression = DecompressionMethods.GZip |DecompressionMethods.Deflate;
                request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
                request.ContentType = "application/x-www-form-urlencoded";

                request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                request.Headers.Add("Accept-Language", "en-US,en;q=0.8,en-US;q=0.5,en;q=0.3");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");
                request.CookieContainer = cookie;

                request.ContentLength = requestData.Length;

                using (var s = request.GetRequestStream())
                {
                    s.Write(requestData, 0, requestData.Length);
                }

                HttpWebResponse resp = (HttpWebResponse)request.GetResponse();

                var stream = new StreamReader(resp.GetResponseStream());
                content = stream.ReadToEnd();

                cookie = request.CookieContainer;
                resp.Close();
                stream.Close();
            }
            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {
                    WebResponse resp = e.Response;
                    using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                    {
                        content = sr.ReadToEnd();
                    }
                }

            }

            return content;
        }



        public static string SendGet(string url, CookieContainer cookie)
        {
            string content = string.Empty;

            try
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
                request.Method = "GET";

                //New
                request.Proxy = null;
                request.Timeout = 10000;
                request.ReadWriteTimeout = 10000;


                request.UserAgent = UA;

                request.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
                request.Accept = "text/javascript, text/html, application/xml, text/xml, */*";
                request.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";

                request.Headers.Add("Accept-Encoding", "gzip, deflate, br");
                request.Headers.Add("Accept-Language", "en-US,en;q=0.8,en-US;q=0.5,en;q=0.3");
                request.Headers.Add("X-Requested-With", "XMLHttpRequest");

                request.CookieContainer = cookie;

                HttpWebResponse response = (HttpWebResponse)request.GetResponse();
                var stream = new StreamReader(response.GetResponseStream());
                content = stream.ReadToEnd();

                response.Close();
                stream.Close();

            }

            catch (WebException e)
            {
                if (e.Status == WebExceptionStatus.ProtocolError)
                {

                    HttpWebResponse resp = (HttpWebResponse)e.Response;
                    int statCode = (int)resp.StatusCode;

                    if (statCode == 403)
                    {
                        content = "403";
                    }
                    else
                    {
                        using (StreamReader sr = new StreamReader(resp.GetResponseStream()))
                        {
                            content = sr.ReadToEnd();
                        }
                    }
               }

            }

            return content;

        }

        public static void StartLoadImgTread(string imgUrl, PictureBox picbox)
        {
            if (imgUrl.Contains("http"))
            {
                ThreadStart threadStart = delegate() { loadImg(imgUrl, picbox, true); };
                Thread pTh = new Thread(threadStart);
                pTh.IsBackground = true;
                pTh.Start();
            }
        }

        private static void loadImg(string imgurl, PictureBox picbox, bool drawtext)
        {
            try
            {
                if (imgurl == string.Empty)
                    return;

                if (drawtext)
                {
                    picbox.Image = Properties.Resources.working;
                }

                WebClient wClient = new WebClient();
                byte[] imageByte = wClient.DownloadData(imgurl);
                using (MemoryStream ms = new MemoryStream(imageByte, 0, imageByte.Length))
                {
                    ms.Write(imageByte, 0, imageByte.Length);
                    var resimg = Image.FromStream(ms, true);
                    picbox.Image = resimg;
                }
            }
            catch (Exception)
            {
                //dummy
            }
        }

        public static byte[] HexStringToByteArray(string hex)
        {
            int hexLen = hex.Length;
            byte[] ret = new byte[hexLen / 2];
            for (int i = 0; i < hexLen; i += 2)
            {
                ret[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return ret;
        }

        public static long GetNoCacheTime()
        {
            return ((long)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds);
        }

        public static void SaveBinary(string p, object o)
        {
            try
            {
                if (o != null)
                {
                    using (Stream stream = File.Create(p))
                    {
                        BinaryFormatter bin = new BinaryFormatter();
                        bin.Serialize(stream, o);
                    }
                }
            }
            catch (Exception)
            {
                //dummy
            }
        }

        public static object LoadBinary(string p)
        {
            try
            {
                using (Stream stream = File.Open(p, FileMode.Open))
                {
                    BinaryFormatter bin = new BinaryFormatter();
                    var res = bin.Deserialize(stream);
                    return res;
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

    }
}
