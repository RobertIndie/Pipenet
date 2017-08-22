using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using Pipenet.Components;

namespace Pipenet.Transport
{
    /// <summary>
    /// 传输接口
    /// </summary>
    public interface ITransport:IConnectState
    { 
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
        void AsynSendAndGet(IPacket packet, SocketTransport.receiveDelegate onReceive);
        /// <summary>
        /// 发送包
        /// </summary>
        /// <param name="packet"></param>
        void Send(IPacket packet);
        /// <summary>
        /// 关闭连接
        /// </summary>
        void Disconnect();
        void Run();
        string Ip
        {
            get;
        }
        int Port
        {
            get;
        }
    }
    public interface IConnectState
    {
        /// <summary>
        /// 如果启用侦听则此项一直为false
        /// </summary>
        bool IsConnected
        {
            get;
        }
        bool IsListen
        {
            get;
        }
        bool IsListenning
        {
            get;
        }
    }
    /// <summary>
    /// 传输类
    /// 连接前需要添加包事件receiveEventList。
    /// 继承这个类需要重写Connect方法。Connect方法里需要实现socket的连接，socket连接成功后需要开新线程调用SocketReceive。
    /// SocketReceive会将接收的包全部放进packetPool包缓冲池中
    /// 外部线程则调用UpdateReceive处理包缓冲池。
    /// </summary>
    public class SocketTransport : ITransport
    {
        Pipeline pipeline;
        SocketTransport parent;
        bool isSubTransport = false;
        public bool IsListen
        {
            get;private set;
        }
        /// <summary>
        /// 状态，是否正在侦听，等待客户端的连接。只有启用侦听才有效
        /// </summary>
        public bool IsListenning
        {
            get;private set;
        }
        /// <summary>
        /// 判断传输是否连接，一般在Connect()设置
        /// </summary>
        public bool IsConnected
        {
            get; private set;
        }
        public string Ip
        {
            get;private set;
        }
        public int Port
        {
            get;private set;
        }
        public bool MultiSocket
        {
            get;private set;
        }
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
        protected Dictionary<int, receiveDelegate> receiveEventList = new Dictionary<int, receiveDelegate>();
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
        public SocketTransport(Pipeline pipeline,string ip,int port,bool isListen,bool multiSocket)
        {
            this.pipeline = pipeline;
            IsConnected = false;
            Ip = ip;
            Port = port;
            IsListen = isListen;
            MultiSocket = multiSocket;
            SetEvent();
        }
        /// <summary>
        /// 创建子传输所用的构造函数
        /// </summary>
        /// <param name="pipeline"></param>
        /// <param name="socket"></param>
        /// <param name="receiveEventList"></param>
        /// <param name="parent"></param>
        internal SocketTransport(SocketTransport parent, Pipeline pipeline, Socket socket, Dictionary<int, receiveDelegate> receiveEventList)
        {
            this.parent = parent;
            this.pipeline = pipeline;
            this.socket = socket;
            string[] address = socket.RemoteEndPoint.ToString().Split(':');
            Ip = address[0];
            Port = int.Parse(address[1]);
            IsListen = false;
            MultiSocket = false;
            this.receiveEventList = receiveEventList;
            isSubTransport = true;
            receiveThread = new Thread(new ThreadStart(SocketReceive));
            receiveThread.Name = "SUB_ON_SERVER";
            receiveThread.Start();
            IsConnected = true;
        }
        void SetEvent()
        {
            receiveEventList = new Dictionary<int, receiveDelegate>()
            {
                { PacketType.EVENT_INVOKE,InvokeEvent},
                { PacketType.REFLECT_INVOKE,InvokeMethod}
            };
        }
        /// <summary>
        /// 开始运行，由主线程调用
        /// </summary>
        public void Run()
        {
            thread = new Thread(new ThreadStart(Connect));
            thread.Name = IsListen ? "SERVER" : "CLIENT";
            thread.Start();
        }
        /// <summary>
        /// 与服务端连接。异步
        /// </summary>
        protected void Connect()
        {
            IPEndPoint endPoint = new IPEndPoint(IPAddress.Parse(Ip), Port);
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            if (IsListen)
            {
                socket.Bind(endPoint);
                socket.Listen(0);
                IsListenning = true;
                

                if (MultiSocket)
                {
                    while (true)
                    {
                        Socket subSocket = socket.Accept();
                        CreateSubTransport(subSocket);
                    }
                }
                else
                {
                    clientSocket = socket.Accept();
                }
            }
            else
            {
                socket.Connect(endPoint);
            }
            if (!MultiSocket)
            {
                //开启接收线程
                receiveThread = new Thread(new ThreadStart(SocketReceive));
                receiveThread.Name = IsListen ? "SERVER_RECEIVE" : "CLIENT_RECEIVE";
                receiveThread.Start();
                IsConnected = true;
                pipeline.invokeOnConnect(this);
            }
        }

