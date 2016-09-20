using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UPnPTest.Devices
{
    public class MediaServer
    {
        public GenericUpnpDevice DeviceDetails { get; set; }

        public MediaServer(GenericUpnpDevice mediaRendererDeviceDetails)
        {
            DeviceDetails = mediaRendererDeviceDetails;
        }
    }
}
