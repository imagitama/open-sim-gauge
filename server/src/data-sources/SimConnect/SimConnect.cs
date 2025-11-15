#if !DEBUG || WINDOWS
using System;

using System.Collections.Generic;
using Microsoft.FlightSimulator.SimConnect;
using System.Runtime.InteropServices;
using OpenGaugeAbstractions;

    public class SimConnectDataSource : DataSourceBase
    {
        private static class SIMCONNECT_GROUP_PRIORITY
        {
            public const uint HIGHEST = 1;
            public const uint HIGHEST_MASKABLE = 10000000;
            public const uint STANDARD = 1900000000;
            public const uint DEFAULT = 2000000000;
            public const uint LOWEST = 4000000000;
        }

        private SimConnect? _simConnect;
        private const int WM_USER_SIMCONNECT = 0x0402;
        private IntPtr _handle = IntPtr.Zero;

        private enum DEFINITIONS { AircraftInfo, SimVarDef }
        private enum DATA_REQUESTS { Request1 }
        private enum EVENT_GROUPS { DefaultGroup }
        private enum EVENTS { SimStart, AircraftLoaded, FlightLoaded }

        private uint _nextDefinitionId = 0;
        private uint _nextRequestId = 0;

        private readonly Dictionary<(string VarName, string Unit), SimVarSubscription> _simVarSubscriptions = new();
        private readonly Dictionary<(string VarName, string Unit), List<Action<object>>> _callbacksByKey = new();

        private Action<string>? _vehicleCallback;
        private DATA_REQUESTS _requestAircraftTitleReqId;

        private class SimVarSubscription
        {
            public required uint ReqId { get; set; }
            public required string VarName { get; set; }
            public required string Unit { get; set; }
        }

        public SimConnectDataSource()
        {
            Name = "SimConnect";
        }

        public override void Connect()
        {
            if (IsConnected) return;

            try
            {
                Console.WriteLine("[SimConnect] Connecting...");

                _simConnect = new SimConnect("OpenGaugeServer", IntPtr.Zero, 0, null, 0);
                RegisterHandlers();

                InternalSubscribeToEvent("SimStart");
                InternalSubscribeToEvent("AircraftLoaded");
                InternalSubscribeToEvent("FlightLoaded");

                RequestAircraftTitle();

                IsConnected = true;

                Console.WriteLine("[SimConnect] Connected");
            }
            catch (COMException ex)
            {
                throw new Exception("[SimConnect] Failed to connect. Is the sim running?", ex);
            }
        }

        public override void Disconnect()
        {
            if (!IsConnected) return;

            if (_simConnect != null)
                _simConnect.Dispose();

            IsConnected = false;
        }

        private void RegisterHandlers()
        {
            _simConnect!.OnRecvOpen += (sender, data) => Console.WriteLine("[SimConnect] Sim opened");
            _simConnect!.OnRecvQuit += (sender, data) => { Console.WriteLine("[SimConnect] Sim closed"); Disconnect(); };
            _simConnect!.OnRecvException += (sender, data) => Console.WriteLine($"[SimConnect] Sim exception: {data.dwException}");
            // 3 - SIMCONNECT_EXCEPTION_UNRECOGNIZED_ID
            // 7 - SIMCONNECT_EXCEPTION_DATA_ERROR

            _simConnect!.OnRecvSimobjectData += OnRecvSimobjectData;
            _simConnect!.OnRecvEvent += OnRecvEvent;
        }

        public override void SubscribeToVehicle(Action<string> callback)
        {
            _vehicleCallback = callback;
            Console.WriteLine($"[SimConnect] Subscribed to vehicle change");
        }

        private void OnNewVehicle(string vehicleName)
        {
            Console.WriteLine($"[SimConnect] New vehicle '{vehicleName}'");
            CurrentVehicleName = vehicleName;
            _vehicleCallback?.Invoke(vehicleName);
        }

        private void OnRecvSimobjectData(SimConnect sender, SIMCONNECT_RECV_SIMOBJECT_DATA data)
        {
            uint reqId = data.dwRequestID;

            if (reqId == (uint)_requestAircraftTitleReqId && data.dwData[0] is StructString256 str)
            {
                var title = str.value;

                if (title != CurrentVehicleName)
                    OnNewVehicle(title);

                return;
            }

            double value = (double)data.dwData[0];

            foreach (var kvp in _simVarSubscriptions)
            {
                var sub = kvp.Value;
                if (sub.ReqId == reqId)
                {
                    NotifySubscribers(sub.VarName, sub.Unit, value);
                    return;
                }
            }

            Console.WriteLine($"[SimConnect] Unknown request ID {reqId}");
        }

        private void InternalSubscribeToEvent(string eventName)
        {
            var evt = (EVENTS)Enum.Parse(typeof(EVENTS), eventName, ignoreCase: true);

            if (eventName.Equals("SimStart", StringComparison.OrdinalIgnoreCase) ||
                eventName.Equals("AircraftLoaded", StringComparison.OrdinalIgnoreCase) ||
                eventName.Equals("FlightLoaded", StringComparison.OrdinalIgnoreCase))
            {
                _simConnect!.SubscribeToSystemEvent(evt, eventName);
            }
            else
            {
                _simConnect!.MapClientEventToSimEvent(evt, eventName);
                _simConnect!.AddClientEventToNotificationGroup(EVENT_GROUPS.DefaultGroup, evt, false);
                _simConnect!.SetNotificationGroupPriority(EVENT_GROUPS.DefaultGroup, SIMCONNECT_GROUP_PRIORITY.HIGHEST);
            }
        }

        private void OnRecvEvent(SimConnect sender, SIMCONNECT_RECV_EVENT data)
        {
            var eventId = (EVENTS)data.uEventID;

            if (ConfigManager.Debug == true)
                Console.WriteLine($"[SimConnect] Received event '{eventId}'");

            switch (eventId)
            {
                case EVENTS.SimStart:
                case EVENTS.AircraftLoaded:
                case EVENTS.FlightLoaded:
                    RequestAircraftTitle();
                    break;
            }
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public struct StructString256
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
            public string value;
        }

        private void RequestAircraftTitle()
        {
            var defId = (DEFINITIONS)_nextDefinitionId++;
            var reqId = (DATA_REQUESTS)_nextRequestId++;

            _simConnect!.AddToDataDefinition(defId, "TITLE", null, SIMCONNECT_DATATYPE.STRING256, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            _simConnect!.RegisterDataDefineStruct<StructString256>(defId);
            _simConnect!.RequestDataOnSimObject(reqId, defId, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.ONCE, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

            _requestAircraftTitleReqId = reqId;

            if (ConfigManager.Debug == true)
                Console.WriteLine("[SimConnect] Requested aircraft title");
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

        public bool GetIsSubscribed(string varName, string unit)
        {
            return _simVarSubscriptions.ContainsKey((varName, unit));
        }

        private DATA_REQUESTS SubscribeToSimVar(string varName, string unit)
        {
            var defId = (DEFINITIONS)_nextDefinitionId++;
            var reqId = (DATA_REQUESTS)_nextRequestId++;

            SIMCONNECT_DATATYPE type = SIMCONNECT_DATATYPE.FLOAT64;

            // TODO: Support other data types here? note simconnect normalizes basically everything to float
            if (unit == "string")
                type = SIMCONNECT_DATATYPE.STRING32;

            _simConnect!.AddToDataDefinition(defId, varName, unit, type, 0.0f, SimConnect.SIMCONNECT_UNUSED);
            _simConnect!.RegisterDataDefineStruct<double>(defId);

            _simConnect!.RequestDataOnSimObject(reqId, defId, SimConnect.SIMCONNECT_OBJECT_ID_USER, SIMCONNECT_PERIOD.SIM_FRAME, SIMCONNECT_DATA_REQUEST_FLAG.DEFAULT, 0, 0, 0);

            if (ConfigManager.Config?.Debug == true)
                Console.WriteLine($"[SimConnect] Subscribed to SimVar '{varName}' ({unit}) type={type} reqId={reqId}");

            return reqId;
        }

        public override void SubscribeToVar(string varName, string unit, Action<object> callback)
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

                var reqId = SubscribeToSimVar(varName, unit);

                _simVarSubscriptions[key] = new SimVarSubscription()
                {
                    ReqId = (uint)reqId,
                    VarName = varName,
                    Unit = unit
                };
                
                Console.WriteLine($"[SimConnect] Subscribed to SimVar '{varName}' ({unit})");
            }
        }

        public override void UnsubscribeFromVar(string varName, string unit)
        {
            // TODO
            // Console.WriteLine($"[SimConnect] Unsubscribed from SimVar '{varName}' ({unit})");
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
        
        public override void Listen(Config config)
        {
            _ = Task.Run(async () =>
            {
                Console.WriteLine($"[SimConnect] Listening at rate {config.Rate}ms");

                while (IsConnected)
                {
                    try
                    {
                        _simConnect!.ReceiveMessage();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"SimConnect error: {ex.Message}");
                    }

                    await Task.Delay((int)config.Rate);
                }
            });
        }
    }
#endif
