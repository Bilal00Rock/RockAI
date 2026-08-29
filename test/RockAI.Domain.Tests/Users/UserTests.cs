using FluentAssertions;
using RockAI.Common.Tests.Builders;
using RockAI.Domain.Users;

namespace RockAI.Domain.Tests.Users;

public sealed class UserTests
{
    [Fact]
    public void AddRole_WhenRoleAlreadyExists_ReturnsConflict()
    {
        var user = new UserBuilder().WithRoles(UserRole.User).Build();

        var result = user.AddRole(UserRole.User);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.RoleAlreadyAssigned);
    }

    [Fact]
    public void RemoveRole_WhenItIsTheOnlyRole_PreservesLastRoleInvariant()
    {
        var user = new UserBuilder().WithRoles(UserRole.User).Build();

        var result = user.RemoveRole(UserRole.User);

        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(UserErrors.LastRole);
        user.HasRole(UserRole.User).Should().BeTrue();
    }

    [Fact]
    public void Constructor_WhenEmailIsBlank_ThrowsArgumentException()
    {
        var action = () => new UserBuilder().WithEmail(" ").Build();

        action.Should().Throw<ArgumentException>()
            .Which.ParamName.Should().Be("email");
    }
}
