using System;
using System.Collections.Generic;
using System.Text;
using Pipenet.Transport;
using System.Threading;

namespace Pipenet.Components
{
    public interface IAddEvent
    {
        /// <summary>
        /// 添加没有返回值的事件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="method"></param>
        void AddEvent(string name, Action<ITransport, object[]> method);
        /// <summary>
        /// 添加事件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="method"></param>
        void AddReturnEvent(string name, Func<ITransport, object[], object> method);
    }
    public interface IEventPipline:IConnectState,IAddEvent
    {
        /// <summary>
        /// 管道连接
        /// </summary>
        void Connect();
        /// <summary>
        /// 触发事件
        /// </summary>
        /// <param name="name"></param>
        /// <param name="parameters"></param>
        /// <param name="isReturn"></param>
        /// <returns></returns>
        object Invoke(string name, object[] parameters,bool isReturn = false);
    }
    public interface IMultiTransport:IAddEvent
    {
        event Action<ITransport> onSubTransportConnect;
        event Action<ITransport> onSubTransportDisconnect;
        List<ITransport> subTransportPool
        {
            get;
        }
        void Connect();
        void Invoke(ITransport transport, string name, object[] parameters, bool isReturn = false);
        bool IsListenning
        {
            get;
        }
    }
    public class PipelineSettings
    {
        public enum ConnectionType
        {
            TCP
        }
        public string Ip
        {
            get; set;
        }
        public int Port
        {
            get; set;
        }
        public bool IsListen
        {
            get;set;
        }
        public bool IsMultiConnect
        {
            get;set;
        }
        public ConnectionType transportType
        {
            get;set;
        }
        public PipelineSettings()
        {
            Ip = "127.000.000.001";
            Port = 8078;
            IsListen = false;
            transportType = ConnectionType.TCP;
            IsMultiConnect = false;
        }
    }
    public class Pipeline:IEventPipline,IMultiTransport
    {
        PipelineSettings settings;
        ITransport transport;
        internal List<ITransport> _subTransportPool = new List<ITransport>();
        public List<ITransport> subTransportPool
        {
            get
            {
                return _subTransportPool;
            }
        }
        public event Action<ITransport> onSubTransportConnect;
        public event Action<ITransport> onSubTransportDisconnect;
        internal void invokeSubTransportConnect(ITransport subTransport) => onSubTransportConnect(subTransport);
        internal void invokeSubTransportDisconnect(ITransport subTransport) => onSubTransportDisconnect(subTransport);
        public Pipeline(PipelineSettings settings)
        {
            this.settings = settings;
        }

        public Pipeline()
        {
            settings = new PipelineSettings();
        }

        public Pipeline(bool isListen)
        {
            settings = new PipelineSettings()
            {
                IsListen = isListen
            };
        }

        public void Connect()
        {
            switch (settings.transportType)
            {
                case PipelineSettings.ConnectionType.TCP: transport = new SocketTransport(this,settings.Ip, settings.Port, settings.IsListen,settings.IsMultiConnect);
                    break;
            }         
            transport.Run();
        }

        object Invoke(ITransport transport,string name, object[] parameters, bool isReturn)
        {
            EventInvokePacket packet = new EventInvokePacket();
            packet.state = EventInvokePacket.State.Invoke;
            packet.eventName = name;
            packet.parameters = parameters;
            packet.randomID = isReturn ? new Random().Next() : -1;
            transport.Send(packet);
            if (isReturn)
            {
                waitingResultThreads.Add(packet.randomID, Thread.CurrentThread);
                try
                {
                    //Thread.Sleep(Timeout.Infinite);
                    Thread.Sleep(100);
                }
                catch (Exception)
                {
                    EventInvokePacket returnPacket = returnValuePacketPool[packet.randomID];
                    returnValuePacketPool.Remove(returnPacket.randomID);
                    return returnPacket.returnValue;
                }
            }
            return null;
        }

        #region IConnectState
        public bool IsConnected => transport.IsConnected;

        public bool IsListen => transport.IsListen;

        public bool IsListenning => transport.IsListenning;


        #endregion
        #region IEventPipline
        Dictionary<string, Action<ITransport,object[]>> noReturnEventList = new Dictionary<string, Action<ITransport, object[]>>();
        Dictionary<string, Func<ITransport,object[], object>> returnEventList = new Dictionary<string, Func<ITransport, object[], object>>();
        /// <summary>
        /// 等待接收返回值的线程
        /// </summary>
        Dictionary<int, Thread> waitingResultThreads = new Dictionary<int, Thread>();
        /// <summary>
        /// /等待被接收的包
        /// </summary>
        Dictionary<int, EventInvokePacket> returnValuePacketPool = new Dictionary<int, EventInvokePacket>();

        void IAddEvent.AddEvent(string name, Action<ITransport, object[]> method)
        {
            if (returnEventList.ContainsKey(name)) throw new ArgumentException("Name exist");
            noReturnEventList.Add(name, method);
        }

        void IAddEvent.AddReturnEvent(string name, Func<ITransport,object[], object> method)
        {
            if(noReturnEventList.ContainsKey(name)) throw new ArgumentException("Name exist");
            returnEventList.Add(name, method);
        }

        object IEventPipline.Invoke(string name, object[] parameters, bool isReturn = false) => Invoke(transport, name, parameters, isReturn);

        internal void InvokeEvent(ITransport transport,EventInvokePacket packet)
        {
            if (packet.state == EventInvokePacket.State.Invoke)
            {
                if (noReturnEventList.ContainsKey(packet.eventName))
                {
                    noReturnEventList[packet.eventName](transport,packet.parameters);
                    return;
                }
                if (returnEventList.ContainsKey(packet.eventName))
                {
                    object returnValue = returnEventList[packet.eventName](transport,packet.parameters);
                    packet.state = EventInvokePacket.State.Return;
                    packet.parameters = null;
                    packet.returnValue = returnValue;
                    transport.Send(packet);
                    return;
                }
                packet.parameters = null;
                packet.state = EventInvokePacket.State.NoEvent;
                transport.Send(packet);
            }
            else if(packet.state == EventInvokePacket.State.Return)
            {
                returnValuePacketPool.Add(packet.randomID, packet);
                waitingResultThreads[packet.randomID].Interrupt();
                waitingResultThreads.Remove(packet.randomID);
            }
        }

        #endregion
        #region IMultiTransport

        void IMultiTransport.Invoke(ITransport subTransport, string name, object[] parameters, bool isReturn) => Invoke(subTransport, name, parameters, isReturn);
        #endregion
    }
}
