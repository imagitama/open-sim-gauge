using OpenGaugeClient;
using TUnit.Assertions.Exceptions;

public class FlexibleVector2Tests
{
    [Test]
    [DisplayName("Does FlexibleVector2 $inputX,$inputY resolve to $expectedX,$expectedY")]
    [Arguments("50%", "50%", 500, 500)]
    [Arguments("100", "100", 100, 100)]
    [Arguments("-100", "-100", 900, 900)]
    public async Task FlexibleVector2Resolve_ResolvesGood(
        object inputX,
        object inputY,
        float expectedX,
        float expectedY)
    {
        var vector2 = new FlexibleVector2()
        {
            X = inputX,
            Y = inputY
        };

        var (resultX, resultY) = vector2.Resolve(1000, 1000);

        await Assert.That(resultX).IsEqualTo(expectedX);
        await Assert.That(resultY).IsEqualTo(expectedY);
    }

    [Test]
    [DisplayName("Does FlexibleVector2 $inputX,$inputY resolve to $expectedX,$expectedY - by throwing")]
    [Arguments("50%", "50%", 1234, 1234)]
    public async Task FlexibleVector2Resolve_ResolvesBad(
        object inputX,
        object inputY,
        float expectedX,
        float expectedY)
    {
        var vector2 = new FlexibleVector2()
        {
            X = inputX,
            Y = inputY
        };

        await Assert.ThrowsAsync<AssertionException>(async () =>
            {
                var (x, y) = vector2.Resolve(1000, 1000);

                await Assert.That(x).IsEqualTo(expectedX);
                await Assert.That(y).IsEqualTo(expectedY);
            });
    }
}
