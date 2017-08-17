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
            server.Run();
            while (!server.IsListenning) ;
            client.Run();
            Console.ReadLine();
        }
    }
}
