namespace OpenGaugeAbstractions
{
    public interface IDataSource
    {
        /// <summary>
        /// The name of the current vehicle (or null if the player is not in one).
        /// </summary>
        public string? CurrentVehicleName { get; set; }

        /// <summary>
        /// Called whenever a gauge layer wants to subscribe to a var.
        /// </summary>
        public Task SubscribeToVar(string varName, string? unit, Action<object> callback);
        /// <summary>
        /// Called whenever a gauge unrenders and was subscribed to a var.
        /// </summary>
        public Task UnsubscribeFromVar(string varName, string? unit, Action<object> callback);
        /// <summary>
        /// Called whenever a gauge layer wants to subscribe to an event.
        /// </summary>
        public Task SubscribeToEvent(string eventName, Action<object> callback);
        /// <summary>
        /// Called whenever a gauge unrenders and was subscribed to an event.
        /// </summary>
        public Task UnsubscribeFromEvent(string eventName, Action<object> callback);
        /// <summary>
        /// A function called with a callback so that clients can be notified of vehicle changes.
        /// You should also set `CurrentVehicleName` whenever the vehicle changes.
        /// </summary>
        public Task SubscribeToVehicle(Action<string> callback);

        bool IsConnected { get; set; }
        /// <summary>
        /// Attempts to connect to the data source. It must set `IsConnected` before it ends.
        /// Optionally can return a task to delay any reconnection attempts.
        /// </summary>
        Task Connect();
        /// <summary>
        /// Attempts to disconnect from the data source.
        /// </summary>
        Task Disconnect();
        /// <summary>
        /// Attempts to listen to the data source.
        /// </summary>
        Task Listen();
    }

    public abstract class DataSourceBase : IDataSource
    {
        public virtual string? CurrentVehicleName { get; set; }

        public abstract Task SubscribeToVar(string varName, string? unit, Action<object> callback);
        public abstract Task UnsubscribeFromVar(string varName, string? unit, Action<object> callback);
        public abstract Task SubscribeToEvent(string eventName, Action<object> callback);
        public abstract Task UnsubscribeFromEvent(string eventName, Action<object> callback);

        public abstract Task SubscribeToVehicle(Action<string> callback);

        public bool IsConnected { get; set; }
        public abstract Task Connect();
        public abstract Task Disconnect();
        public abstract Task Listen();
    }
}