using LocalNetworkPhotoSaverService.FileTransfer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.ServiceProcess;
using System.Text;
using System.Text.Json;
using LocalNetworkPhotoSaverService.WifiMonitoring;
using System.Threading;
using System.Linq;

namespace LocalNetworkPhotoSaverService
{
    internal class Program
    {
        private static string folderDirectory = @"C:\Users\dsawa\Downloads";
        private static string saveDirectory = @"C:\Users\dsawa\Pictures\Camera Roll";
        private static int serverPort = 5000;
        private static Application application;

        static async void Main(string[] args)
        {
            if (args.Length == 1)
            {
                Console.WriteLine("Running interactively. Press Ctrl+C to exit.");

                Console.WriteLine("Client application started!");


                var service = new WifiService();
                application = new ClinetApplication(serverPort, folderDirectory);
                service.StartService(application);
                FileSystemWatcher watcher = new FileSystemWatcher(folderDirectory);

                watcher.EnableRaisingEvents = true;
                watcher.Changed += Watcher_Changed;

                Console.Read();
                service.StopService();
                return;
            }

            Console.WriteLine("Server application started!");
            application = new ServerApplication(serverPort, saveDirectory);
            application.Start();
        }

        private static void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (WifiService.ConnectedToRightWifi && !ClinetApplication.Started)
            {
                application.Start();
            }
        }
    }
}

public class ClinetApplication: Application
{
    private int serverPort;
    private string serverIp; // IP of the server  192.168.1.37
    private string folderPath;
    public static bool Started { get; private set; }

    public ClinetApplication(int serverPort, string saveDirectory)
    {
        this.serverPort = serverPort;
        this.folderPath = saveDirectory;
    }


    public void Start()
    {
        var foldersPaths = FileOperations.GetFolderContents(folderPath);

        while (WifiService.ConnectedToRightWifi && Started)
        {
            try
            {
                using (TcpClient client = new TcpClient(serverIp, serverPort))
                using (NetworkStream networkStream = client.GetStream())
                {
                    var incomingPhotosString = JsonSerializer.Serialize(foldersPaths);
                    TCPOperations.WriteString(incomingPhotosString, networkStream);
                    var uniquePhotosPathsString = TCPOperations.ReadString(networkStream);
                    Console.WriteLine($"Files '{uniquePhotosPathsString}' received !");
                    var uniquePhotosPaths = JsonSerializer.Deserialize<List<FileInfoDto>>(uniquePhotosPathsString);

                    foreach (var item in uniquePhotosPaths)
                    {
                        byte[] fileBytes = File.ReadAllBytes(item.Path);
                        string fileName = Path.GetFileName(item.Path);
                        byte[] fileNameBytes = Encoding.UTF8.GetBytes(fileName);

                        TCPOperations.WriteInt(fileNameBytes.Length, networkStream);
                        Console.WriteLine("file name size in bytes: " + fileNameBytes.Length);
                        TCPOperations.WriteString(fileName, networkStream);
                        Console.WriteLine(fileName);
                        TCPOperations.WriteInt(fileBytes.Length, networkStream);
                        Console.WriteLine("file size in bytes: " + fileBytes.Length);

                        //Chunking 
                        for (int fileChunk = 0; fileChunk <= (fileBytes.Length / UInt16.MaxValue); fileChunk++)
                        {
                            var tempFileBytes = fileBytes.Skip(fileChunk * UInt16.MaxValue).Take(UInt16.MaxValue).ToArray();
                            Console.WriteLine($"Sending {tempFileBytes.Length} bytes");
                            networkStream.Write(tempFileBytes, 0, tempFileBytes.Length);
                        }
                    }
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
}

public class ServerApplication: Application
{

    private int serverPort;
    private string saveDirectory;

    public ServerApplication(int serverPort, string saveDirectory)
    {
        this.serverPort = serverPort;
        this.saveDirectory = saveDirectory;
    }

    public void Start()
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

public interface Application
{
    void Start();
}