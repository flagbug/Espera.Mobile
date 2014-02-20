using Newtonsoft.Json.Linq;
using System;
using System.IO;
using System.IO.Compression;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Espera.Android.Network
{
    public static class NetworkHelpers
    {
        public static async Task<byte[]> CompressDataAsync(byte[] data)
        {
            using (var targetStream = new MemoryStream())
            {
                using (var stream = new GZipStream(targetStream, CompressionMode.Compress))
                {
                    await stream.WriteAsync(data, 0, data.Length);
                }

                return targetStream.ToArray();
            }
        }

        public static async Task<byte[]> DecompressDataAsync(byte[] buffer)
        {
            using (var sourceStream = new MemoryStream(buffer))
            {
                using (var stream = new GZipStream(sourceStream, CompressionMode.Decompress))
                {
                    using (var memoryStream = new MemoryStream())
                    {
                        await stream.CopyToAsync(memoryStream);
                        return memoryStream.ToArray();
                    }
                }
            }
        }

        /// <summary>
        /// Reads the next message for the Espera protocol from the TCP client.
        /// </summary>
        /// <returns>The uncompressed, deserialized message in JSON, or null, if the underlying client has closed the connection.</returns>
        public static async Task<JObject> ReadNextMessageAsync(this TcpClient client)
        {
            byte[] messageLength = await client.ReadAsync(4);

            if (messageLength.Length == 0)
            {
                return null;
            }

            int realMessageLength = BitConverter.ToInt32(messageLength, 0);

            byte[] messageContent = await client.ReadAsync(realMessageLength);

            if (messageLength.Length == 0)
            {
                return null;
            }

            byte[] decompressed = await DecompressDataAsync(messageContent);
            string decoded = Encoding.UTF8.GetString(decompressed);

            JObject jsonMessage = JObject.Parse(decoded);

            return jsonMessage;
        }

        private static async Task<byte[]> ReadAsync(this TcpClient client, int length)
        {
            if (length <= 0)
                throw new ArgumentOutOfRangeException("length", "Length must be greater than 0");

            int count = 0;
            var buffer = new byte[length];

            do
            {
                int read = await client.GetStream().ReadAsync(buffer, count, length - count);
                count += read;

                // The client has closed the connection
                if (read == 0)
                    return new byte[0];
            }
            while (count < length);

            return buffer;
        }
    }
}