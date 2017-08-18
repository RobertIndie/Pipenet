using System;
using Pipenet.Transport;
using System.Threading;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Transport client = new Transport("127.000.000.001",8078,false);
            Transport server = new Transport("127.000.000.001", 8078, true);
            server.SetReceiveEventList(
                    new System.Collections.Generic.Dictionary<int, Transport.receiveDelegate>()
                    {
                        { 1,OutputTestPacket}
                    }
                );
            server.Run();
            while (!server.IsListenning) ;
            client.Run();
            TestPacket tp = new TestPacket();
            tp.message = "Hello world";
            client.Send(tp);
            Console.ReadLine();
            Environment.Exit(0);
        }
        static void OutputTestPacket(Packet packet)
        {
            TestPacket tp = (TestPacket)packet;
            Console.WriteLine(tp.message);
        }
    }
}
