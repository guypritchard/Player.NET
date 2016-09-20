using System.Collections.Generic;

namespace UPnPTest.Message
{
    public class SearchMessage : SSDPMessage
    {
        public SearchMessage(Dictionary<string, string> headers)
        {
            this.MessageType = MessageTypeEnum.Search;
            this.MessageHeaders = headers;
        }
    }
}
