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
    using System.Net;
    using System.Security.Policy;
    using System.Xml;
    using System.Xml.Serialization;

    // https://nmaier.github.io/simpleDLNA/
    public class ContentDirectory
    {
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

            XmlDocument soapEnvelopeXml = CreateSoapEnvelope();
            HttpWebRequest webRequest = CreateWebRequest(new Uri(location), GenericUpnpDevice.ContentDirectory1 + "#Browse");
            InsertSoapEnvelopeIntoWebRequest(soapEnvelopeXml, webRequest);

            var response = webRequest.GetResponse();

            var dataStream = response.GetResponseStream();
            XmlSerializer mySerializer = new XmlSerializer(typeof(Envelope));
            var browseResponse = (Envelope)mySerializer.Deserialize(dataStream);
            
            XmlSerializer mydidSerializer = new XmlSerializer(typeof(roottype));

            var didl = (roottype)mydidSerializer.Deserialize(new StringReader((string)browseResponse.Body.Response.Result));
        }

        private static HttpWebRequest CreateWebRequest(Uri url, string action)
        {
            HttpWebRequest webRequest = (HttpWebRequest)WebRequest.Create(url);
            webRequest.Headers.Add("SOAPACTION", "\"" + action + "\"");
            webRequest.ContentType = "text/xml;charset=\"utf-8\"";
            webRequest.Accept = "text/xml";
            webRequest.Method = "POST";
            return webRequest;
        }

        private static XmlDocument CreateSoapEnvelope()
        {
            XmlDocument soapEnvelop = new XmlDocument();
            soapEnvelop.LoadXml(DJPad.Upnp.Resource.Browse);
            return soapEnvelop;
        }

        private static void InsertSoapEnvelopeIntoWebRequest(XmlDocument soapEnvelopeXml, HttpWebRequest webRequest)
        {

            byte[] binary = Encoding.UTF8.GetBytes(DJPad.Upnp.Resource.Browse);
            webRequest.ContentLength = DJPad.Upnp.Resource.Browse.Length;
            using (Stream stream = webRequest.GetRequestStream())
            {
               stream.Write(binary, 0, binary.Length);
            }
        }
    }
}
