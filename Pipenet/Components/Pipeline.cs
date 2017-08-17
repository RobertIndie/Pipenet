using System;
using System.Collections.Generic;
using System.Text;

namespace Pipenet.Components
{
    public class PipelineSettings
    {
        public enum ConnectionType
        {
            TCP
        }
        public string ip
        {
            get; set;
        }
        public int port
        {
            get; set;
        }
        public ConnectionType transportType
        {
            get;set;
        }
    }
    public class Pipeline
    {
        
    }
}
