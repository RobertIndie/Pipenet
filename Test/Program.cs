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
        static void Main(string[] args)
        {
            IEventPipline server = new Pipeline(true);
            server.Connect();
            IEventPipline client = new Pipeline();
            while (!server.IsListenning) ;
            client.Connect();
            server.AddNoReturnEvent("OutputMessage", OutputMessage);
            client.Invoke("OutputMessage", new object[] { "Helloworld" },false);
            Console.ReadLine();
            Environment.Exit(0);
        }
        static void OutputMessage(object[] parameters)
        {
            Console.WriteLine((string)parameters[0]);
        }
    }
    [Serializable]
    class TestC
    {
        public object[] test = new object[2];
    }
}
