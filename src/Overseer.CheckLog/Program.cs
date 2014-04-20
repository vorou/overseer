using System;
using EasyNetQ;
using Overseer.Common;

namespace Overseer.CheckLog
{
    internal class Program
    {
        private static readonly IBus Bus = RabbitHutch.CreateBus("host=localhost");

        private static void Main(string[] args)
        {
            Bus.Subscribe<TenderWasChecked>("panda", ShowCheckResult);
        }

        private static void ShowCheckResult(TenderWasChecked tenderWasChecked)
        {
            Console.Out.WriteLine(tenderWasChecked);
        }
    }
}