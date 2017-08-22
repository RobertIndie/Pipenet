using System;
using Pipenet.Transport;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using Pipenet.Components;
using System.Reflection;

namespace Test
{
    class Program
    {
        static PipelineSettings serverSettings = new PipelineSettings()
        {
            Ip = "0.0.0.0",
            Port = 8078,
            IsListen = true,
            IsMultiConnect = false,
            transportType = PipelineSettings.ConnectionType.TCP
        };
        static PipelineSettings clientSettings = new PipelineSettings()
        {
            Ip = "127.000.000.001",
            Port = 8078
        };
        static void Main(string[] args)
        {
            IReflectPipeline server = new ReflectPipeline(serverSettings);
            server.Connect();
            server.invokedClass = typeof(Program);
            while (!server.IsListenning) ;
            IReflectPipeline client = new ReflectPipeline(clientSettings);
            client.Connect();
            new Random().NextBytes(data);
            client.Invoke("Output", "Hello world");
            Console.ReadLine();
            Environment.Exit(0);
        }
        public static void Output(string message)
        {
            Console.WriteLine(message);
        }
    }
}
