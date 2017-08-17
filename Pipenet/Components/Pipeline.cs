using System;
using System.Collections.Generic;
using System.Text;

namespace Pipenet.Components
{
    public class PipelineSettings
    {
        public enum TransportType
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
        public TransportType transportType
        {
            get;set;
        }
    }
    public class Pipeline
    {
        
    }
}
