using System;
using Pipenet.Transport;
using System.Threading;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using Pipenet.Components;
using System.Reflection;

namespace Test
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Start");
            Invoke("OutputMessage", "Helloworld");
            Console.ReadLine();
            Environment.Exit(0);
        }
        static void Invoke(string name,params object[] args)
        {
            Type type = typeof(Program);
            MethodInfo info = type.GetMethod(name);
            info.Invoke(null, args);
        }
        public static void OutputMessage(string message)
        {
            Console.WriteLine(message);
        }
    }
}
