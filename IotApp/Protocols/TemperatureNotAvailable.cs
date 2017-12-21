namespace IotApp.Protocols
{
    public sealed class TemperatureNotAvailable : ITemperatureReading
    {
        public static TemperatureNotAvailable Instance { get; } = new TemperatureNotAvailable();
        private TemperatureNotAvailable() { }
    }
}