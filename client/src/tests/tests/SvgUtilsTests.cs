using OpenGaugeClient;

public class SvgUtilsTests
{
    [Test]
    [DisplayName("GetPathPosition (inside 400x400 with a single line) with value '$value' => $expectedX,$expectedY")]
    [Arguments(-1.0, 100, 200)]
    [Arguments(0.0, 200, 200)]
    [Arguments(1.0, 300, 200)]
    public async Task GetPathPosition_Works(
        double value,
        float expectedX,
        float expectedY)
    {
        var pathConfig = new PathConfig
        {
            Var = new SimVarConfig
            {
                Name = "TURN_BALL_COORDINATOR",
                Unit = "position"
            },
            Image = "my/image.svg",
            Width = 400,
            Height = 400
        };

        var svgCache = new SvgCache();
        var svgPath = Path.Combine(AppContext.BaseDirectory, "single-path-line.svg");

        var result = SvgUtils.GetPathPosition(
            svgCache,
            svgPath,
            pathConfig,
            400,
            400,
            value,
            false
        );

        await Assert.That(result.X).IsEqualTo(expectedX);
        await Assert.That(result.Y).IsEqualTo(expectedY);
    }
}
