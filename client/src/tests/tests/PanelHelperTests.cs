using OpenGaugeClient;

public class PanelHelperTests
{
    [Test]
    [DisplayName("Should panel be visible: vehicle $vehicleName vs. actual $actualVehicleName should be $expectedResult")]
    [Arguments("Cessna 172", "Cessna 172", true)]
    [Arguments("Cessna 172", "PA44", false)]
    [Arguments("Cessna", "Cessna 172", false)]
    [Arguments("*Cessna*", "Cessna 172", true)]
    [Arguments("Cessna*", "Cessna 172", true)]
    [Arguments("*Cessna*", null, false)]
    [Arguments("Cessna", null, false)]
    [Arguments(null, null, false)]
    [Arguments(null, "Cessna 172", true)]
    public async Task GetIsPanelVisibleTest(
        string? vehicleName,
        string? actualVehicleName,
        bool expectedResult)
    {
        var panel = new Panel()
        {
            Name = "Test Panel",
            Vehicle = vehicleName,
            Gauges = []
        };

        var result = PanelHelper.GetIsPanelVisible(panel, actualVehicleName);

        await Assert.That(result).IsEqualTo(expectedResult);
    }
}
