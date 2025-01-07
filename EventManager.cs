using LocalNetworkPhotoSaverService.Applictations;
using LocalNetworkPhotoSaverService.WifiMonitoring;
using System.IO;
namespace LocalNetworkPhotoSaverService
{
    internal class EventManager
    {
        private readonly IApplication application;
        private readonly string folderDirectory;
        public EventManager(IApplication clinetApplication, string folderDirectory)
        {
            this.application = clinetApplication;
            this.folderDirectory = folderDirectory;
        }

        public void AddFolderChangeEventListener()
        {
            FileSystemWatcher watcher = new FileSystemWatcher(folderDirectory);

            watcher.EnableRaisingEvents = true;
            watcher.Changed += Watcher_Changed;
        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (WifiService.ConnectedToRightWifi && !ClientApplication.Started)
            {
                this.application.SyncFiles();
            }
        }
    }
}
