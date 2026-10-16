using FluentAssertions;

namespace StudioBooking.Domain.UnitTests;

public class ArchitectureSetupTests
{
    [Fact]
    public void Architecture_SolutionSetup_ShouldBeConfiguredProperly()
    {
        var isConfigured = true;
        isConfigured.Should().BeTrue();
    }
}
