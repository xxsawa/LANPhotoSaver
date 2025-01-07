using LocalNetworkPhotoSaverService.FileTransfer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace LocalNetworkPhotoSaverService.Applictations
{
    public class CommonHelper
    {
        public void SendFilesInfo(NetworkStream networkStream, List<FileInfoDto> fileInfos)
        {
            var fileInfoString = JsonSerializer.Serialize(fileInfos);
            TCPByteOperations.WriteString(fileInfoString, networkStream);
        }

        public List<FileInfoDto> ReceiveFilesPathsToSend(NetworkStream networkStream)
        {
            var uniquePhotosPathsString = TCPByteOperations.ReadString(networkStream);
            Console.WriteLine($"Files '{uniquePhotosPathsString}' received !");
            return JsonSerializer.Deserialize<List<FileInfoDto>>(uniquePhotosPathsString);
        }

        public void SendFileSize(NetworkStream networkStream, byte[] fileBytes)
        {
            TCPByteOperations.WriteInt(fileBytes.Length, networkStream);
            Console.WriteLine("file size in bytes: " + fileBytes.Length);
        }

        public void SendFileBytes(NetworkStream networkStream, byte[] fileBytes)
        {
            for (int fileChunk = 0; fileChunk <= (fileBytes.Length / UInt16.MaxValue); fileChunk++)
            {
                var tempFileBytes = fileBytes.Skip(fileChunk * UInt16.MaxValue).Take(UInt16.MaxValue).ToArray();
                Console.WriteLine($"Sending {tempFileBytes.Length} bytes");
                networkStream.Write(tempFileBytes, 0, tempFileBytes.Length);
            }
        }

        public byte[] ReceiveFileBytes(NetworkStream networkStream)
        {
            int fileLength = TCPByteOperations.ReadInt(networkStream);
            Console.WriteLine($"fileLength {fileLength}");

            //Chunking 
            List<byte> fileBytes = new List<byte>();

            int chunkSize = fileLength / UInt16.MaxValue;
            Console.WriteLine($"Number of chunks {chunkSize} when subtracted from whoel {fileLength - chunkSize * UInt16.MaxValue}");
            for (int fileChunk = 0; fileChunk <= chunkSize; fileChunk++)
            {
                if (fileChunk == chunkSize)
                {
                    var _tempFileBytes = new byte[fileLength - chunkSize * UInt16.MaxValue];
                    networkStream.Read(_tempFileBytes, 0, _tempFileBytes.Length);
                    fileBytes.AddRange(_tempFileBytes);
                    break;
                }
                var tempFileBytes = new byte[UInt16.MaxValue];
                networkStream.Read(tempFileBytes, 0, tempFileBytes.Length);
                fileBytes.AddRange(tempFileBytes);
            }

            return fileBytes.ToArray();
        }
    }
}
