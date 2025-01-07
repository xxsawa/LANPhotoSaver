using System;
using LocalNetworkPhotoSaverService.WifiMonitoring;
using LocalNetworkPhotoSaverService.Applictations;
namespace LocalNetworkPhotoSaverService
{
    internal class Program
    {
        private static string folderDirectory = @"C:\Users\dsawa\Downloads";
        private static string saveDirectory = @"C:\Users\dsawa\Pictures\Camera Roll";
        private static int serverPort = 5000;
        private static IApplication application;

        static void Main(string[] args)
        {
            //Client app
            if (args.Length == 1)
            {
                application = new ClientApplication(serverPort, folderDirectory);
                application.StartMessage();
                var wifiService = new WifiService(application);
                wifiService.StartService();
                var eventManager = new EventManager(application, folderDirectory);
                eventManager.AddFolderChangeEventListener();
                Console.Read();
                wifiService.StopService();
                return;
            }
            //Server app
            application = new ServerApplication(serverPort, saveDirectory);
            application.StartMessage();
            application.SyncFiles();
        }
    }
}