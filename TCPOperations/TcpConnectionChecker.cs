using System;
using System.Net;
using System.Net.Sockets;
using System.Threading.Tasks;

public class TcpConnectionChecker
{
    private readonly int serverPort;
    private readonly string localIpAddress;

    public TcpConnectionChecker(int port, string localIp)
    {
        serverPort = port;
        localIpAddress = localIp;
    }

    public async Task<string> GetFirstOpenTcpConnectionAsync()
    {
        var localIp = IPAddress.Parse(localIpAddress);
        var subnet = localIp.GetAddressBytes();

        // Loop through all the addresses in the subnet (last byte of the IP)
        for (int i = 1; i < 255; i++) // Range 1-254 for last byte
        {
            if (subnet[3] == i) continue; // Skip own IP address

            subnet[3] = (byte)i;
            var ipAddress = new IPAddress(subnet);
            if (await TryConnectAsync(ipAddress.ToString()))
            {
                return ipAddress.ToString(); // Return the first successful connection IP
            }
        }

        return null; // Return null if no connection is found
    }

    private async Task<bool> TryConnectAsync(string ipAddress)
    {
        try
        {
            using (var client = new TcpClient())
            {
                await client.ConnectAsync(ipAddress, serverPort);
                return true;
            }
        }
        catch
        {
            return false;
        }
    }
}