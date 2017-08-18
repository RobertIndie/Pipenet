using System;
using System.IO;

namespace Pipenet.Transport
{
    public class HeadStream:MemoryStream
    {
        public HeadStream() : base()
        {

        }
        public HeadStream(byte[] buffer) : base(buffer)
        {
            
        }
        public void Write(byte[] buffer) => Write(buffer, 0, buffer.Length);
           
        public byte[] Read(int size)
        {
            byte[] buffer = new byte[size];
            int _size = Read(buffer, 0, size);
            if (_size != size) throw new SystemException("Read failed");
            return buffer;
        }
    }
}
