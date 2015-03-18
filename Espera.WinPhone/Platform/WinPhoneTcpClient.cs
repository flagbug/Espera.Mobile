using Espera.Mobile.Core.Network;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Networking;
using Windows.Networking.Sockets;
using Windows.Storage.Streams;

namespace Espera.WinPhone.Platform
{
    internal class WinPhoneTcpClient : ITcpClient
    {
        private readonly StreamSocket socket;

        public WinPhoneTcpClient()
        {
            this.socket = new StreamSocket();
            this.socket.Control.NoDelay = true;
        }

        public async Task ConnectAsync(string ipAddress, int port)
        {
            try
            {
                await this.socket.ConnectAsync(new HostName(ipAddress), port.ToString());
            }

            catch (Exception ex)
            {
                throw new NetworkException("Failed to connect to the Espera server", ex);
            }
        }

        public void Dispose()
        {
            this.socket.Dispose();
        }

        public Stream GetStream()
        {
            return new DualStream(this.socket.InputStream, this.socket.OutputStream);
        }

        private class DualStream : Stream
        {
            private readonly IInputStream inputstream;
            private readonly IOutputStream outputStream;

            public DualStream(IInputStream inputStream, IOutputStream outputStream)
            {
                this.inputstream = inputStream;
                this.outputStream = outputStream;
            }

            public override bool CanRead
            {
                get { return true; }
            }

            public override bool CanSeek
            {
                get { return false; }
            }

            public override bool CanWrite
            {
                get { return true; }
            }

            public override long Length
            {
                get { throw new NotImplementedException(); }
            }

            public override long Position { get; set; }

            public override void Flush()
            {
                this.outputStream.AsStreamForWrite().Flush();
            }

            public override int Read(byte[] buffer, int offset, int count)
            {
                return this.inputstream.AsStreamForRead().Read(buffer, offset, count);
            }

            public override long Seek(long offset, SeekOrigin origin)
            {
                throw new NotImplementedException();
            }

            public override void SetLength(long value)
            {
                throw new NotImplementedException();
            }

            public override void Write(byte[] buffer, int offset, int count)
            {
                this.outputStream.AsStreamForWrite().Write(buffer, offset, count);
                this.Flush();
            }
        }
    }
}