using System;
using System.Net.Sockets;

namespace LocalNetworkPhotoSaverService.FileTransfer
{
    internal class TCPOperations
    {
        public static void WriteString(string inputData, NetworkStream networkStream)
        {
            byte[] data = System.Text.Encoding.UTF8.GetBytes(inputData);
            byte[] lengthBytes = BitConverter.GetBytes(data.Length);
            networkStream.Write(lengthBytes, 0, lengthBytes.Length);
            networkStream.Write(data, 0, data.Length);
        }

        public static void WriteInt(int inputData, NetworkStream networkStream)
        {
            byte[] intBytes = BitConverter.GetBytes(inputData);
            networkStream.Write(intBytes, 0, intBytes.Length);
        }

        public static string ReadString(NetworkStream networkStream)
        {
            byte[] lengthBytes = new byte[sizeof(int)];
            networkStream.Read(lengthBytes, 0, lengthBytes.Length);
            int dataLength = BitConverter.ToInt32(lengthBytes, 0);

            byte[] data = new byte[dataLength];
            networkStream.Read(data, 0, dataLength);
            return System.Text.Encoding.UTF8.GetString(data);
        }

        public static int ReadInt(NetworkStream networkStream)
        {
            byte[] intBytes = new byte[sizeof(int)];
            networkStream.Read(intBytes, 0, intBytes.Length);
            return BitConverter.ToInt32(intBytes, 0);
        }
    }
}
