using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace Pipenet.Transport
{
    /// <summary>
    /// 传输接口
    /// </summary>
    public interface ITransport
    {
        bool IsConnected
        {
            get; set;
        }
        /// <summary>
        /// 更新处理接收。
        /// 由外部线程频繁调用，处理接收的包。
        /// </summary>
        void UpdateReceive();
        /// <summary>
        /// 异步发送并且获得包，接收到包后将调用onReceive委托。
        /// </summary>
        /// <param name="packet">发送的包</param>
        /// <param name="onReceive">接收到包后触发的委托</param>
        void AsynSendAndGet(IPacket packet, Transport.receiveDelegate onReceive);
        /// <summary>
        /// 发送包
        /// </summary>
        /// <param name="packet"></param>
        void Send(IPacket packet);
        /// <summary>
        /// 关闭连接
        /// </summary>
        void Disconnect();
    }
    /// <summary>
    /// 传输类
    /// 连接前需要添加包事件receiveEventList。
    /// 继承这个类需要重写Connect方法。Connect方法里需要实现socket的连接，socket连接成功后需要开新线程调用SocketReceive。
    /// SocketReceive会将接收的包全部放进packetPool包缓冲池中
    /// 外部线程则调用UpdateReceive处理包缓冲池。
    /// </summary>
    public class Transport : ITransport
    {
        public bool IsListen
        {
            get;set;
        }
        /// <summary>
        /// 判断传输是否连接，一般在Connect()设置
        /// </summary>
        public bool IsConnected
        {
            get; set;
        }
        public string ip;
        public int port;
        /// <summary>
        /// 传输所使用的socket
        /// </summary>
        Socket socket;
        /// <summary>
        /// 如果启用侦听，则启用这个Socket
        /// </summary>
        Socket clientSocket;
        /// <summary>
        /// 一次接收允许的延迟，超出则自动断开连接。
        /// </summary>
        public int receiveTimeout = 0;
        /// <summary>
        /// 连接所使用的线程
        /// </summary>
        Thread thread;
        /// <summary>
        /// 接收数据线程
        /// </summary>
        public Thread receiveThread;
        public delegate void receiveDelegate(Packet packet);
        public event receiveDelegate onReceive;
        /// <summary>
        /// 接收对应类型的包则调用对应的委托。
        /// </summary>
        protected Dictionary<PacketType, receiveDelegate> receiveEventList = new Dictionary<PacketType, receiveDelegate>();
        /// <summary>
        /// 存放请求返回Packet的委托池。
        /// 客户端通过AsynSendAndGet并在这里等待，翘首等待以望相同ID的Packet之归来。但它的另一半从服务器回来时，
        /// 那么就调用对应的委托。
        /// </summary>
        Dictionary<int, receiveDelegate> receiveRequestPool = new Dictionary<int, receiveDelegate>();
        /// <summary>
        /// 由socket接收到包存放于此。
        /// </summary>
        List<Packet> packetPool = new List<Packet>();
        /// <summary>
        /// 开始运行，由主线程调用
        /// </summary>
        public void Run()
        {
            thread = new Thread(new ThreadStart(Connect));
            thread.Start();
        }
        /// <summary>
        /// 与服务端连接。异步
        /// </summary>
        protected void Connect()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(ip), port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (IsListen)
            {
                socket.Bind(endPoint);
                socket.Listen(0);
                clientSocket = socket.Accept();
            }
            else
            {
                socket.Connect(endPoint);
            }
            //开启接收线程
            receiveThread = new Thread(new ThreadStart(SocketReceive));
            receiveThread.Start();
            IsConnected = true;
        }

        /// <summary>
        /// Socket层面上接收包，接收到包放进包缓冲池
        /// </summary>
        protected void SocketReceive()
        {
            while (true)
            {
                //socket.ReceiveTimeout = 3000;
                Packet packet = Receive(1024);
                packet.transport = this;//给包贴上接收者的标签
                if (packet == null)
                    return;
                packetPool.Add(packet);
            }
        }

        void ITransport.UpdateReceive()
        {
            foreach (Packet packet in packetPool)
            {
                if (receiveRequestPool.ContainsKey(packet.Id))
                {
                    receiveRequestPool[packet.Id](packet);
                    receiveRequestPool.Remove(packet.Id);//处理完包后就将请求移除，注意：处理包时的线程与此线程相同。
                }
                if (receiveEventList.ContainsKey(packet.type))
                {
                    receiveEventList[packet.type](packet);
                }
            }
            packetPool.Clear();
        }

        /// <summary>
        /// 从服务器接收信息，返回null为接收失败
        /// </summary>
        /// <returns></returns>
        Packet Receive(int dataSize)
        {
            if (receiveTimeout != 0)
                socket.ReceiveTimeout = receiveTimeout;
            try
            {
                byte[] data = new byte[dataSize];
                if (socket.Receive(data) == 0)
                {
                    Disconnect();
                    return null;
                }
                return Packet.GetPacket(data);
            }
            catch (SocketException)
            {
                Disconnect();
                return null;
            }
        }

        /// <summary>
        /// 设置接收包事件
        /// </summary>
        /// <param name="receiveEventList"></param>
        public void SetReceiveEventList(Dictionary<PacketType, receiveDelegate> receiveEventList)
        {
            this.receiveEventList = receiveEventList;
        }

        void Disconnect()
        {
            if (socket == null) return;
            socket.Disconnect(true);
            socket.Close();
            socket = null;
            receiveThread.Abort();
        }

        void ITransport.Disconnect()
        {
            Disconnect();
        }
        int packetID = 0;
        void ITransport.AsynSendAndGet(IPacket packet, receiveDelegate onReceive)
        {
            packet.SetID(packetID++);
            receiveRequestPool.Add(packet.GetID(), onReceive);
            socket.Send(packet.GetData());
        }

        void ITransport.Send(IPacket packet)
        {
            socket.Send(packet.GetData());
        }
    }
}