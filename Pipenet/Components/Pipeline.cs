using System;
using System.Collections.Generic;
using System.Text;
using Pipenet.Transport;

namespace Pipenet.Components
{
    public interface IEventPipline
    {
        void AddEvent(string name, Action<object[]> method);
        void AddEvent(string name, Func<object[], object> method);
        object Invoke(string name, object[] parameters);
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

        public void Connect()
        {
            switch (settings.transportType)
            {
                case PipelineSettings.ConnectionType.TCP: transport = new SocketTransport(settings.Ip, settings.Port, settings.IsListen);
                    break;
            }         
            transport.Run();
        }
        Dictionary<string, Action<object[]>> noReturnEventList = new Dictionary<string, Action<object[]>>();
        Dictionary<string, Func<object[], object>> returnEventList = new Dictionary<string, Func<object[], object>>();
        void IEventPipline.AddEvent(string name, Action<object[]> method)
        {
            noReturnEventList.Add(name, method);
        }

        void IEventPipline.AddEvent(string name, Func<object[], object> method)
        {
            returnEventList.Add(name, method);
        }

        object IEventPipline.Invoke(string name, object[] parameters)
        {
            throw new NotImplementedException();
        }
    }
}
