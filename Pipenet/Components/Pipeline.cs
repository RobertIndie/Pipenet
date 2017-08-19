using System;
using System.Collections.Generic;
using System.Text;
using Pipenet.Transport;

namespace Pipenet.Components
{
    public interface IEventPipline:IConnectState
    {
        bool IsConnected
        {
            get;
        }
        void Connect();
        void AddNoReturnEvent(string name, Action<object[]> method);
        void AddEvent(string name, Func<object[], object> method);
        object Invoke(string name, object[] parameters,bool isReturn);
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
        public ConnectionType transportType
        {
            get;set;
        }
    }
    public class Pipeline:IEventPipline
    {
        PipelineSettings settings;
        ITransport transport;
        public Pipeline(PipelineSettings settings)
        {
            this.settings = settings;
        }

        public Pipeline()
        {
            settings = new PipelineSettings()
            {
                Ip = "127.000.000.001",
                Port = 8078,
                IsListen = false,
                transportType = PipelineSettings.ConnectionType.TCP
            };
        }

        public Pipeline(bool isListen)
        {
            settings = new PipelineSettings()
            {
                Ip = "127.000.000.001",
                Port = 8078,
                IsListen = isListen,
                transportType = PipelineSettings.ConnectionType.TCP
            };
        }

        public void Connect()
        {
            switch (settings.transportType)
            {
                case PipelineSettings.ConnectionType.TCP: transport = new SocketTransport(this,settings.Ip, settings.Port, settings.IsListen);
                    break;
            }         
            transport.Run();
        }
        #region IConnectState
        public bool IsConnected => transport.IsConnected;

        public bool IsListen => transport.IsListen;

        public bool IsListenning => transport.IsListenning;
        #endregion
        #region IEventPipline
        Dictionary<string, Action<object[]>> noReturnEventList = new Dictionary<string, Action<object[]>>();
        Dictionary<string, Func<object[], object>> returnEventList = new Dictionary<string, Func<object[], object>>();

        void IEventPipline.AddNoReturnEvent(string name, Action<object[]> method)
        {
            if (returnEventList.ContainsKey(name)) throw new ArgumentException("Name exist");
            noReturnEventList.Add(name, method);
        }

        void IEventPipline.AddEvent(string name, Func<object[], object> method)
        {
            if(noReturnEventList.ContainsKey(name)) throw new ArgumentException("Name exist");
            returnEventList.Add(name, method);
        }

        object IEventPipline.Invoke(string name, object[] parameters,bool isReturn)
        {
            EventInvokePacket packet = new EventInvokePacket();
            packet.state = EventInvokePacket.State.Invoke;
            packet.eventName = name;
            packet.parameters = parameters;
            transport.Send(packet);
            if (!isReturn)
                return null;
            return null;
        }

        internal void InvokeEvent(EventInvokePacket packet)
        {
            if (noReturnEventList.ContainsKey(packet.eventName))
            {
                noReturnEventList[packet.eventName](packet.parameters);
                return;
            }
            if (returnEventList.ContainsKey(packet.eventName))
            {
                object returnValue = returnEventList[packet.eventName](packet.parameters);
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
        #endregion
    }
}
