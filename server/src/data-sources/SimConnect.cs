#if !DEBUG || WINDOWS
using System;

using System.Collections.Generic;
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;

namespace OpenGaugeServer
{
    public class SimConnectDataSource : IDataSource
    {
        private static class SIMCONNECT_GROUP_PRIORITY
        {
            public const uint HIGHEST = 1;
            public const uint HIGHEST_MASKABLE = 10000000;
            public const uint STANDARD = 1900000000;
            public const uint DEFAULT = 2000000000;
            public const uint LOWEST = 4000000000;
        }

        private SimConnect _simConnect;
        private const int WM_USER_SIMCONNECT = 0x0402;
        private IntPtr _handle = IntPtr.Zero;

        private enum DEFINITIONS { SimVarDef }
        private enum DATA_REQUESTS { Request1 }
        private enum EVENT_GROUPS { DefaultGroup }
        private enum EVENTS { CustomEventBase = 0 }

        private uint _nextDefinitionId = 0;
        private uint _nextRequestId = 0;

        private readonly Dictionary<(string VarName, string Unit), SimVarSubscription> _simVarSubscriptions = new();
        private readonly Dictionary<(string VarName, string Unit), List<Action<object>>> _callbacksByKey = new();

        private class SimVarSubscription
        {
            public uint ReqId { get; set; }
            public string VarName { get; set; }
            public string Unit { get; set; }
        }

        public bool IsConnected { get; set; } = false;

        public void Connect()
        {
            if (IsConnected) return;

            try
            {
                Console.WriteLine("[SimConnect] Connecting...");

                _simConnect = new SimConnect("OpenGaugeServer", IntPtr.Zero, 0, null, 0);
                RegisterHandlers();
                IsConnected = true;

                Console.WriteLine("[SimConnect] Connected");
            }
            catch (COMException ex)
            {
                throw new Exception("[SimConnect] Failed to connect. Is the sim running?", ex);
            }
        }

        public void Disconnect()
        {
            if (!IsConnected) return;

            _simConnect.Dispose();
            _simConnect = null;
            IsConnected = false;
        }

        private void RegisterHandlers()
        {
            _simConnect.OnRecvOpen += (sender, data) => Console.WriteLine("[SimConnect] Sim opened");
            _simConnect.OnRecvQuit += (sender, data) => { Console.WriteLine("[SimConnect] Sim closed"); Disconnect(); };
            _simConnect.OnRecvException += (sender, data) => Console.WriteLine($"[SimConnect] Sim exception: {data.dwException}");
            // 3 - SIMCONNECT_EXCEPTION_UNRECOGNIZED_ID
            // 7 - SIMCONNECT_EXCEPTION_DATA_ERROR

            _simConnect.OnRecvSimobjectData += OnRecvSimobjectData;
            _simConnect.OnRecvEvent += OnRecvEvent;
        }

        private void OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            uint reqId = data.dwRequestID;
            double value = (double)data.dwData[0];

            foreach (var kvp in _simVarSubscriptions)
            {
                var sub = kvp.Value;
                if (sub.ReqId == reqId)
                {
                    if (sub.VarName == "PLANE BANK DEGREES")
                        Console.WriteLine($"[SimConnect] {sub.VarName} = {value} ({sub.Unit})");

                    // sub.Callback?.Invoke(value);

                    NotifySubscribers(sub.VarName, sub.Unit, value);

                    return;
                }
            }

            Console.WriteLine($"[SimConnect] Unknown request ID {reqId}");
        }

        private void NotifySubscribers(string simVarName, string unit, object value)
        {
            foreach (var kvp in _callbacksByKey)
            {
                var (VarName, Unit) = kvp.Key;

                if (VarName == simVarName && Unit == unit)
                {
                    var callbacks = kvp.Value;

                    foreach (var cb in callbacks)
                    {
                        cb.Invoke(value);
                    }
                }
            }
        }

        private void OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            Console.WriteLine($"[SimConnect] Received event X");

            // foreach (var cb in _eventCallbacks.Values)
            //     cb();
        }

        public bool GetIsSubscribed(string varName, string unit)
        {
            return _simVarSubscriptions.ContainsKey((varName, unit));
        }

        private void SubscribeToSimVar(string varName, string unit)
        {
            var key = (varName, unit);

            var defId = (DEFINITIONS)_nextDefinitionId++;
            var reqId = (DATA_REQUESTS)_nextRequestId++;

            _simConnect.AddToDataDefinition(defId, varName, unit, SIMCONNECT_DATATYPE.FLOAT64, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            _simConnect.RegisterDataDefineStruct<double>(defId);

            _simConnect.RequestDataOnSimObject(reqId, defId, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

            _simVarSubscriptions[key] = new SimVarSubscription()
            {
                ReqId = (uint)reqId,
                VarName = varName,
                Unit = unit
            };
            
            Console.WriteLine($"[SimConnect] Subscribed to SimVar '{varName}' ({unit})");
        }

        public void SubscribeToVar(string varName, string unit, Action<object> callback)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to sim");

            if (!GetIsSubscribed(varName, unit))
            {
                var key = (varName, unit);

                if (!_callbacksByKey.ContainsKey(key))
                {
                    _callbacksByKey[key] = new List<Action<object>>();
                }

                _callbacksByKey[key].Add(callback);

                SubscribeToSimVar(varName, unit);
            }
        }

        public void SubscribeToEvent(string eventName, Action callback)
        {
            if (!IsConnected) throw new InvalidOperationException("Not connected to sim");

            // _eventCallbacks[eventName] = callback;

            // int eventId = _eventCallbacks.Count + (int)EVENTS.CustomEventBase;
            // var eventEnum = (EVENTS)eventId;

            // _simConnect.MapClientEventToSimEvent(eventEnum, eventName);
            // _simConnect.AddClientEventToNotificationGroup(EVENT_GROUPS.DefaultGroup, eventEnum, false);
            // _simConnect.SetNotificationGroupPriority(EVENT_GROUPS.DefaultGroup, (uint)SIMCONNECT_GROUP_PRIORITY.HIGHEST);
            
            // Console.WriteLine($"[SimConnect] Subscribed to event '{eventName}'");
        }
        
        public void Listen(Config config)
        {
            _ = Task.Run(async () =>
            {
                int rate = config.Rate ?? 50; // 20Hz

                Console.WriteLine($"[SimConnect] Listening at rate {rate}");

                while (IsConnected)
                {
                    try
                    {
                        // Console.WriteLine("[SimConnect] ReceiveMessage()");
                        _simConnect.ReceiveMessage();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SimConnect error: {ex.Message}");
                    }

                    await Task.Delay(rate);
                }
            });
        }

        public void WatchVar(string varName) {}
    }
}
#endif
