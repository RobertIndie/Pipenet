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
            PipelineSettings settings = new PipelineSettings()
            {
                IsListen = true,
                IsMultiConnect = true
            };
            IMultiTransport server = new Pipeline(settings);
            server.onSubTransportConnect += (sub) => { Console.WriteLine("成功"); };
            server.Connect();
            IEventPipline client = new Pipeline();
            while (!server.IsListenning) ;
            client.Connect();
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
