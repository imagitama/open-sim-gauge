using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace OpenGaugeServer
{
    public class EmulatorDataSource : IDataSource
    {
        private readonly Random _rand = new();
        private readonly Dictionary<string, Func<string?, object>> _simVars = new();
        private readonly Dictionary<string, object> _forcedVars = new();
        private readonly List<string> _simEvents = new();
        private CancellationTokenSource _cts = null!;
        private string? _watching = null;
        private int _watchCount = 0;

        public class CallbackInfo
        {
            public required string name;
            public required string unit;
            public required Action<object> callback;
        }

        private readonly List<CallbackInfo> callbacks = new();

        public bool IsConnected { get; set; } = false;

        public void WatchVar(string varName)
        {
            _watching = varName;
        }

        public void SubscribeToVar(string name, string unit, Action<object> callback)
        {
            callbacks.Add(new CallbackInfo {
                name = name,
                unit = unit,
                callback = callback
            });
            
            Console.WriteLine($"[EMULATOR] Subscribed to var '{name}' ({unit})");
        }

        public void SubscribeToEvent(string eventName, Action callback)
        {
            // if (!_simEvents.Contains(eventName))
            // {
            //     _simEvents.Add(eventName);
                
            //     _eventCallbacks[eventName] = callback;

            //     Console.WriteLine($"[EMULATOR] Subscribed to event '{eventName}'");
            // }
        }

        public void Listen(Config config) {}

        public void Connect()
        {
            IsConnected = true;
            _cts = new CancellationTokenSource();

            _simVars.Clear();

            _simVars["INDICATED ALTITUDE"] = unit => _altitude; // feet
            _simVars["AIRSPEED INDICATED"] = unit => _airspeedKnots; // knots
            _simVars["PLANE BANK DEGREES"] = unit =>
                unit switch
                {
                    "radians" => _planeBankRadians,
                    _ => _planeBankRadians * 180.0 / Math.PI
                };
            _simVars["GENERAL ENG RPM:1"] = unit => _rpmLeft;
            _simVars["GENERAL ENG RPM:2"] = unit => _rpmRight;
            _simVars["VERTICAL SPEED"] = unit =>
                unit switch
                {
                    "feet" => _verticalSpeed,
                    _ => _verticalSpeed
                };
            _simVars["PLANE HEADING DEGREES TRUE"] = unit =>
                unit switch
                {
                    "radians" => _heading * Math.PI / 180.0,
                    _ => _heading
                };
            _simVars["PLANE PITCH DEGREES"] = unit =>
                unit switch
                {
                    "degrees" => _planePitchDegrees,
                    _ => _planePitchDegrees
                };
            _simVars["TURN INDICATOR RATE"] = unit =>
                unit switch
                {
                    "radians" => _turnRateRadians,
                    _ => _turnRateRadians * 180.0 / Math.PI
                };
            _simVars["TURN COORDINATOR BALL"] = unit =>
                unit switch
                {
                    "degrees" => _turnBallDegrees,
                    _ => _turnBallPosition
                };
            _simVars["KOHLSMAN SETTING HG:1"] = unit => 29.92;
            _simVars["ENG MANIFOLD PRESSURE:1"] = unit => _manifoldPressure1;
            _simVars["ENG MANIFOLD PRESSURE:2"] = unit => _manifoldPressure2;
            
            Task.Run(async () =>
            {
                Console.WriteLine("[EMULATOR] Sending fake data");

                while (!_cts.Token.IsCancellationRequested)
                {
                    UpdatePlaneAltitude();
                    UpdatePlaneRoll();
                    UpdatePlanePitch();
                    UpdatePlaneRpms();
                    UpdatePlaneVerticalSpeed();
                    UpdatePlaneHeading();
                    UpdateTurnCoordinatorBall();
                    UpdateAirspeed();
                    UpdateTurnIndicatorRate();
                    UpdateManifoldPressure();

                    foreach (var info in callbacks)
                    {
                        var value = GetVarValue(info.name, info.unit);
                        if (value != null)
                        {
                            if (_watching != null && _watching == info.name)
                            {
                                if (_watchCount < 10)
                                {
                                    Console.WriteLine($"SimVar {_watching}: {value}");
                                    _watchCount++;
                                }
                                else 
                                {
                                    _watching = null;
                                    _watchCount = 0;
                                }
                            }
                            
                            info.callback(value);
                        }
                        else
                        {
                            Console.WriteLine($"Unknown SimVar: {info.name}");
                        }
                    }

                    await Task.Delay(10);
                }
            }, _cts.Token);
        }

        private object? GetVarValue(string name, string? unit)
        {
            object? result;

                if (_forcedVars.TryGetValue(name, out var forced))
                    result = forced;
                else if (_simVars.TryGetValue(name, out var generator))
                    result = generator(unit);
                else
                    return null;

                if (result is string s && double.TryParse(s, out var n))
                    result = n;

                return result;
        }

        public void ForceVarValue(string name, object value)
        {
            _forcedVars[name] = value;
        }

        public void ClearForcedVar(string name)
        {
            _forcedVars.Remove(name);
        }

        public void Disconnect() => _cts?.Cancel();

        private double _altitude = 1000;
        private bool _ascending = true;

        public void UpdatePlaneAltitude()
        {
            double step = 2;
            double minAlt = 1000;
            double maxAlt = 10000;

            if (_ascending)
            {
                _altitude += step;
                if (_altitude >= maxAlt)
                    _ascending = false;
            }
            else
            {
                _altitude -= step;
                if (_altitude <= minAlt)
                    _ascending = true;
            }
        }

        private double _planeBankRadians = 0;
        private bool _rollingLeft = true;
        private const double MaxBankAngle = Math.PI / 6;
        private const double RollSpeed = 0.001;

        public void UpdatePlaneRoll()
        {
            if (_rollingLeft)
            {
                _planeBankRadians -= RollSpeed;

                if (_planeBankRadians <= -MaxBankAngle)
                    _rollingLeft = false;
            }
            else
            {
                _planeBankRadians += RollSpeed;

                if (_planeBankRadians >= MaxBankAngle)
                    _rollingLeft = true;
            }
        }

        private double _planePitchDegrees = 0;
        private double _planePitchTime = 0;

        public void UpdatePlanePitch()
        {
            _planePitchTime += 0.005;
            _planePitchDegrees = Math.Sin(_planePitchTime) * 20;
        }

        private double _rpmTimeLeft = 0;
        private double _rpmTimeRight = 0;
        public double _rpmLeft = 0;
        public double _rpmRight = 0;

        public void UpdatePlaneRpms()
        {
            _rpmTimeLeft += 0.01;
            _rpmTimeRight += 0.0105;

            double minLeft = 500;
            double maxLeft = 2800;
            double minRight = 600;
            double maxRight = 2900;

            double waveLeft = Math.Sin(_rpmTimeLeft);
            double waveRight = Math.Sin(_rpmTimeRight);

            _rpmLeft = (maxLeft + minLeft) / 2 + (maxLeft - minLeft) / 2 * waveLeft;
            _rpmRight = (maxRight + minRight) / 2 + (maxRight - minRight) / 2 * waveRight;

            _rpmLeft += _rand.NextDouble() * 10 - 5;
            _rpmRight += _rand.NextDouble() * 10 - 5;
        }

        private double _verticalSpeedTime = 0;
        public double _verticalSpeed = 0;

        public void UpdatePlaneVerticalSpeed()
        {
            _verticalSpeedTime += 0.005;
            _verticalSpeed = Math.Sin(_verticalSpeedTime) * 33.3333;
        }

        private double _planeHeadingTime = 0;
        public double _heading = 0;

        public void UpdatePlaneHeading()
        {
            _planeHeadingTime += 0.005; // controls how fast the oscillation happens
            _heading = 180 + Math.Sin(_planeHeadingTime) * 90; // oscillates between 90° and 270°
        }

        private double _turnBallDegrees = 0;  // roll/slip angle
        private double _turnBallPosition = 0;    // -127 to 127
        private double _turnBallTime = 0;

        public void UpdateTurnCoordinatorBall()
        {
            _turnBallTime += 0.005;
            _turnBallDegrees = Math.Sin(_turnBallTime) * 10; // ±10 degrees slip/skid
            _turnBallPosition = Math.Sin(_turnBallTime) * 127; // ±127 position range
        }

        private double _airspeedKnots = 0;
        private double _airspeedPhase = 0;
        private double _minSpeed = 0;
        private double _maxSpeed = 0;
        private double _cycleDuration = 0;

        public void UpdateAirspeed(double deltaTime = 1.0 / 60.0) // assume ~60 FPS by default
        {
            // If we're starting or finished a full oscillation, pick a new random range + duration
            if (_cycleDuration <= 0 || _airspeedPhase >= 2 * Math.PI)
            {
                _minSpeed = 40 + _rand.NextDouble() * 20;   // 40–80 kt
                _maxSpeed = 120 + _rand.NextDouble() * 40;  // 120–160 kt
                _cycleDuration = 10 + _rand.NextDouble() * 10; // 10–20 seconds per full cycle
                _airspeedPhase = 0;
            }

            // Advance phase based on elapsed time and desired duration
            double speed = (2 * Math.PI) / _cycleDuration; // radians per second
            _airspeedPhase += speed * deltaTime;

            // Compute smooth oscillation between min and max
            double wave = (Math.Sin(_airspeedPhase) + 1) / 2; // 0–1 range
            _airspeedKnots = _minSpeed + (_maxSpeed - _minSpeed) * wave;
        }

        private double _turnRateTime = 0;
        public double _turnRateRadians = 0;

        public void UpdateTurnIndicatorRate()
        {
            _turnRateTime += 0.01;

            double maxTurnRate = 3 * Math.PI / 180.0;

            _turnRateRadians = Math.Sin(_turnRateTime) * maxTurnRate;
        }

        private double _manifoldTime1 = 0;
        private double _manifoldTime2 = 0;
        public double _manifoldPressure1 = 0;
        public double _manifoldPressure2 = 0;

        public void UpdateManifoldPressure()
        {
            _manifoldTime1 += 0.008;   // slightly different rates for realism
            _manifoldTime2 += 0.0095;

            double min1 = 10;
            double max1 = 35;
            double min2 = 10;
            double max2 = 35;

            double wave1 = Math.Sin(_manifoldTime1);
            double wave2 = Math.Sin(_manifoldTime2 + 0.3); // small phase offset

            // midpoint + half range * wave pattern
            _manifoldPressure1 = (max1 + min1) / 2 + (max1 - min1) / 2 * wave1;
            _manifoldPressure2 = (max2 + min2) / 2 + (max2 - min2) / 2 * wave2;

            // add light noise for realism
            _manifoldPressure1 += _rand.NextDouble() * 0.5 - 0.25;
            _manifoldPressure2 += _rand.NextDouble() * 0.5 - 0.25;
        }
    }
}
