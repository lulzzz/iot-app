using System.Collections.Immutable;

namespace IotApp.Protocols
{
    public sealed class ReplyDeviceList
    {
        public ReplyDeviceList(long requestId, IImmutableSet<string> ids)
        {
            RequestId = requestId;
            Ids = ids;
        }

        public long RequestId { get; }
        public IImmutableSet<string> Ids { get; }
    }
}