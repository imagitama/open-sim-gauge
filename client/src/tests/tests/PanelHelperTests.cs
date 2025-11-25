using OpenGaugeClient;

public class PanelHelperTests
{
    [Test]
    [DisplayName("Should panel be visible: vehicles [$vehicleNames] vs. actual $actualVehicleName => $expectedResult")]
    // exact matches
    [Arguments(new string[] { "Cessna 172" }, "Cessna 172", true)]
    [Arguments(new string[] { "Cessna 172" }, "PA44", false)]
    // loose
    [Arguments(new string[] { "Cessna" }, "Cessna 172", false)]
    [Arguments(new string[] { "*Cessna*" }, "Cessna 172", true)]
    [Arguments(new string[] { "Cessna*" }, "Cessna 172", true)]
    // no actual
    [Arguments(new string[] { "*Cessna*" }, null, false)]
    [Arguments(new string[] { "Cessna" }, null, false)]
    // no panel vehicle
    [Arguments(null, null, false)]
    // empty vehicles = all
    [Arguments(new string[0], null, false)]
    [Arguments(new string[0], "Cessna 172", true)]
    [Arguments(null, "Cessna 172", true)]
    // multiple
    [Arguments(new string[] { "Cessna 172", "PA44" }, "Cessna 172", true)]
    // multiple with include/exclude
    [Arguments(new string[] { "*Cessna*", "PA44" }, "Cessna 172", true)]
    [Arguments(new string[] { "!*Cessna*", "PA44" }, "Cessna 172", false)]
    [Arguments(new string[] { "!*Cessna*" }, "Cessna 172", false)]
    [Arguments(new string[] { "*", "!*Cessna*" }, "Cessna 172", false)]
    [Arguments(new string[] { "*", "!*Cessna*" }, "PA44", true)]
    public async Task GetIsPanelVisibleTest(
        string[]? vehicleNames,
        string? actualVehicleName,
        bool expectedResult)
    {
        var panel = new Panel()
        {
            Name = "Test Panel",
            Vehicle = vehicleNames != null ? vehicleNames.ToList() : [],
            Gauges = []
        };

        var result = PanelHelper.GetIsPanelVisible(panel, actualVehicleName);

        await Assert.That(result).IsEqualTo(expectedResult);
    }
}
