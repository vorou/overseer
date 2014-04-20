namespace Overseer.Common
{
    public class TenderWasChecked
    {
        public string Number { get; set; }
        public int HttpStatus { get; set; }

        public override string ToString()
        {
            return string.Format("Number: {0}, HttpStatus: {1}", Number, HttpStatus);
        }
    }
}