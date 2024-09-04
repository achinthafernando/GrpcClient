// See https://aka.ms/new-console-template for more information

using Grpc.Net.Client;
using GrpcClient;
using System.Net;
using System.Net.Sockets;
using System.Text;



// Create a UDP client with broadcasting enabled
using (UdpClient udpClient = new UdpClient { EnableBroadcast = true })
{
    // Prepare the request data
    byte[] requestData = Encoding.ASCII.GetBytes("Request");
    int udpPort = 8888;
    udpClient.Client.ReceiveTimeout = 5000; //Set a timeout for receiving responses - 5 seconds timeout
    
    var iPAddresses = Dns.GetHostEntry(Dns.GetHostName()).AddressList.ToList(); // fetch IP Address List in the network
    var detectedServerIPs = new HashSet<IPAddress>();  // List to store detected gRPC server IPs
    IPEndPoint serverEndPoint = new IPEndPoint(IPAddress.Any, 0);

    foreach (var ip in iPAddresses.Where(i => i != null).ToList())
    {
        IPAddress iPAddress = ip;
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
        {
            // Try to map it to IPv4 if it's an IPv4-mapped IPv6 address
            if (ip.IsIPv4MappedToIPv6)
                iPAddress = ip.MapToIPv4();
            else
                continue; // Skip non-mappable IPv6 addresses
        }

        try
        {
            // Send request to server
            udpClient.Send(requestData, requestData.Length, new IPEndPoint(iPAddress, udpPort));
            
            // Receive response to server
            byte[] serverResponseData = udpClient.Receive(ref serverEndPoint);
            string serverResponse = Encoding.ASCII.GetString(serverResponseData);

            if (serverResponse == "Response")
            {
                Console.WriteLine($"Received '{serverResponse}' from {serverEndPoint.Address}");
                detectedServerIPs.Add(serverEndPoint.Address);
            }
        }
        catch (SocketException ex)
        {
            if (ex.SocketErrorCode == SocketError.TimedOut)
            {
                // Ignore timeout exception inside the loop, handled by outer condition
            }
            else
            {
                Console.WriteLine($"SocketException: {ex.Message}");
            }
        }
    }

    udpClient.Close();

    // Create gRPC clients for each detected server

    foreach (var ipAddress in detectedServerIPs)
    {
        Console.WriteLine($"Connecting to gRPC service at {ipAddress}");
        using var channel = GrpcChannel.ForAddress($"http://{ipAddress}:8090");
        var client = new Greeter.GreeterClient(channel);
        var reply = await client.SayHelloAsync(new HelloRequest { Name = $"Request to Grpc Service {ipAddress}" });
        Console.WriteLine($"Greetings: {reply.Message}");
    }
}

Console.WriteLine("Press key to exit");
Console.ReadLine();












