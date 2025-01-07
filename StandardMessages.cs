using LocalNetworkPhotoSaverService.Applictations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LocalNetworkPhotoSaverService
{
    public static class StandardMessages
    {
        public static void StartMessage(this IApplication application)
        {
            if (application is ClientApplication)
                Console.WriteLine("Client application started!");
            if (application is ServerApplication)
                Console.WriteLine("Server application started!");
        }
    }
}
