using System;
using System.Globalization;
using System.Net;
using EasyNetQ;
using Overseer.Common;
using Overseer.Common.Messages;

namespace Overseer.Checker
{
    public class Program
    {
        private static readonly IBus Bus = BusFactory.CreateBus();

        private static void Main(string[] args)
        {
            Bus.Subscribe<TenderNumberWasSeen>("panda", CheckNumber);
        }

        private static void CheckNumber(TenderNumberWasSeen tenderNumberWasSeen)
        {
            var uri = new Uri(string.Format("https://zakupki.kontur.ru/Notification44?Id={0}", tenderNumberWasSeen.Number));
            var request = (HttpWebRequest) WebRequest.Create(uri);
            request.Timeout = 2000;
            request.Method = "HEAD";
            string result;
            try
            {
                using (var response = (HttpWebResponse) request.GetResponse())
                    result = ((int) response.StatusCode).ToString(CultureInfo.InvariantCulture);
            }
            catch (WebException e)
            {
                result = e.Message;
            }
            Bus.Publish(new TenderWasChecked {Number = tenderNumberWasSeen.Number, Uri = uri, Result = result});
        }
    }
}