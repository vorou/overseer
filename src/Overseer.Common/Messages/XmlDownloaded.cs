using System;

namespace Overseer.Common.Messages
{
    public class XmlDownloaded
    {
        public Uri Uri { get; set; }
        public string Content { get; set; }

        public override string ToString()
        {
            return Uri.ToString();
        }
    }
}