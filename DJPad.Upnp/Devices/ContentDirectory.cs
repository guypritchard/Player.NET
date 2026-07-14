using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UPnPTest.Devices
{
    using DJPad.Upnp.SOAP;
    using System.Configuration;
    using System.Diagnostics;
    using System.IO;
    using System.Net.Http;
    using System.Security.Policy;
    using System.Xml;
    using System.Xml.Serialization;

    // https://nmaier.github.io/simpleDLNA/
    public class ContentDirectory
    {
        private static readonly HttpClient HttpClient = new HttpClient();
        private GenericUpnpDevice Device
        {
            get;
            set;
        }

        public ContentDirectory(GenericUpnpDevice device)
        {
            this.Device = device;
           

        }

        public void Browse()
        {
            var host = new Uri(this.Device.Location).Host;
            var port = new Uri(this.Device.Location).Port;
            var serviceLocation = this.Device.Description.Device.serviceList.FirstOrDefault(
                s =>
                    string.Equals(s.serviceType, GenericUpnpDevice.ContentDirectory1, StringComparison.OrdinalIgnoreCase));

            var location = "http://" + host + ":" + port + serviceLocation.controlURL;
            Trace.WriteLine(location);

            using var request = CreateRequest(new Uri(location), GenericUpnpDevice.ContentDirectory1 + "#Browse");
            using var response = HttpClient.Send(request);
            response.EnsureSuccessStatusCode();
            using var dataStream = response.Content.ReadAsStream();
            XmlSerializer mySerializer = new XmlSerializer(typeof(Envelope));
            var browseResponse = (Envelope)mySerializer.Deserialize(dataStream);
            
            XmlSerializer mydidSerializer = new XmlSerializer(typeof(roottype));

            var didl = (roottype)mydidSerializer.Deserialize(new StringReader((string)browseResponse.Body.Response.Result));
        }

        private static HttpRequestMessage CreateRequest(Uri url, string action)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, url);
            request.Headers.Add("SOAPACTION", "\"" + action + "\"");
            request.Headers.Accept.ParseAdd("text/xml");
            request.Content = new StringContent(DJPad.Upnp.Resource.Browse, Encoding.UTF8, "text/xml");
            return request;
        }

        private static XmlDocument CreateSoapEnvelope()
        {
            XmlDocument soapEnvelop = new XmlDocument();
            soapEnvelop.LoadXml(DJPad.Upnp.Resource.Browse);
            return soapEnvelop;
        }

    }
}
