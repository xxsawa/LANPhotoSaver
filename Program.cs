using System;
using LocalNetworkPhotoSaverService.WifiMonitoring;
using LocalNetworkPhotoSaverService.Applictations;
namespace LocalNetworkPhotoSaverService
{
    internal class Program
    {
        private static string FolderDirectory = @"C:\Users\dsawa\Downloads";
        private static string SaveDirectory = @"C:\Users\dsawa\Pictures\Camera Roll";
        private static int ServerPort = 5000;
        private static IApplication Application;

        static void Main(string[] args)
        {
            //Client app
            if (args.Length == 1)
            {
                Application = new ClientApplication(ServerPort, FolderDirectory, new CommonHelper());
                Application.StartMessage();

                var eventListenerManager = new EventListenerManager(Application, FolderDirectory);
                eventListenerManager.AddWifiChangeEventListener();
                eventListenerManager.AddFolderChangeEventListener();
                Console.Read();
                return;
            }
            //Server app
            Application = new ServerApplication(ServerPort, SaveDirectory, new CommonHelper());
            Application.StartMessage();
            Application.SyncFiles();
        }
    }
}