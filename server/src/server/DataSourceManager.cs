using OpenGaugeAbstractions;

namespace OpenGaugeServer
{
    public class DataSourceManager
    {
        private IDataSource _dataSource;
        private readonly Dictionary<(string VarName, string? Unit), object> _vars = [];
        private readonly Dictionary<(string VarName, string? Unit), object> _forcedVars = [];
        private string? _currentVehicleName;
        private string? _forceVehicleName;
        private Action<string>? _vehicleCallback;
        private readonly Dictionary<(string VarName, string? Unit), Action<object?>> _varCallbacks = [];
        private readonly Dictionary<string, Action<object?>> _eventCallbacks = [];
        private (string VarName, string? Unit)? _watching = null;
        private int _watchCount = 0;

        public DataSourceManager(IDataSource dataSource)
        {
            _dataSource = dataSource;
        }

        public void WatchVar(string varName, string? unit)
        {
            var key = GetKey(varName, unit);
            _watching = key;
            _watchCount = 0;
            Console.WriteLine($"[DataSourceManager] Watching var '{varName}' ({unit})");
        }

        public bool GetIsSubscribedToVar(string varName, string? unit)
        {
            var key = GetKey(varName, unit);
            return _varCallbacks.ContainsKey(key);
        }

        public bool GetIsSubscribedToEvent(string eventName)
        {
            var key = eventName.ToLower();
            return _eventCallbacks.ContainsKey(key);
        }

        public void SubscribeToVar(string varName, string? unit, Action<object?> callback)
        {
            // TODO: throw here?
            if (GetIsSubscribedToVar(varName, unit))
                return;

            var key = GetKey(varName, unit);

            void managerCallback(object newValue)
            {
                StoreVarValue(varName, unit, newValue);
                var valueToUse = GetVarValue(varName, unit);

                if (_watching.HasValue && (_watching == key || (_watching.Value.VarName.ToLower() == varName.ToLower())) && _watchCount < 10)
                {
                    Console.WriteLine($"[DataSourceManager] Var={varName} Unit={unit} value={newValue} use={valueToUse}");
                    _watchCount++;
                }

                callback(valueToUse);
            }

            _dataSource.SubscribeToVar(varName, unit, managerCallback);

            _varCallbacks[key] = managerCallback;

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[DataSourceManager] Subscribed to var '{varName}' ({unit})");
        }

        public void UnsubscribeFromVar(string varName, string? unit)
        {
            if (!GetIsSubscribedToVar(varName, unit))
                return;

            var key = GetKey(varName, unit);

            var managerCallback = _varCallbacks[key];

            _dataSource.UnsubscribeFromVar(varName, unit, managerCallback);

            _varCallbacks.Remove(key);

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[DataSourceManager] Unsubscribed from var '{varName}' ({unit})");
        }

        public void SubscribeToEvent(string eventName, Action<object> callback)
        {
            // TODO: throw here?
            if (GetIsSubscribedToEvent(eventName))
                return;

            var key = eventName.ToLower();

            void managerCallback(object newValue)
            {
                callback(newValue);
            }

            _dataSource.SubscribeToEvent(eventName, managerCallback);

            _eventCallbacks[key] = managerCallback;

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[DataSourceManager] Subscribed to event '{eventName}'");
        }

        public void UnsubscribeFromEvent(string eventName)
        {
            if (!GetIsSubscribedToEvent(eventName))
                return;

            var key = eventName.ToLower();

            var managerCallback = _eventCallbacks[key];

            _dataSource.UnsubscribeFromEvent(eventName, managerCallback);

            _eventCallbacks.Remove(key);

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[DataSourceManager] Unsubscribed from event '{eventName}'");
        }

        public void SubscribeToVehicle(Action<string> callback)
        {
            void managerCallback(string vehicleName)
            {
                _currentVehicleName = vehicleName;
                callback(vehicleName);
            }

            _dataSource.SubscribeToVehicle(managerCallback);

            _vehicleCallback = managerCallback;

            if (ConfigManager.Config.Debug)
                Console.WriteLine($"[DataSourceManager] Subscribed to vehicle");
        }

        public string? GetCurrentVehicleName()
        {
            if (_forceVehicleName != null)
                return _forceVehicleName;

            return _currentVehicleName;
        }

        public void UnsubscribeFromUnusedVars(VarDef[] vars)
        {
            foreach (var kvp in _varCallbacks)
            {
                var (varName, unit) = kvp.Key;

                if (!vars.Any(v => v.Name.ToLower() == varName.ToLower() && v.Unit?.ToLower() == unit?.ToLower()))
                {
                    UnsubscribeFromVar(varName, unit);
                }
            }
        }

        public void UnsubscribeFromUnusedEvents(string[] eventNames)
        {
            foreach (var kvp in _eventCallbacks)
            {
                var eventName = kvp.Key;

                if (!eventNames.Contains(eventName.ToLower()))
                {
                    UnsubscribeFromEvent(eventName);
                }
            }
        }

        public void StoreVarValue(string varName, string? unit, object value)
        {
            var key = GetKey(varName, unit);
            _vars[key] = value;
        }

        private object? GetVarValue(string varName, string? unit)
        {
            var key = GetKey(varName, unit);
            object? result;

            if (_forcedVars.TryGetValue(key, out var forced))
                result = forced;
            else if (_vars.TryGetValue(key, out var actualValue))
                result = actualValue;
            else
                return null;

            if (result is string s && double.TryParse(s, out var n))
                result = n;

            return result;
        }

        public void ForceVarValue(string varName, string? unit, object value)
        {
            var key = GetKey(varName, unit);
            _forcedVars[key] = value;
            Console.WriteLine($"[DataSourceManager] Forcing var '{varName}' ({unit}) to '{value}'");
        }

        public void ClearForcedVar(string varName, string? unit)
        {
            var key = GetKey(varName, unit);
            _forcedVars.Remove(key);
            Console.WriteLine($"[DataSourceManager] Clear forced var '{varName}' ({unit})");
        }

        public void ForceVehicleName(string vehicleName)
        {
            _forceVehicleName = vehicleName;
            _vehicleCallback?.Invoke(vehicleName);
            Console.WriteLine($"[DataSourceManager] Forcing vehicle name '{vehicleName}'");
        }

        public void ClearForcedVehicleName()
        {
            _forceVehicleName = null;
            Console.WriteLine($"[DataSourceManager] Clear forced vehicle name");
        }

        private static (string VarName, string? Unit) GetKey(string varName, string? unit)
        {
            return (varName.ToLower(), unit?.ToLower());
        }
    }
}