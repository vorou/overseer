using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using log4net;

namespace Overseer.Common
{
    public class FtpClient
    {
        private readonly Uri ftp;
        private readonly ILog log = LogManager.GetLogger(typeof (FtpClient));

        public FtpClient(Uri ftp)
        {
            this.ftp = ftp;
        }

        public byte[] Download(Uri uri)
        {
            var request = new WebClient {Credentials = new NetworkCredential("free", "free")};
            byte[] newFileData;
            try
            {
                newFileData = GetFileCore(request, uri);
            }
            catch (WebException)
            {
                log.ErrorFormat("failed to download file {0}", uri);
                return null;
            }
            return newFileData;
        }

        protected virtual byte[] GetFileCore(WebClient request, Uri uri)
        {
            return request.DownloadData(uri);
        }

        public IEnumerable<Uri> ListDirectory(string path)
        {
            var baseUri = new Uri(ftp, path);
            var ftpRequest = (FtpWebRequest) WebRequest.Create(baseUri);
            ftpRequest.Credentials = new NetworkCredential("free", "free");
            ftpRequest.Method = WebRequestMethods.Ftp.ListDirectory;
            FtpWebResponse response;
            try
            {
                response = (FtpWebResponse) ftpRequest.GetResponse();
            }
            catch (WebException)
            {
                log.WarnFormat("failed to list directory {0}", path);
                yield break;
            }

            using (var streamReader = new StreamReader(response.GetResponseStream()))
                while (true)
                {
                    var line = streamReader.ReadLine();
                    if (line == null)
                        break;
                    yield return new Uri(baseUri, line);
                }
        }
    }
}