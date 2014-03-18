using System;

namespace Overseer.Doorkeeper
{
    public class Raw
    {
        public Raw(Uri zipUri, string entryName, string content) : this(content)
        {
            Uri = new Uri(zipUri + "/" + entryName);
        }

        private Raw(string content)
        {
            Content = content;
        }

        public Uri Uri { get; private set; }
        public string Content { get; private set; }

        public override string ToString()
        {
            return Uri.ToString();
        }
    }
}