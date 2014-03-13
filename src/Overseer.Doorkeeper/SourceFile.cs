namespace Overseer.Doorkeeper
{
    public class SourceFile
    {
        public string Uri { get; set; }
        public string Content { get; set; }

        public override string ToString()
        {
            return Uri;
        }
    }
}