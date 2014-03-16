using System;

namespace Overseer.Doorkeeper
{
    public class SourceFile
    {
        public Uri Uri { get; set; }
        public string Content { get; set; }

        public override string ToString()
        {
            return Uri.ToString();
        }
    }
}