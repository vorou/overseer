using System;
using EasyNetQ;
using Overseer.Common;
using Overseer.Common.Messages;

namespace Overseer.CheckLog
{
    internal class Program
    {
        private static readonly IBus Bus = BusFactory.CreateBus();

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