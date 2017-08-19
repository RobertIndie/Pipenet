using System;
using Pipenet.Transport;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using Pipenet.Components;

namespace Test
{
    class Program
    {
        static IEventPipline client;
        static void Main(string[] args)
        {
            //while (true)
            //    Console.WriteLine(new Random().Next());
            IEventPipline server = new Pipeline(true);
            server.Connect();
            client = new Pipeline();
            while (!server.IsListenning) ;
            client.Connect();
            server.AddNoReturnEvent("OutputMessage", OutputMessage);
            server.AddEvent("OutputWithReturn", OutputWithReturn);
            client.Invoke("OutputMessage", new object[] { "Helloworld" },false);
            object value = client.Invoke("OutputWithReturn", new object[] { "Helloworld" }, true);
            Console.WriteLine("Return value:" + (string)value);
            Console.ReadLine();
            Environment.Exit(0);
        }
        static void OutputMessage(object[] parameters)
        {
            Console.WriteLine((string)parameters[0]);
        }
        static object OutputWithReturn(object[] parameters)
        {
            Console.WriteLine((string)parameters[0]);
            return "Helloworld TOO";
        }
    }
}
