using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LocalNetworkPhotoSaverService.FileTransfer
{
    internal class FileOperations
    {
        private static ConcurrentDictionary<string,int> savedPhotos;
        private static string[] imageExtensions = { ".jpg", ".jpeg", ".png" };
        public static List<FileInfoDto> GetFolderContents(string path)
        {
            List<FileInfoDto> fileDtos = new List<FileInfoDto>();
            string[] fileEntries = Directory.GetFiles(path)
                .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()))
                .ToArray();
            foreach (string fileName in fileEntries)
            {
                FileInfo fileInfo = new FileInfo(fileName);
                fileDtos.Add(new FileInfoDto { Path = fileName, CreatedAt = fileInfo.CreationTime.ToString("yyyy-MM-ddTHH:mm:ss")});
            }

            return fileDtos;
        }

        public static List<FileInfoDto> GetUniquePhotos(List<FileInfoDto> incomingPhotos, string saveDirectory)
        {
            initSavedPhotosDatabase(saveDirectory);
            var foundItems = new ConcurrentBag<FileInfoDto>();


            Parallel.ForEach(incomingPhotos, item =>
            {
                if(!savedPhotos.ContainsKey(item.CreatedAt + Path.GetFileName(item.Path)))
                {
                    foundItems.Add(item);
                }
            });

            return new List<FileInfoDto>(foundItems);
        }


        private static void initSavedPhotosDatabase(string saveDirectory) {
            if (savedPhotos == null)
            {
                savedPhotos = new ConcurrentDictionary<string, int>();

                var savedPhotod = GetFolderContents(saveDirectory);
                Console.WriteLine("All saved photos collected and going to be moved into ConcurrentDictionary");
                Parallel.ForEach(savedPhotod, item =>
                {
                    Console.WriteLine("Adding: " + item.CreatedAt + Path.GetFileName(item.Path));
                    savedPhotos.TryAdd(item.CreatedAt + Path.GetFileName(item.Path), 0);
                });
            }
        }

        public static void AddPhotoToSaved(FileInfoDto fileInfo)
        {
            Console.WriteLine("Adding: " + fileInfo.CreatedAt + Path.GetFileName(fileInfo.Path));
            savedPhotos.TryAdd(fileInfo.CreatedAt + Path.GetFileName(fileInfo.Path), 0);
        }

    }
}
