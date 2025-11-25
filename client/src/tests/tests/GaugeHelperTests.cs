using OpenGaugeClient;

public class GaugeHelperTests
{
    [Test]
    [DisplayName("MapSimVarValueToOffset Basic $value => $expectedValue")]
    [Arguments(null, 0)]
    [Arguments(0, 0)]
    [Arguments(1, 1)]
    [Arguments(123.456, 123.456)]
    public async Task MapSimVarValueToOffset_Basic(
        double? value,
        double expectedValue
    )
    {
        var config = new TransformConfig()
        {
            Var = new SimVarConfig() { Name = "My SimVar", Unit = "some_unit" },
        };

        var result = GaugeHelper.MapSimVarValueToOffset(config, value);

        await Assert.That(result).IsEqualTo(expectedValue);
    }

    [Test]
    [DisplayName("MapSimVarValueToOffset Multiply $value => $expectedValue (multiply=$multiply)")]
    [Arguments(123.456, null, 123.456)]
    [Arguments(123.456, 100, 12345.6)]
    public async Task MapSimVarValueToOffset_Multiply(
    double? value,
    double? multiply,
    double expectedValue)
    {
        var config = new TransformConfig()
        {
            Var = new SimVarConfig() { Name = "My SimVar", Unit = "some_unit" },
            Multiply = multiply,
        };

        var result = GaugeHelper.MapSimVarValueToOffset(config, value);

        await Assert.That(result).IsEqualTo(expectedValue);
    }

    [Test]
    [DisplayName("MapSimVarValueToOffset Invert $value => $expectedValue (invert=$invert)")]
    [Arguments(123.456, true, -123.456)]
    public async Task MapSimVarValueToOffset_Invert(
    double? value,
    bool? invert,
    double expectedValue)
    {
        var config = new TransformConfig()
        {
            Var = new SimVarConfig() { Name = "My SimVar", Unit = "some_unit" },
            Invert = invert
        };

        var result = GaugeHelper.MapSimVarValueToOffset(config, value);

        await Assert.That(result).IsEqualTo(expectedValue);
    }

    // [Test]
    // [DisplayName("MapSimVarValueToOffset $value => $expectedValue (calibration=$invert)")]
    // [Arguments(123.456, 12345.6, true)]
    // public async Task MapSimVarValueToOffset_Calibration(
    // double? value,
    // double expectedValue,
    // )
    // {
    //     var config = new TransformConfig()
    //     {
    //         Var = new SimVarConfig() { Name = "My SimVar", Unit = "some_unit" },
    //         Calibration = ...
    //     };

    //     var result = GaugeHelper.MapSimVarValueToOffset(config, value);

    //     await Assert.That(result).IsEqualTo(expectedValue);
    // }

    // [Test]
    // [DisplayName("MapSimVarValueToOffset $value => $expectedValue (invert=$invert)")]
    // [Arguments(180, Math.PI)]
    // public async Task MapSimVarValueToOffset_Radians(
    // double? value,
    // double expectedValue)
    // {
    //     var config = new TransformConfig()
    //     {
    //         Var = new SimVarConfig() { Name = "My SimVar", Unit = "some_unit" },
    //     };

    //     var result = GaugeHelper.MapSimVarValueToOffset(config, value);

    //     await Assert.That(result).IsEqualTo(expectedValue);
    // }

    [Test]
    [DisplayName("MapSimVarValueToOffset Normalize $value => $expectedValue (min=$min max=$max from=$from to=$to)")]
    // default to assume user knows what they are doing
    [Arguments(/*value*/0, /*min*/null, /*max*/null, /*from*/null, /*to*/null, /*result*/0)]
    [Arguments(/*value*/1, /*min*/null, /*max*/null, /*from*/null, /*to*/null, /*result*/1)]
    // simple floats
    [Arguments(/*value*/0, /*min*/0, /*max*/1, /*from*/0, /*to*/180, /*result*/0)]
    [Arguments(/*value*/1, /*min*/0, /*max*/1, /*from*/0, /*to*/180, /*result*/180)]
    public async Task MapSimVarValueToOffset_Normalize(
        double? value,
        double? min,
        double? max,
        double? from,
        double? to,
        double expectedValue)
    {
        var config = new TransformConfig()
        {
            Var = new SimVarConfig() { Name = "My SimVar", Unit = "some_unit" },
            Min = min,
            Max = max,
            From = from,
            To = to
        };

        var result = GaugeHelper.MapSimVarValueToOffset(config, value);

        await Assert.That(result).IsEqualTo(expectedValue);
    }

    [Test]
    [DisplayName("MapSimVarValueToOffset Wrap $value => $expectedValue (min=$min max=$max from=$from to=$to, wrap=$wrap)")]
    [Arguments(/*value*/0, /*min*/0, /*max*/1, /*from*/0, /*to*/180, /*wrap*/ true, /*result*/0)]
    [Arguments(/*value*/1, /*min*/0, /*max*/1, /*from*/0, /*to*/180, /*wrap*/ false, /*result*/180)]
    [Arguments(/*value*/1, /*min*/0, /*max*/1, /*from*/0, /*to*/180, /*wrap*/ true, /*result*/180)]
    [Arguments(/*value*/1.5, /*min*/0, /*max*/1, /*from*/0, /*to*/180, /*wrap*/ true, /*result*/90)]
    public async Task MapSimVarValueToOffset_Wrap(
        double? value,
        double? min,
        double? max,
        double? from,
        double? to,
        bool wrap,
        double expectedValue)
    {
        var config = new RotateConfig()
        {
            Var = new SimVarConfig() { Name = "My SimVar", Unit = "some_unit" },
            Min = min,
            Max = max,
            From = from,
            To = to,
            Wrap = wrap
        };

        var result = GaugeHelper.MapSimVarValueToOffset(config, value);

        await Assert.That(result).IsEqualTo(expectedValue);
    }
}
