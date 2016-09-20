using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using UPnPTest.Devices;

namespace DJPad.Upnp.SOAP
{
    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    [XmlRoot(Namespace = "http://schemas.xmlsoap.org/soap/envelope/", IsNullable = false)]
    public class Envelope
    {
        [XmlElement("Body")]
        public EnvelopeBody Body
        {
            get;
            set;
        }

        [XmlAttribute(Form = System.Xml.Schema.XmlSchemaForm.Qualified)]
        public string encodingStyle
        {
            get;
            set;
        }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "http://schemas.xmlsoap.org/soap/envelope/")]
    public class EnvelopeBody
    {
        [XmlElement(Type = typeof(BrowseResponse), ElementName = "BrowseResponse", Namespace = "urn:schemas-upnp-org:service:ContentDirectory:1")]
        public BrowseResponse Response
        {
            get;
            set;
        }
    }

    [Serializable]
    [XmlType(AnonymousType = true, Namespace = "urn:schemas-upnp-org:service:ContentDirectory:1")]
    [XmlRoot(Namespace = "urn:schemas-upnp-org:service:ContentDirectory:1", IsNullable = false)]
    public partial class BrowseResponse
    {
        /// <remarks/>
        [XmlElement(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string Result
        {
            get;
            set;
        }

        [XmlElement(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string NumberReturned
        {
            get;
            set;
        }

        [XmlElement(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string TotalMatches
        {
            get;
            set;
        }

        [XmlElement(Form = System.Xml.Schema.XmlSchemaForm.Unqualified)]
        public string UpdateID
        {
            get;
            set;
        }
    }
}
