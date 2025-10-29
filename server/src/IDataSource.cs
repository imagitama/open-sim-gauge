namespace OpenGaugeServer
{
    public interface IDataSource
    {
        public void SubscribeToVar(string varName, string unit, Action<object> callback);
        public void SubscribeToEvent(string eventName, Action callback);

        bool IsConnected { get; set; }
        void Connect();
        void Disconnect();
        void Listen(Config config);
        void WatchVar(string varName);
    }
}