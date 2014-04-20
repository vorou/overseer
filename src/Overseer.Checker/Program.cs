using System.Net;
using EasyNetQ;
using Overseer.Common.Messages;

namespace Overseer.Checker
{
    public class Program
    {
        private static readonly IBus Bus = RabbitHutch.CreateBus("host=localhost");

        private static void Main(string[] args)
        {
            Bus.Subscribe<TenderNumberWasSeen>("panda", CheckNumber);
        }

        private static void CheckNumber(TenderNumberWasSeen tenderNumberWasSeen)
        {
            var request = WebRequest.Create(string.Format("https://zakupki.kontur.ru/Notification44?Id={0}", tenderNumberWasSeen.Number)) as HttpWebRequest;
            request.Method = "HEAD";
            int statusCode;
            try
            {
                var response = request.GetResponse() as HttpWebResponse;
                statusCode = (int) response.StatusCode;
            }
            catch (WebException e)
            {
                statusCode = (int) e.Status;
            }
            Bus.Publish(new TenderWasChecked {Number = tenderNumberWasSeen.Number, HttpStatus = statusCode});
        }
    }
}