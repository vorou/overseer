using System;

namespace Overseer.Common.Messages
{
    public class TenderWasChecked
    {
        public string Number { get; set; }
        public Uri Uri { get; set; }
        public string Result { get; set; }

        public override string ToString()
        {
            return string.Format("Number: {0}, Uri: {1}, Result: {2}", Number, Uri, Result);
        }
    }
}