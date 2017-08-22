using Pipenet.Transport;
using System;
using System.Collections.Generic;
using System.Text;

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
    public interface IMultiTransport : IAddEvent, IConnectEvent
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
}
