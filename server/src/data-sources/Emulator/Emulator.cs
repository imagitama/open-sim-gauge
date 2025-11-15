using OpenGaugeAbstractions;

public class EmulatorDataSource : DataSourceBase
{
    private readonly string[] _aircraftTitles =
    {
            "Cessna Skyhawk",
            "Piper PA44"
        };
    private Action<string>? _vehicleCallback;
    private readonly Random _rand = new();
    private readonly Dictionary<string, Func<string?, object>> _simVars = [];
    private readonly Dictionary<string, object> _forcedVars = [];
    private CancellationTokenSource _cts = null!;
    private string? _watching = null;
    private int _watchCount = 0;

    public class CallbackInfo
    {
        public required string name;
        public required string unit;
        public required Action<object> callback;
    }

    private readonly List<CallbackInfo> _simVarCallbacks = [];

    public EmulatorDataSource(Config config)
    {
        Name = "Emulator";
    }

    public override void WatchVar(string varName)
    {
        _watching = varName;
    }

    public bool GetIsSubscribed(string varName, string unit)
    {
        return _simVarCallbacks.Any(cb => cb.name == varName && cb.unit == unit);
    }

    public override void SubscribeToVar(string varName, string unit, Action<object> callback)
    {
        if (GetIsSubscribed(varName, unit))
            return;

        _simVarCallbacks.Add(new CallbackInfo
        {
            name = varName,
            unit = unit,
            callback = callback
        });

        Console.WriteLine($"[EMULATOR] Subscribed to var '{varName}' ({unit})");
    }

    public override void UnsubscribeFromVar(string name, string unit)
    {
        // TODO
        // Console.WriteLine($"[EMULATOR] Unsubscribed from var '{name}' ({unit})");
    }

    public override void SubscribeToVehicle(Action<string> callback)
    {
        _vehicleCallback = callback;
        Console.WriteLine($"[EMULATOR] Subscribed to vehicle change");
    }

    public override void Listen(Config config)
    {
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
            var remainingTitles = new List<string>(_aircraftTitles);

            while (!_cts.Token.IsCancellationRequested)
            {
                if (remainingTitles.Count == 0)
                    remainingTitles = new List<string>(_aircraftTitles);

                int index = _rand.Next(remainingTitles.Count);
                var newName = remainingTitles[index];

                if (newName != CurrentVehicleName)
                {
                    CurrentVehicleName = newName;

                    Console.WriteLine($"[EMULATOR] New vehicle: {CurrentVehicleName}");

                    _vehicleCallback?.Invoke(CurrentVehicleName);
                }

                remainingTitles.RemoveAt(index);

                try
                {
                    await Task.Delay(TimeSpan.FromSeconds(5), _cts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }
            }
        }, _cts.Token);

        Task.Run(async () =>
        {
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

                await Task.Delay(10);
            }
        }, _cts.Token);

        Task.Run(async () =>
        {
            Console.WriteLine($"[EMULATOR] Sending fake data at rate {(int)config.Rate}ms");

            while (!_cts.Token.IsCancellationRequested)
            {
                foreach (var info in _simVarCallbacks)
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

                await Task.Delay((int)config.Rate);
            }
        }, _cts.Token);
    }

    public override void Connect()
    {
        IsConnected = true;
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

    public override void Disconnect() => _cts?.Cancel();

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
        _planeHeadingTime += 0.005;
        _heading = 180 + Math.Sin(_planeHeadingTime) * 90; // oscillates between 90->270
    }

    private double _turnBallDegrees = 0; // roll/slip angle
    private double _turnBallPosition = 0; // -1 to 1
    private double _turnBallTime = 0;

    public void UpdateTurnCoordinatorBall()
    {
        _turnBallTime += 0.005;
        _turnBallDegrees = Math.Sin(_turnBallTime) * 10; // ±10 degrees slip/skid
        _turnBallPosition = Math.Sin(_turnBallTime);     // ±1 position range
    }

    private double _airspeedKnots = 0;
    private double _airspeedPhase = 0;
    private double _minSpeed = 0;
    private double _maxSpeed = 0;
    private double _cycleDuration = 0;

    public void UpdateAirspeed(double deltaTime = 1.0 / 60.0) // 60 fps
    {
        if (_cycleDuration <= 0 || _airspeedPhase >= 2 * Math.PI)
        {
            _minSpeed = 40 + _rand.NextDouble() * 20;   // 40–80 kt
            _maxSpeed = 120 + _rand.NextDouble() * 40;  // 120–160 kt
            _cycleDuration = 10 + _rand.NextDouble() * 10; // 10–20 seconds per full cycle
            _airspeedPhase = 0;
        }

        double speed = (2 * Math.PI) / _cycleDuration; // radians per second
        _airspeedPhase += speed * deltaTime;

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
        _manifoldTime1 += 0.001;
        _manifoldTime2 += 0.0012;

        double min = 10;
        double max = 35;

        double wave1 = Math.Sin(_manifoldTime1);
        double wave2 = Math.Sin(_manifoldTime2 + 0.3);

        double noise1 = (_rand.NextDouble() - 0.5) * 0.1;
        double noise2 = (_rand.NextDouble() - 0.5) * 0.1;

        _manifoldPressure1 = 0.98 * _manifoldPressure1 +
                            0.02 * ((max + min) / 2 + (max - min) / 2 * wave1 + noise1);
        _manifoldPressure2 = 0.98 * _manifoldPressure2 +
                            0.02 * ((max + min) / 2 + (max - min) / 2 * wave2 + noise2);
    }
}
