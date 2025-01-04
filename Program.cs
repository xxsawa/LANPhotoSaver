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

namespace LocalNetworkPhotoSaverService
{
    internal class Program
    {
        private static string folderDirectory = @"C:\Users\dsawa\Downloads";
        private static string saveDirectory = @"C:\Users\dsawa\Pictures\Camera Roll";
        private static int serverPort = 5000;
        private static Application application;

        static void Main(string[] args)
        {
            if (args.Length == 1)
            {
                Console.WriteLine("Running interactively. Press Ctrl+C to exit.");

                Console.WriteLine("Client application started!");
                application = new ClinetApplication(serverPort, folderDirectory);
                var service = new WifiService(application);
                service.StartService();
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
    private string serverIp = "127.0.0.1"; // IP of the server  192.168.1.16
    private string folderPath;
    public static bool Started { get; private set; }

    public ClinetApplication(int serverPort, string saveDirectory)
    {
        this.serverPort = serverPort;
        this.folderPath = saveDirectory;
    }


    public void Start()
    {

        Started = true;
        var foldersPaths = FileOperations.GetFolderContents(folderPath);
        while (WifiService.ConnectedToRightWifi)
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
                        TCPOperations.WriteString(fileName, networkStream);
                        TCPOperations.WriteInt(fileBytes.Length, networkStream);
                        networkStream.Write(fileBytes, 0, fileBytes.Length);

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
                if (WifiService.ConnectedToRightWifi)
                {
                    Thread.Sleep(600000);
                }
            }
        }
        Started = false;
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
                        if (fileNameLength == 0) break;
                        string fileName = TCPOperations.ReadString(networkStream);

                        int fileLength = TCPOperations.ReadInt(networkStream);
                        byte[] fileBytes = new byte[fileLength];
                        networkStream.Read(fileBytes, 0, fileLength);
                        string filePath = Path.Combine(saveDirectory, fileName);
                        File.WriteAllBytes(filePath, fileBytes);
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