using LocalNetworkPhotoSaverService.Applictations;
using LocalNetworkPhotoSaverService.FileTransfer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.Json;

public class ServerApplication: IApplication
{

    private int serverPort;
    private string saveDirectory;

    public ServerApplication(int serverPort, string saveDirectory)
    {
        this.serverPort = serverPort;
        this.saveDirectory = saveDirectory;
    }

    public void SyncFiles()
    {
        try
        {
            Console.WriteLine("Starting the server...");
            TcpListener listener = new TcpListener(IPAddress.Any, serverPort);
            listener.Start();

            Console.WriteLine($"Server is listening on port {serverPort}...");
            while (true)
            {
                using (TcpClient client = listener.AcceptTcpClient())
                using (NetworkStream networkStream = client.GetStream())
                {
                    var incomingPhotosPathsString = TCPOperations.ReadString(networkStream);

                    var incomingPhotosPaths = JsonSerializer.Deserialize<List<FileInfoDto>>(incomingPhotosPathsString);
                    var uniquePhotosPaths = FileOperations.GetUniquePhotos(incomingPhotosPaths, saveDirectory);
                    var uniquePhotosPathsString = JsonSerializer.Serialize(uniquePhotosPaths);
                    TCPOperations.WriteString(uniquePhotosPathsString, networkStream);

                    for (int i = 0; i < uniquePhotosPaths.Count; i++)
                    {
                        int fileNameLength = TCPOperations.ReadInt(networkStream);
                        Console.WriteLine($"fileNameLength {fileNameLength}");
                        string fileName = TCPOperations.ReadString(networkStream);
                        Console.WriteLine(fileName);
                        int fileLength = TCPOperations.ReadInt(networkStream);
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
                        string filePath = Path.Combine(saveDirectory, fileName);
                        Console.WriteLine($"{filePath}");
                        File.WriteAllBytes(filePath, fileBytes.ToArray());
                        File.SetCreationTime(filePath, DateTime.ParseExact(uniquePhotosPaths[i].CreatedAt, "yyyy-MM-ddTHH:mm:ss", System.Globalization.CultureInfo.InvariantCulture));
                        FileOperations.AddPhotoToSaved(uniquePhotosPaths[i]);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
        }
    }
}
