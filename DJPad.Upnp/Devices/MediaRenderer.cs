using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UPnPTest.Devices
{
    public class MediaRenderer
    {
        public GenericUpnpDevice DeviceDetails { get; private set; }

        public MediaRenderer(GenericUpnpDevice mediaRendererDeviceDetails)
        {
            this.DeviceDetails = mediaRendererDeviceDetails;
        }
    }
}
