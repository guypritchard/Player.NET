using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace UPnPTest.Message
{
    public class ResponseMessage : SSDPMessage
    {
        
        public int MessageDuration
        {
            get;
            set;
        }

        public string Location
        {
            get;
            set;
        }

        public string NotificationType
        {
            get;
            set;
        }

        public string NotificationSubType
        {
            get;
            set;
        }

        public string Server
        {
            get;
            set;
        }

        public string UniqueServiceName
        {
            get;
            set;
        }

        public ResponseMessage()
        {
            this.MessageType = MessageTypeEnum.Response;
        }


       public ResponseMessage(Dictionary<string, string> headers) : this()
        {
            this.MessageHeaders = headers;

            if (this.MessageHeaders.ContainsKey(UpnpClient.HostHeader))
            {
                this.Host = this.MessageHeaders[UpnpClient.HostHeader];
            }

            if (this.MessageHeaders.ContainsKey(UpnpClient.MessageDurationHeader))
            {
                int duration = 0;
                if (int.TryParse(this.MessageHeaders[UpnpClient.MessageDurationHeader].Split('=')[1], out duration))
                {
                    this.MessageDuration = duration;
                }
            }

            if (this.MessageHeaders.ContainsKey(UpnpClient.UniqueServerNameHeader))
            {
                this.UniqueServiceName = this.MessageHeaders[UpnpClient.UniqueServerNameHeader];
            }

            if (this.MessageHeaders.ContainsKey(UpnpClient.LocationHeader))
            {
                this.Location = this.MessageHeaders[UpnpClient.LocationHeader];
            }
        }
    }
}
