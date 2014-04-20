using EasyNetQ;

namespace Overseer.Common
{
    public static class BusFactory
    {
        public static IBus CreateBus()
        {
            return RabbitHutch.CreateBus("host=localhost");
        }
    }
}