        void CreateSubTransport(Socket socket)
        {
            SocketTransport subTransport = new SocketTransport(this, pipeline, socket, receiveEventList);
            pipeline._subTransportPool.Add(subTransport);
            pipeline.invokeSubTransportConnect(subTransport);
        }

        const int HEAD_STREAM_SIZE = sizeof(int)/*包的长度*/;
        /// <summary>
        /// Socket层面上接收包，接收到包放进包缓冲池
        /// </summary>
        protected void SocketReceive()
        {
            try
            {
                while (true)
                {

                    //socket.ReceiveTimeout = 3000;
                    HeadStream headStream = ReceiveHeadStream(HEAD_STREAM_SIZE);//头部有256字节的信息，提取有用部分到headStream
                    int packetLength = BitConverter.ToInt32(headStream.Read(sizeof(int)), 0);

                    headStream.Close();
                    Packet packet = Receive(packetLength);
                    packet.transport = this;//给包贴上接收者的标签
                    if (packet == null)
                        return;
                    packetPool.Add(packet);
                    UpdateReceive();
                }

            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        public void UpdateReceive()
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


#region 处理Socket传输
        /// <summary>
        /// 前导码
        /// 加在包头部用以识别是否是正确的包
        /// </summary>
        static readonly byte[] PREAMBLE = new byte[8]
        {
            170,
            170,
            170,
            170,
            170,
            170,
            170,
            171
        };
        static readonly int PREAMBLE_LENGTH = PREAMBLE.Length;
        /// <summary>
        /// 从服务器接收信息，返回null为接收失败
        /// </summary>
        /// <returns></returns>
        Packet Receive(int dataSize)
        {
            Socket receiveSocket = IsListen ? clientSocket : socket;
            if (receiveTimeout != 0)
                receiveSocket.ReceiveTimeout = receiveTimeout;
            try
            {
                List<byte> data = new List<byte>();
                byte[] _tempBuffer = null;//临时缓存数据，当需要重新接收数据时存储上一次接收的可用数据
                int _tempBufferSize = 0;
                bool isDone = false;
                do
                {
                    data = new List<byte>();
                    int _size = 0;//已传输的包数据大小
                    byte[] rawData = new byte[dataSize + PREAMBLE_LENGTH - _tempBufferSize];
                    int rawSzie = receiveSocket.Receive(rawData);//尝试传输TCP包数据
                    if (_tempBuffer != null)//补充上一次接收失败的可用数据
                    {
                        rawData = Util.JoinByteArray(_tempBuffer, rawData);
                        rawSzie += _tempBufferSize;
                    }
                    if (rawSzie != (dataSize + PREAMBLE_LENGTH))//传输数据错误
                    {
                        if(rawSzie>PREAMBLE_LENGTH)//前导码传输完成
                        {
                            List<byte[]> extractedData = ExtractRawData(rawData,rawSzie);
                            if (Util.ByteArrayEquals(extractedData[0], PREAMBLE))//前导码正确
                            {
                                _size += extractedData[1].Length;
                                data.AddRange(extractedData[1]);
                                #region 补充数据
                                while (_size != dataSize)
                                {
                                    byte[] contentData = new byte[dataSize - _size];
                                    int receiveContentTempSize = receiveSocket.Receive(contentData);
                                    _size += receiveContentTempSize;
                                    AddData(data, contentData, receiveContentTempSize);
                                }
                                isDone = true;
                                #endregion
                            }
                            else
                            {
                                _tempBuffer = null;
                                _tempBufferSize = 0;
                                isDone = false;
                            }
                        }
                        else
                        {
                            _tempBuffer = new byte[rawSzie];
                            Array.Copy(rawData, 0, _tempBuffer, 0, rawSzie);
                            _tempBufferSize = rawSzie;
                            isDone = false;
                        }
                    }
                    else
                    {
                        List<byte[]> extractedData = ExtractRawData(rawData,rawSzie);
                        if (Util.ByteArrayEquals(extractedData[0], PREAMBLE))//前导码正确
                        {
                            AddData(data, extractedData[1], dataSize);
                            isDone = true;
                        }
                        else
                        {
                            isDone = false;
                        }
                    }
                }
                while (!isDone);
                return Packet.GetPacket(data.ToArray());
            }
            catch (SocketException)
            {
                Disconnect();
                return null;
            }
        }
        List<byte[]> ExtractRawData(byte[] data,int rawSize)
        {
            if (data.Length < PREAMBLE_LENGTH) throw new SystemException("Extract data failed");
            List<byte[]> result = new List<byte[]>();
            byte[] preamble = new byte[PREAMBLE_LENGTH];
            Array.Copy(data, 0, preamble, 0, PREAMBLE_LENGTH);
            result.Add(preamble);
            byte[] content = new byte[rawSize - PREAMBLE_LENGTH];
            Array.Copy(data, PREAMBLE_LENGTH, content, 0, rawSize - PREAMBLE_LENGTH);
            result.Add(content);
            return result;
        }
        void AddData(List<byte> data,byte[] sourceData,int length)
        {
            byte[] temp = new byte[length];
            Array.Copy(sourceData, 0, temp, 0, length);
            data.AddRange(temp);
        }
        HeadStream ReceiveHeadStream(int size)
        {
            Socket receiveSocket = IsListen ? clientSocket : socket;
            if (receiveTimeout != 0)
                receiveSocket.ReceiveTimeout = receiveTimeout;
            try
            {
                byte[] data = new byte[size];
                if (receiveSocket.Receive(data) == 0)
                {
                    Disconnect();
                    return null;
                }
                byte[] buffer = new byte[256 - size];
                if (receiveSocket.Receive(buffer) == 0)//释放其他数据
                {
                    Disconnect();
                    return null;
                }
                return new HeadStream(data);
            }
            catch (SocketException)
            {
                Disconnect();
                return null;
            }
        }
#endregion 

        public void Send(IPacket packet)
        {
            try
            {
                byte[] data = packet.GetData();
                HeadStream headStream = new HeadStream();
                headStream.Write(BitConverter.GetBytes(data.Length));
                headStream.Close();
                //Console.WriteLine("准备发送的包长度：" + data.Length);
                List<byte> sendData = new List<byte>();
                sendData.AddRange(PREAMBLE);
                sendData.AddRange(data);
                if (IsListen && !MultiSocket)
                {
                    clientSocket.Send(headStream.GetBuffer());
                    clientSocket.Send(sendData.ToArray());
                }
                else
                {
                    socket.Send(headStream.GetBuffer());
                    socket.Send(sendData.ToArray());
                }
            }
            catch (Exception)
            {
                Disconnect();
            }
        }

        /// <summary>
        /// 设置接收包事件
        /// </summary>
        /// <param name="receiveEventList"></param>
        public void SetReceiveEventList(Dictionary<int, receiveDelegate> receiveEventList)
        {
            this.receiveEventList = receiveEventList;
        }
        bool isClosed = false;
        public void Disconnect()
        {
            if (isClosed) return;
            if(isSubTransport)
                pipeline.invokeSubTransportDisconnect(this);
            if (socket == null) return;
            socket.Disconnect(true);
            socket.Close();
            socket = null;
            isClosed = true;
            //receiveThread.Abort();
        }
        int packetID = 0;
        public void AsynSendAndGet(IPacket packet, receiveDelegate onReceive)
        {
            packet.SetID(packetID++);
            receiveRequestPool.Add(packet.GetID(), onReceive);
            Send(packet);
        }

        #region 包事件
        public static void InvokeEvent(Packet packet)
        {
            ((EventPipeline)((SocketTransport)packet.transport).pipeline).InvokeEvent(packet.transport, (EventInvokePacket)packet);
        }
        public static void InvokeMethod(Packet packet)
        {
            ((ReflectPipeline)((SocketTransport)packet.transport).pipeline).InvokeMethod(packet.transport, (ReflectInvokePacket)packet);
        }
        #endregion
    }
}