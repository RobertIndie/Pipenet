using Pipenet.Transport;
using System;
using System.Collections.Generic;
using System.Text;
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
    public interface IEventPipeline : IConnectState, IAddEvent, IConnectEvent
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
        object Invoke(string name, object[] parameters, bool isReturn = false);
    }
    public interface IEventMultiTransport : IAddEvent, IConnectEvent
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
    public interface IConnectEvent
    {
        event Action<ITransport> onConnect;
    }
    public class EventPipeline : Pipeline, IEventPipeline, IEventMultiTransport
    {

        public EventPipeline(PipelineSettings settings) : base(settings)
        {

        }

        public EventPipeline() : base()
        {

        }

        public EventPipeline(bool isListen) : base(isListen)
        {

        }

        object Invoke(ITransport transport, string name, object[] parameters, bool isReturn)
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
        #region IEventPipline
        Dictionary<string, Action<ITransport, object[]>> noReturnEventList = new Dictionary<string, Action<ITransport, object[]>>();
        Dictionary<string, Func<ITransport, object[], object>> returnEventList = new Dictionary<string, Func<ITransport, object[], object>>();
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

        void IAddEvent.AddReturnEvent(string name, Func<ITransport, object[], object> method)
        {
            if (noReturnEventList.ContainsKey(name)) throw new ArgumentException("Name exist");
            returnEventList.Add(name, method);
        }

        object IEventPipeline.Invoke(string name, object[] parameters, bool isReturn = false) => Invoke(transport, name, parameters, isReturn);

        internal void InvokeEvent(ITransport transport, EventInvokePacket packet)
        {
            if (packet.state == EventInvokePacket.State.Invoke)
            {
                if (noReturnEventList.ContainsKey(packet.eventName))
                {
                    noReturnEventList[packet.eventName](transport, packet.parameters);
                    return;
                }
                if (returnEventList.ContainsKey(packet.eventName))
                {
                    object returnValue = returnEventList[packet.eventName](transport, packet.parameters);
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
            else if (packet.state == EventInvokePacket.State.Return)
            {
                returnValuePacketPool.Add(packet.randomID, packet);
                waitingResultThreads[packet.randomID].Interrupt();
                waitingResultThreads.Remove(packet.randomID);
            }
        }

        #endregion
        #region IMultiTransport

        void IEventMultiTransport.Invoke(ITransport subTransport, string name, object[] parameters, bool isReturn) => Invoke(subTransport, name, parameters, isReturn);
        #endregion
    }
}
