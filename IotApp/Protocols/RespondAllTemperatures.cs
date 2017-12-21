using System.Collections.Immutable;

namespace IotApp.Protocols
{
    public sealed class RespondAllTemperatures
    {
        public RespondAllTemperatures(long requestId, IImmutableDictionary<string, ITemperatureReading> temperatures)
        {
            RequestId = requestId;
            Temperatures = temperatures;
        }

        public long RequestId { get; }
        public IImmutableDictionary<string, ITemperatureReading> Temperatures { get; }
    }
}