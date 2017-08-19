using System;
using System.Collections.Generic;
using System.Text;
using Pipenet.Transport;

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
        public ConnectionType transportType
        {
            get;set;
        }
    }
    public class Pipeline
    {
        PipelineSettings settings;
        SocketTransport transport;
        public Pipeline(PipelineSettings settings)
        {
            this.settings = settings;
        }

        public void Connect()
        {

        }
    }
}
