using Pipenet.Transport;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Threading;

namespace Pipenet.Components
{
    public interface IReflectPipeline:IConnectEvent, IConnectState
    {
        void Connect();
        Type invokedClass
        {
            get;set;
        }
        object InvokeAndReturn(string methodName, params object[] parameters);
        void Invoke(string methodName, params object[] parameters);
    }
    public interface IReflectMultiTransport : IConnectEvent
    {
        event Action<ITransport> onSubTransportConnect;
        event Action<ITransport> onSubTransportDisconnect;
        List<ITransport> subTransportPool
        {
            get;
        }
        void Connect();
        Type invokedClass
        {
            get; set;
        }
        object InvokeAndReturn(ITransport transport,string methodName, params object[] parameters);
        void Invoke(ITransport transport, string methodName, params object[] parameters);
        bool IsListenning
        {
            get;
        }
    }
    public class ReflectPipeline:Pipeline,IReflectPipeline, IReflectMultiTransport
    {
        public Type invokedClass
        {
            get;set;
        }

        public ReflectPipeline(PipelineSettings settings) : base(settings)
        {

        }

        public ReflectPipeline() : base()
        {

        }

        public ReflectPipeline(bool isListen) : base(isListen)
        {

        }

        object Invoke(ITransport transport,string methodName,bool isReturn,params object[] parameters)
        {
            ReflectInvokePacket packet = new ReflectInvokePacket();
            packet.state = ReflectInvokePacket.State.Invoke;
            packet.methodName = methodName;
            packet.parameters = parameters;
            packet.randomID = isReturn ? new Random().Next():-1;
            transport.Send(packet);
            if (isReturn)
            {
                waitingResultThreads.Add(packet.randomID, Thread.CurrentThread);
                try
                {
                    Thread.Sleep(100);
                }
                catch (Exception)
                {
                    ReflectInvokePacket returnPacket = returnValuePacketPool[packet.randomID];
                    returnValuePacketPool.Remove(returnPacket.randomID);
                    return returnPacket.returnValue;
                }
            }
            return null;
        }

        internal void InvokeMethod(ITransport transport,ReflectInvokePacket packet)
        {
            if(packet.state == ReflectInvokePacket.State.Invoke)
            {
                try
                {
                    MethodInfo method = invokedClass.GetMethod(packet.methodName);
                    method.Invoke(null, packet.parameters);
                    return;
                }
                catch (Exception)
                {
                    SendError(transport,packet);
                }
            }
            else if(packet.state == ReflectInvokePacket.State.InvokeReturnMethod)
            {
                try
                {
                    MethodInfo method = invokedClass.GetMethod(packet.methodName);
                    object returnValue = method.Invoke(null, packet.parameters);
                    packet.state = ReflectInvokePacket.State.Return;
                    packet.parameters = null;
                    packet.returnValue = returnValue;
                    transport.Send(packet);
                    return;
                }
                catch (Exception)
                {
                    SendError(transport, packet);
                }
            }
        }

        void SendError(ITransport transport, ReflectInvokePacket packet)
        {
            packet.state = ReflectInvokePacket.State.NoMethodOrError;
            packet.parameters = null;
            transport.Send(packet);
        }

        /// <summary>
        /// 等待接收返回值的线程
        /// </summary>
        Dictionary<int, Thread> waitingResultThreads = new Dictionary<int, Thread>();
        /// <summary>
        /// /等待被接收的包
        /// </summary>
        Dictionary<int, ReflectInvokePacket> returnValuePacketPool = new Dictionary<int, ReflectInvokePacket>();

        object IReflectPipeline.InvokeAndReturn(string methodName, params object[] parameters)
            => Invoke(transport, methodName, true, parameters);

        void IReflectPipeline.Invoke(string methodName, params object[] parameters)
            => Invoke(transport, methodName, false, parameters);

        object IReflectMultiTransport.InvokeAndReturn(ITransport transport, string methodName, params object[] parameters)
            => Invoke(transport, methodName, true, parameters);

        void IReflectMultiTransport.Invoke(ITransport transport, string methodName, params object[] parameters)
            => Invoke(transport, methodName, false, parameters);
    }
}
