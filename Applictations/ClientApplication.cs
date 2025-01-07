using LocalNetworkPhotoSaverService.FileTransfer;
using LocalNetworkPhotoSaverService.WifiMonitoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;

namespace LocalNetworkPhotoSaverService.Applictations
{
    internal class ClientApplication : IApplication
    {
        private int serverPort;
        private string serverIp; // IP of the server  192.168.1.37
        private string folderPath;
        public static bool Started { get; private set; }

        public ClientApplication(int serverPort, string saveDirectory)
        {
            this.serverPort = serverPort;
            this.folderPath = saveDirectory;
        }


        public void SyncFiles()
        {
            Started = true;
            while (WifiService.ConnectedToRightWifi && Started)
            {
                try
                {
                    using (TcpClient client = new TcpClient(serverIp, serverPort))
                    using (NetworkStream networkStream = client.GetStream())
                    {
                        SendFilesInfo(networkStream);                                   // File names and creation dates in client directory
                        var uniquePhotosPaths = ReceiveFilesPathsToSend(networkStream); // File paths recieved from server that are not already in server save directory
                        SendAllFiles(networkStream, uniquePhotosPaths);
                        Started = false;

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                    if (WifiService.ConnectedToRightWifi)
                    {
                        Thread.Sleep(100);
                    }
                }
            }
        }

        private string GetFileNamesString()
        {
            var foldersPaths = FileOperations.GetFolderContents(folderPath);
            return JsonSerializer.Serialize(foldersPaths);
        }

        private void SendFilesInfo(NetworkStream networkStream)
        {
            var incomingPhotosString = GetFileNamesString();
            TCPOperations.WriteString(incomingPhotosString, networkStream);
        }

        private List<FileInfoDto> ReceiveFilesPathsToSend(NetworkStream networkStream)
        {
            var uniquePhotosPathsString = TCPOperations.ReadString(networkStream);
            Console.WriteLine($"Files '{uniquePhotosPathsString}' received !");
            return JsonSerializer.Deserialize<List<FileInfoDto>>(uniquePhotosPathsString);
        }

        private void SendAllFiles(NetworkStream networkStream,List<FileInfoDto> files)
        {
            foreach (var file in files)
            {
                byte[] fileBytes = File.ReadAllBytes(file.Path);
                string fileName = Path.GetFileName(file.Path);
                byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);

                SendFileNameSize(networkStream, fileNameBytes);
                SendFileName(networkStream, fileName);
                SendFileSize(networkStream, fileBytes);
                SendFile(networkStream, fileBytes);
            }
        }

        private void SendFileNameSize(NetworkStream networkStream, byte[] fileNameBytes)
        {
            TCPOperations.WriteInt(fileNameBytes.Length, networkStream);
            Console.WriteLine("file name size in bytes: " + fileNameBytes.Length);
        }

        private void SendFileName(NetworkStream networkStream, string fileName)
        {
            TCPOperations.WriteString(fileName, networkStream);
            Console.WriteLine(fileName);
        }

        private void SendFileSize(NetworkStream networkStream, byte[] fileBytes)
        {
            TCPOperations.WriteInt(fileBytes.Length, networkStream);
            Console.WriteLine("file size in bytes: " + fileBytes.Length);
        }

        private void SendFile(NetworkStream networkStream, byte[] fileBytes)
        {
            for (int fileChunk = 0; fileChunk <= (fileBytes.Length / UInt16.MaxValue); fileChunk++)
            {
                var tempFileBytes = fileBytes.Skip(fileChunk * UInt16.MaxValue).Take(UInt16.MaxValue).ToArray();
                Console.WriteLine($"Sending {tempFileBytes.Length} bytes");
                networkStream.Write(tempFileBytes, 0, tempFileBytes.Length);
            }
        }
    }
}
