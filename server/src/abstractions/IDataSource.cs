namespace OpenGaugeAbstractions
{
    public interface IDataSource
    {
        public string Name { get; set; }
        public string? CurrentVehicleName { get; set; }

        public void SubscribeToVar(string varName, string unit, Action<object> callback);
        public void UnsubscribeFromVar(string varName, string unit);
        public void SubscribeToVehicle(Action<string> callback);

        bool IsConnected { get; set; }
        void Connect();
        void Disconnect();
        void Listen(Config config);
        void WatchVar(string varName);
    }

    public abstract class DataSourceBase : IDataSource
    {
        public required string Name { get; set; }
        public virtual string? CurrentVehicleName { get; set; }

        public abstract void SubscribeToVar(string varName, string unit, Action<object> callback);
        public abstract void UnsubscribeFromVar(string varName, string unit);

        public virtual void SubscribeToVehicle(Action<string> callback) { }
        public virtual void WatchVar(string varName) { }

        public bool IsConnected { get; set; }
        public abstract void Connect();
        public abstract void Disconnect();
        public abstract void Listen(Config config);
    }
}