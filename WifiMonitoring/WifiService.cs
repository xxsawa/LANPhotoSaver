using System;
using System.Net.NetworkInformation;
using System.ServiceProcess;
using System.Diagnostics;

namespace LocalNetworkPhotoSaverService.WifiMonitoring
{
    public class WifiService : ServiceBase
    {
        private const string TargetWifiName = "ZTE-5USG79"; // Replace with your Wi-Fi SSID
        private Application Application;
        public static bool ConnectedToRightWifi { get; set; }

        public WifiService(Application application)
        {
            Application = application;
            ServiceName = "WifiMonitoringService";
        }

        public void StartService()
        {
            OnStart(null);
        }

        public void StopService()
        {
            OnStop();
        }

        protected override void OnStart(string[] args)
        {
            try
            {
                SetCurrentWifiSSID();
                NetworkChange.NetworkAddressChanged += NetworkAddressChanged;
                Console.WriteLine("Service started.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on service start: {ex.Message}");
            }
        }

        protected override void OnStop()
        {
            try
            {
                NetworkChange.NetworkAddressChanged -= NetworkAddressChanged;
                Console.WriteLine("Service stopped.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error on service stop: {ex.Message}");
            }
        }

        private void NetworkAddressChanged(object sender, EventArgs e)
        {
            SetCurrentWifiSSID();
        }

        private void SetCurrentWifiSSID()
        {
            try
            {
                var currentNetwork = GetCurrentWifiSSID();
                if (currentNetwork != null && currentNetwork.Contains(TargetWifiName))
                {
                    Console.WriteLine("Connected to home Wifi!");
                    ConnectedToRightWifi = true;
                    Application.Start();
                }
                else
                {
                    ConnectedToRightWifi = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in network address change handler: {ex.Message}");
            }
        }

        private string GetCurrentWifiSSID()
        {
            try
            {
                // Execute the netsh command to get current Wi-Fi details
                using (var process = new Process())
                {
                    process.StartInfo.FileName = "netsh";
                    process.StartInfo.Arguments = "wlan show interfaces";
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.CreateNoWindow = true;
                    process.Start();

                    string output = process.StandardOutput.ReadToEnd();
                    process.WaitForExit();

                    // Parse the SSID from the output
                    string ssid = null;
                    foreach (var line in output.Split('\n'))
                    {
                        if (line.Contains("SSID"))
                        {
                            ssid = line.Split(':')[1].Trim();
                            break;
                        }
                    }
                    return ssid;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting SSID: {ex.Message}");
            }
            return null;
        }
    }
}
