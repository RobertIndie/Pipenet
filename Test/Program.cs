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
        static IEventPipeline client;
        static ITransport test;
        static void Main(string[] args)
        {
            PipelineSettings settings = new PipelineSettings()
            {
                Ip = "0.0.0.0",
                IsListen = true,
                IsMultiConnect = true
            };
            IMultiTransport server = new Pipeline(settings);
            server.onSubTransportConnect += (sub) => { Console.WriteLine("成功");
                test = sub;
            };
            server.Connect();
            IEventPipeline client = new Pipeline();
            client.AddEvent("FUCK", OutputMessage);
            while (!server.IsListenning) ;
            client.Connect();
            while (test==null) ;
            server.Invoke(test, "FUCK", new object[] { "HELLO WORLD" });
            Console.ReadLine();
            Environment.Exit(0);
        }
        static void OutputMessage(ITransport transport,object[] parameters)
        {
            Console.WriteLine((string)parameters[0]);
        }
        static object OutputWithReturn(ITransport transport,object[] parameters)
        {
            Console.WriteLine((string)parameters[0]);
            return "Helloworld TOO";
        }
    }
}
