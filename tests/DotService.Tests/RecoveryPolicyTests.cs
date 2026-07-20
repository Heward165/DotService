using DotService.Packaging;

namespace DotService.Tests;

public sealed class RecoveryPolicyTests
{
    [Fact]
    public void RestartCreatesBoundedIndependentActions()
    {
        WindowsServiceRecoveryPolicy policy = WindowsServiceRecoveryPolicy.Restart(
            attempts: 3,
            delay: TimeSpan.FromSeconds(15));

        Assert.Equal(3, policy.Actions.Count);
        Assert.All(policy.Actions, action =>
        {
            Assert.Equal(WindowsServiceRecoveryActionType.Restart, action.Type);
            Assert.Equal(TimeSpan.FromSeconds(15), action.Delay);
        });
    }

    [Theory]
    [InlineData(0)]
    [InlineData(17)]
    public void RestartRejectsUnboundedAttemptCounts(int attempts)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => WindowsServiceRecoveryPolicy.Restart(attempts));
    }
}

