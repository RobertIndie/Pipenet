using System;
using System.Collections.Generic;
using System.Text;
using Pipenet.Transport;
using System.Threading;

namespace Pipenet.Components
{
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
    public abstract class Pipeline:IConnectState
    {
        protected PipelineSettings settings;
        protected ITransport transport;

        internal List<ITransport> _subTransportPool = new List<ITransport>();
        public List<ITransport> subTransportPool { get => _subTransportPool; }
        #region SocketTransport
        public event Action<ITransport> onSubTransportConnect;
        public event Action<ITransport> onSubTransportDisconnect;
        public event Action<ITransport> onConnect;

        internal void invokeSubTransportConnect(ITransport subTransport) => onSubTransportConnect(subTransport);
        internal void invokeSubTransportDisconnect(ITransport subTransport) => onSubTransportDisconnect(subTransport);
        internal void invokeOnConnect(ITransport transport) => onConnect(transport);
        #endregion
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
                case PipelineSettings.ConnectionType.TCP:
                    transport = new SocketTransport(this, settings.Ip, settings.Port, settings.IsListen, settings.IsMultiConnect);
                    break;
            }
            transport.Run();
        }
        #region IConnectState
        public bool IsConnected => transport.IsConnected;

        public bool IsListen => transport.IsListen;

        public bool IsListenning => transport.IsListenning;


        #endregion
    }
}
