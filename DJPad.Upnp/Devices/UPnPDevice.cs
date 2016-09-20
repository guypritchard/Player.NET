namespace UPnPTest.Devices
{
    using System.Diagnostics;
    using System.Net;
    using System.Xml;
    using System.Xml.Serialization;
    using UPnPTest.Message;


    public class GenericUpnpDevice
    {
        public const string All = "ssdp:all";
        public const string Root = "upnp:rootdevice";
        public const string Basic1 = "urn:schemas-upnp-org:device:Basic:1";
        public const string MediaServer1 = "urn:schemas-upnp-org:device:MediaServer:1";
        public const string MediaServer2 = "urn:schemas-upnp-org:device:MediaServer:2";
        public const string MediaServer3 = "urn:schemas-upnp-org:device:MediaServer:3";
        public const string MediaRenderer1 = "urn:schemas-upnp-org:device:MediaRenderer:1";
        public const string WfaDevice1 = "urn:schemas-wifialliance-org:device:WFADevice:1";
        public const string ContentDirectory1 = "urn:schemas-upnp-org:service:ContentDirectory:1";
        
        public DeviceDescription Description { get; set; }
        public SSDPMessage Notification { get; set; }

        public string Location
        {
            get { return Notification.MessageHeaders[UpnpClient.LocationHeader]; }
        }

        public string Id
        {
            get {
                return Description != null 
                    ? Description.Device.UDN 
                    : "Unknown";
            }
        }

        public static GenericUpnpDevice DiscoverDevice(NotifyMessage deviceNotification)
        {
            if (deviceNotification != null)
            {
                return new GenericUpnpDevice
                {
                    Description = ProcessDeviceNotification(deviceNotification.UniqueServiceName, deviceNotification.Location), 
                    Notification = deviceNotification
                };
            }

            return null;
        }

        public static GenericUpnpDevice DiscoverDevice(ResponseMessage deviceResponse)
        {
            if (deviceResponse != null)
            {

                return new GenericUpnpDevice
                {
                    Description = ProcessDeviceNotification(deviceResponse.UniqueServiceName, deviceResponse.Location),
                    Notification = deviceResponse
                };
            }

            return null;
        }

        public bool IsValidDevice()
        {
            return this.Description != null && this.Notification != null;
        }

        public bool IsMediaSource()
        {
            return this.IsValidDevice() &&
                (this.Description.Device.deviceType == MediaServer1 ||
                 this.Description.Device.deviceType == MediaServer2 ||
                 this.Description.Device.deviceType == MediaServer3);
        }

        public bool IsBasicDevice()
        {
            return this.IsValidDevice() &&
                this.Description.Device.deviceType == Basic1;
        }

        public override string ToString()
        {
            return this.Notification.Host + " " + this.Description.Device.friendlyName;
        }

        public static DeviceDescription ProcessDeviceNotification(string serviceName, string location)
        {
            DeviceDescription device = null;

            try
            {
                if (!string.IsNullOrEmpty(serviceName))
                {
                    var serializer = new XmlSerializer(typeof(DeviceDescription));
                    var request = (HttpWebRequest)WebRequest.Create(location);

                    using (var response = (HttpWebResponse)request.GetResponse())
                    {
                        device = (DeviceDescription)serializer.Deserialize(response.GetResponseStream());
                        Trace.WriteLine(device.Device.friendlyName + " found");
                    }
                }
            }
            catch (XmlException xmlException)
            {
                Trace.WriteLine(xmlException);
            }
            catch (WebException webException)
            {
                Trace.WriteLine(webException);
            }

            return device;
        }
    }
}
