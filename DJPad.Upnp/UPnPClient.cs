namespace UPnPTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Sockets;
    using System.Net;
    using System.Threading;
    using System.Threading.Tasks;
    using UPnPTest.Message;
    using System.Diagnostics;
    using Utils;
    using UPnPTest.Devices;

    public class UpnpClient
    {
        public enum UPnPDeviceTypes { MediaServer1, MediaServer2, MediaServer3, MediaRenderer1,  };

        public const string NotifyHeader = "NOTIFY * HTTP/1.1";
        public const string SearchHeader = "M-SEARCH * HTTP/1.1";
        public const string ResponseHeader = "HTTP/1.1 200 OK";
        public const string HostHeader = "HOST";
        public const string MessageDurationHeader = "CACHE-CONTROL";
        public const string LocationHeader = "LOCATION";
        public const string NotificationTypeHeader = "NT";
        public const string NotificationSubTypeHeader = "NTS";
        public const string ServerHeader = "SERVER";
        public const string UniqueServerNameHeader = "USN";
        public const string SubType = "ST";
        public const string Mx = "MX";
        public const string Man = "MAN";
        public const string UserAgent = "USER-AGENT";

        public const int PortNumber = 1900;
        public const string MulticastIPAddress = "239.255.255.250";

        private Task notifier;
        private Task listener;

        public Guid Id { get; set; }

        public List<MediaServer> MediaServers { get; set; }
        public Dictionary<string, GenericUpnpDevice> Devices { get; set; }

        private UdpClient networkClient;

        public event Action<GenericUpnpDevice> NewMediaDevice;

        public UpnpClient()
        {
            this.Id = Guid.NewGuid();
            this.MediaServers = new List<MediaServer>();
            this.Devices = new  Dictionary<string, GenericUpnpDevice>();
        }

        public void Initialize()
        {
            Console.WriteLine(GetLocalIPAddress());

            this.networkClient = new UdpClient();
            this.networkClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
            this.networkClient.Client.Bind(new IPEndPoint(GetLocalIPAddress(), 0));
            this.networkClient.JoinMulticastGroup(IPAddress.Parse(UpnpClient.MulticastIPAddress));
            this.networkClient.MulticastLoopback = true;

            this.listener = PeriodicTask.Run(this.SendNotification, TimeSpan.FromSeconds(5.0));
            this.notifier = Task.Factory.StartNew(() => this.ReceiveNotifications(CancellationToken.None));
        }

        private NotifyMessage CreateAdvertiseMessage()
        {
            var headers = new Dictionary<string, string>
            {
                {UpnpClient.HostHeader, UpnpClient.MulticastIPAddress + ":" + UpnpClient.PortNumber},
                {UpnpClient.MessageDurationHeader, "max-age=1810"},
                    {
                        UpnpClient.LocationHeader, 
                        "http://" + Dns.GetHostAddresses("localhost").FirstOrDefault().MapToIPv4() + "/DeviceDescription.xml"
                    },
                {UpnpClient.NotificationTypeHeader, GenericUpnpDevice.MediaRenderer1},
                {UpnpClient.NotificationSubTypeHeader, "ssdp:alive"},
                {UpnpClient.ServerHeader, "UPnP/1.0, UPnPClient Cheese"},
                {UpnpClient.UniqueServerNameHeader, string.Format("{0}::{1}", this.Id, GenericUpnpDevice.MediaRenderer1)}
            };

            var advertise = new NotifyMessage(headers);
            return advertise;
        }

        private SearchMessage CreateSearchMessage(string subType)
        {
            var headers = new Dictionary<string, string>
            {
                {UpnpClient.HostHeader, UpnpClient.MulticastIPAddress + ":" + UpnpClient.PortNumber},
                {UpnpClient.SubType, subType},
                {UpnpClient.Man, "\"ssdp:discover\""},
                {UpnpClient.Mx, "3"},
            };

            var search = new SearchMessage(headers);
            return search;
        }

        private void ReceiveNotifications(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                IPEndPoint remoteendpoint = null;
                byte[] data = this.networkClient.Receive(ref remoteendpoint);

                SSDPMessage message = SSDPMessage.Parse(data);

                switch (message.MessageType)
                {
                    case SSDPMessage.MessageTypeEnum.Notify:
                        var notify = message as NotifyMessage;
                        if (notify.UniqueServiceName.Contains(this.Id.ToString()))
                        {
                            return;
                        }

                        Trace.WriteLine("Device Notification Received.");
                        var device = GenericUpnpDevice.DiscoverDevice(notify);

                        if (device != null && device.IsValidDevice())
                        {
                            if (!this.Devices.ContainsKey(device.Id))
                            {
                                this.Devices[device.Id] = device;
                                Trace.WriteLine("New Generic Device added.");
                            }

                            if (device.IsMediaSource())
                            {
                                if (this.MediaServers.All(mr => mr.DeviceDetails.Id != device.Id))
                                {
                                    this.MediaServers.Add(new MediaServer(device));
                                    if (this.NewMediaDevice != null)
                                    {
                                        this.NewMediaDevice(device);
                                    }
                                }
                            }
                        }

                        break;

                    case SSDPMessage.MessageTypeEnum.Response:
                        var response = message as ResponseMessage;
                        var respondingDevice = GenericUpnpDevice.DiscoverDevice(response);

                        if (respondingDevice.IsMediaSource())
                        {
                            if (this.MediaServers.All(mr => mr.DeviceDetails.Id != respondingDevice.Id))
                            {
                                this.MediaServers.Add(new MediaServer(respondingDevice));
                                Trace.WriteLine("New Media Server added.");

                                if (this.NewMediaDevice != null)
                                {
                                    this.NewMediaDevice(respondingDevice);
                                }
                            }
                        }

                        Trace.WriteLine("Response Notification Received." + string.Join(",", response.MessageHeaders));
                        break;

                    case SSDPMessage.MessageTypeEnum.Search:
                        var search = message as SearchMessage;
                        Trace.WriteLine("Search Notification Received." + string.Join(",", search.MessageHeaders));
                        break;
                }
            }
        }
        public static IPAddress GetLocalIPAddress()
        {
            var host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (var ip in host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip;
                }
            }
            throw new Exception("Local IP Address Not Found!");
        }

        private void SendNotification()
        {
            //var advert = this.CreateAdvertiseMessage().Serialize();
            //this.networkClient.Send(
            //    advert, 
            //    advert.Length, 
            //    new IPEndPoint(IPAddress.Parse(UpnpClient.MulticastIPAddress), UpnpClient.PortNumber));


            var search1 = this.CreateSearchMessage(GenericUpnpDevice.ContentDirectory1).Serialize();
            this.networkClient.Send(
                search1,
                search1.Length,
                new IPEndPoint(IPAddress.Parse(UpnpClient.MulticastIPAddress), UpnpClient.PortNumber));

            var search = this.CreateSearchMessage(GenericUpnpDevice.MediaServer1).Serialize();
            this.networkClient.Send(
                search, 
                search.Length, 
                new IPEndPoint(IPAddress.Parse(UpnpClient.MulticastIPAddress), UpnpClient.PortNumber));
           
           //  Trace.WriteLine("Notification Sent.");
        }
    }
}
