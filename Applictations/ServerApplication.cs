using LocalNetworkPhotoSaverService.Applictations;
using LocalNetworkPhotoSaverService.FileTransfer;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

public class ServerApplication: IApplication
{

    public int serverPort { get; }
    private string saveDirectory;
    private CommonHelper helper;

    public ServerApplication(int serverPort, string saveDirectory, CommonHelper helper)
    {
        this.serverPort = serverPort;
        this.saveDirectory = saveDirectory;
        this.helper = helper;
    }

    public void SyncFiles()
    {
        try
        {
            TcpListener listener = new TcpListener(IPAddress.Any, serverPort);
            listener.Start();

            while (true)
            {
                using (TcpClient client = listener.AcceptTcpClient())
                using (NetworkStream networkStream = client.GetStream())
                {
                    var incomingFiles = helper.ReceiveFilesPathsToSend(networkStream);
                    var uniqueFiles = FileOperations.GetUniquePhotos(incomingFiles, saveDirectory);
                    helper.SendFilesInfo(networkStream, uniqueFiles);

                    ReceiveAndSaveAllFiles(networkStream, uniqueFiles);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }

    private void ReceiveAndSaveAllFiles(NetworkStream networkStream, List<FileInfoDto> uniqueFiles)
    {
        foreach (var file in uniqueFiles)
        {
            byte[] fileBytes = helper.ReceiveFileBytes(networkStream);
            FileOperations.SavePhotoToDirectory(fileBytes, file, saveDirectory);
        }
    }
}
