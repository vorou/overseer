using System;
using System.Net;

namespace Overseer.Doorkeeper.Tests
{
    internal class FtpClientTestable : FtpClient
    {
        public FtpClientTestable(Uri ftp)
            : base(ftp)
        {
        }

        public Func<WebClient, Uri, byte[]> GetFileCoreBody { private get; set; }

        protected override byte[] GetFileCore(WebClient request, Uri uri)
        {
            return GetFileCoreBody(request, uri);
        }
    }
}