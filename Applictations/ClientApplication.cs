using LocalNetworkPhotoSaverService.FileTransfer;
using LocalNetworkPhotoSaverService.WifiMonitoring;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;

namespace LocalNetworkPhotoSaverService.Applictations
{
    internal class ClientApplication : IApplication
    {
        private int serverPort;
        private string serverIp;
        private string folderPath;
        private CommonHelper helper;

        public static bool Started { get; private set; }

        public ClientApplication(int serverPort, string saveDirectory, CommonHelper helper)
        {
            this.serverPort = serverPort;
            this.folderPath = saveDirectory;
            this.helper = helper;
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
                        var incomingPhotosString = FileOperations.GetFolderContents(folderPath);
                        helper.SendFilesInfo(networkStream, incomingPhotosString);             // File names and creation dates in client directory
                        var uniquePhotosPaths = helper.ReceiveFilesPathsToSend(networkStream); // File paths recieved from server that are not already in server save directory
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

        private void SendAllFiles(NetworkStream networkStream, List<FileInfoDto> files)
        {
            foreach (var file in files)
            {
                SendFile(networkStream, file);
            }
        }

        private void SendFile(NetworkStream networkStream, FileInfoDto fileInfo)
        {
            byte[] fileBytes = File.ReadAllBytes(fileInfo.Path);
            helper.SendFileSize(networkStream, fileBytes);
            helper.SendFileBytes(networkStream, fileBytes);
        }
    }
}
