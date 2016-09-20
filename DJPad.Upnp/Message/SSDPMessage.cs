using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace UPnPTest.Message
{
    public class SSDPMessage
    {
        public enum MessageTypeEnum { Notify, Search, Response }

        public static SSDPMessage Parse(byte[] data)
        {
            string message = System.Text.ASCIIEncoding.ASCII.GetString(data);
            SSDPMessage ssdpMessage = null;
            Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            string[] messageHeaders = message.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            if (messageHeaders.Length == 0)
            {
                throw new InvalidOperationException("Message doesn't have any content.");
            }

            foreach (string header in messageHeaders)
            {
                int splitPosition = header.IndexOf(':');

                if (splitPosition != -1)
                {
                    string name = header.Substring(0, splitPosition).Trim();
                    string value = header.Substring(splitPosition + 1).Trim();
                    headers.Add(name, value);
                }
            }

            switch (messageHeaders[0])
            {
                case UpnpClient.NotifyHeader:
                    ssdpMessage = new NotifyMessage(headers);
                    break;

                case UpnpClient.SearchHeader:
                    ssdpMessage = new SearchMessage(headers);
                    break;

                default:
                    ssdpMessage = new ResponseMessage(headers);
                    break;
            }

            return ssdpMessage;
        }

        public byte[] Serialize()
        {
            StringBuilder message = new StringBuilder();

            if (this.MessageType == MessageTypeEnum.Notify)
            {
                message.Append(UpnpClient.NotifyHeader);
            }
            else if (this.MessageType == MessageTypeEnum.Search)
            {
                message.Append(UpnpClient.SearchHeader);
            }

            message.Append("\r\n");

            foreach(var header in this.MessageHeaders.Keys)
            {
                message.Append(header + ":" + this.MessageHeaders[header]);
                message.Append("\r\n");
            }

            message.Append("\r\n");

            var send = message.ToString();

            Trace.WriteLine(send);

            return Encoding.UTF8.GetBytes(send);
        }

        public MessageTypeEnum MessageType
        {
            get;
            set;
        }

        public Dictionary<string, string> MessageHeaders
        {
            get;
            protected set;
        }

        public Dictionary<string, string> ExtensionHeaders
        {
            get;
            set;
        }

        public string Host
        {
            get;
            set;
        }

    }
}
