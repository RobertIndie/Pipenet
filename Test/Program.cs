using System;
using Pipenet.Transport;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text;
using System.Runtime.Serialization;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Transport client = new Transport("27.41.213.139", 8078,false);
            Transport server = new Transport("0.0.0.0", 8078, true);
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
            byte[] a = new byte[] { 0, 1, 2, 3 };
            byte[] b = new byte[] { 0, 1, 2, 3 };
            Console.WriteLine(a.Equals(b));
            int i = 0;
            while (true)
            {
                i++;
                tp.message = "" + i;
                client.Send(tp);
                //Thread.Sleep(100);
            }
            client.Send(tp);
            Console.ReadLine();
            Environment.Exit(0);
        }
        static int temp = 1;
        static void OutputTestPacket(Packet packet)
        {
            TestPacket tp = (TestPacket)packet;
            //if (int.Parse(tp.message) - temp != 0) throw new Exception("传输数据错误");
            Console.WriteLine(tp.message + (temp==int.Parse(tp.message)?"":("   FUCK"+(int.Parse(tp.message)-temp))));
            temp++;
        }
    }
}
