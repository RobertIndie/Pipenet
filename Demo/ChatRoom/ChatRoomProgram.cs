using System;
using Pipenet.Components;
using System.Collections.Generic;
using Pipenet.Transport;

namespace ChatRoom
{
    class ChatRoomProgram
    {
        static PipelineSettings serverSettings = new PipelineSettings()
        {
            Ip = "0.0.0.0",
            Port = 8078,
            IsListen = true,
            IsMultiConnect = true,
            transportType = PipelineSettings.ConnectionType.TCP
        };
        static PipelineSettings clientSettings = new PipelineSettings()
        {
            Ip = "127.000.000.001",
            Port = 8078
        };
        static IEventMultiTransport server;
        static IEventPipeline client;
        static Dictionary<ITransport, string> clientList = new Dictionary<ITransport, string>();
        static void Main(string[] args)
        {
            if (args.Length != 0 && args[0] == "-server")
            {
                Console.WriteLine("服务端开启");
                server = new EventPipeline(serverSettings);
                server.onSubTransportConnect += (sub) =>
                {
                    clientList.Add(sub, string.Format("{0}:{1}", sub.Ip, sub.Port));
                    Broadcast(string.Format("欢迎 {0}加入房间", clientList[sub]));
                };
                server.onSubTransportDisconnect += (sub) =>
                {
                    string name = clientList[sub];
                    clientList.Remove(sub);
                    Broadcast(string.Format("{0} 离开房间", name));
                };
                server.AddEvent("SetName", SetName);
                server.AddEvent("Send", SendMessage);
                server.Connect();
            }
            else
            {
                Console.WriteLine("输入你的名字:");
                string name = Console.ReadLine();
                client = new EventPipeline(clientSettings);
                client.AddEvent("Output", OutputMessage);
                client.onConnect += (transport) =>
                {
                    client.Invoke("SetName", new object[] { name });
                    while (true)
                    {
                        string mes = Console.ReadLine();
                        client.Invoke("Send", new object[] { mes });
                    }
                };
                client.Connect();
            }
        }
        static void Broadcast(string message)
        {
            Console.WriteLine(message);
            foreach(ITransport tp in clientList.Keys)
                server.Invoke(tp, "Output", new object[]{ message});
        }
        /// <summary>
        /// Server to Clients
        /// Client处理
        /// </summary>
        /// <param name="parameters"></param>
        static void OutputMessage(ITransport transport, object[] parameters) => Console.WriteLine((string)parameters[0]);
        /// <summary>
        /// Clients to Server
        /// Server处理
        /// </summary>
        /// <param name="parameters"></param>
        static void SetName(ITransport transport,object[] parameters)
        {
            clientList[transport] = (string)parameters[0];
            Broadcast(string.Format("{0}:{1} 将名字设置为 {2}", transport.Ip, transport.Port, clientList[transport]));
        }
        static void SendMessage(ITransport transport,object[] parameters)
        {
            Broadcast(string.Format("[{0}]{1}", clientList[transport], (string)parameters[0]));
        }
    }
}
