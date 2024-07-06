using System;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Threading;

namespace Motio.UICommon.VersionChecking
{
    public class FileDownloader
    {
        private readonly string _url;
        private readonly string _fullPathWhereToSave;

        public event DownloadProgressChangedEventHandler DownloadProgress;
        public event Action DownloadCompleted;
        public event Action<Exception> Error;

        public FileDownloader(string url, string fullPathWhereToSave)
        {
            if (string.IsNullOrEmpty(url)) throw new ArgumentNullException("url");
            if (string.IsNullOrEmpty(fullPathWhereToSave)) throw new ArgumentNullException("fullPathWhereToSave");

            this._url = url;
            this._fullPathWhereToSave = fullPathWhereToSave;
        }

        public void StartDownload()
        {
            //Directory.CreateDirectory(Path.GetDirectoryName(_fullPathWhereToSave));

            if (File.Exists(_fullPathWhereToSave))
            {
                File.Delete(_fullPathWhereToSave);
            }

            using (WebClient client = new WebClient())
            {
                var ur = new Uri(_url);
                // client.Credentials = new NetworkCredential("username", "password");
                client.DownloadProgressChanged += WebClientDownloadProgressChanged;
                client.DownloadFileCompleted += WebClientDownloadCompleted;
                //Console.WriteLine(@"Downloading file:");
                client.DownloadFileAsync(ur, _fullPathWhereToSave);
            }
        }

        private void WebClientDownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DownloadProgress?.Invoke(sender, e);
            //Console.Write("\r     -->    {0}%.", e.ProgressPercentage);
        }

        private void WebClientDownloadCompleted(object sender, AsyncCompletedEventArgs args)
        {
            if (args.Cancelled)
            {
                Error?.Invoke(args.Error);
            }
            else
            {
                DownloadCompleted?.Invoke();
            }
        }
    }
}