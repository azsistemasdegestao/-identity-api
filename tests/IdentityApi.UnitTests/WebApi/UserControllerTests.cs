using FluentAssertions;
using IdentityApi.Application.DTOs.Common;
using IdentityApi.Application.DTOs.User;
using IdentityApi.Application.Services;
using IdentityApi.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace IdentityApi.UnitTests.WebApi;

public sealed class UserControllerTests
{
    private readonly Mock<IUserApplicationService> _userService = new();
    private readonly UserController _sut;

    private static readonly UserDto SampleUserDto = new(
        Guid.NewGuid(), "Alice Smith", "alice@example.com", new[] { "User" });

    public UserControllerTests()
    {
        _sut = new UserController(_userService.Object);
    }

    // ── GET /{id} ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_ExistingUser_Returns200WithDto()
    {
        _userService
            .Setup(s => s.GetByIdAsync(SampleUserDto.Id, default))
            .ReturnsAsync(ApiResponse<UserDto>.Ok(SampleUserDto));

        var result = await _sut.GetById(SampleUserDto.Id, default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var body = ok.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        body.Data!.Id.Should().Be(SampleUserDto.Id);
        body.Data.Name.Should().Be("Alice Smith");
    }

    [Fact]
    public async Task GetById_NonExistentUser_Returns404()
    {
        _userService
            .Setup(s => s.GetByIdAsync(It.IsAny<Guid>(), default))
            .ReturnsAsync(ApiResponse<UserDto>.Fail("User not found."));

        var result = await _sut.GetById(Guid.NewGuid(), default);

        var notFound = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFound.StatusCode.Should().Be(404);
        var body = notFound.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        body.Success.Should().BeFalse();
    }

    // ── POST /assign-role ─────────────────────────────────────────────────────

    [Fact]
    public async Task AssignRole_ValidRequest_Returns200WithUpdatedUser()
    {
        var updatedDto = SampleUserDto with { Roles = new[] { "User", "Admin" } };
        _userService
            .Setup(s => s.AssignRoleAsync(It.IsAny<AssignRoleRequest>(), default))
            .ReturnsAsync(ApiResponse<UserDto>.Ok(updatedDto));

        var result = await _sut.AssignRole(new AssignRoleRequest(SampleUserDto.Id, "Admin"), default);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        ok.StatusCode.Should().Be(200);
        var body = ok.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        body.Data!.Roles.Should().Contain("Admin");
    }

    [Fact]
    public async Task AssignRole_ServiceFailure_Returns400()
    {
        _userService
            .Setup(s => s.AssignRoleAsync(It.IsAny<AssignRoleRequest>(), default))
            .ReturnsAsync(ApiResponse<UserDto>.Fail("Failed to assign role."));

        var result = await _sut.AssignRole(new AssignRoleRequest(Guid.NewGuid(), "Admin"), default);

        var bad = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        bad.StatusCode.Should().Be(400);
        var body = bad.Value.Should().BeOfType<ApiResponse<UserDto>>().Subject;
        body.Success.Should().BeFalse();
    }

    [Fact]
    public async Task AssignRole_CallsServiceWithCorrectArguments()
    {
        var request = new AssignRoleRequest(SampleUserDto.Id, "Admin");
        _userService
            .Setup(s => s.AssignRoleAsync(request, default))
            .ReturnsAsync(ApiResponse<UserDto>.Ok(SampleUserDto));

        await _sut.AssignRole(request, default);

        _userService.Verify(s => s.AssignRoleAsync(
            It.Is<AssignRoleRequest>(r => r.UserId == SampleUserDto.Id && r.Role == "Admin"),
            default), Times.Once);
    }

    [Fact]
    public async Task GetById_CallsServiceWithCorrectId()
    {
        var id = Guid.NewGuid();
        _userService
            .Setup(s => s.GetByIdAsync(id, default))
            .ReturnsAsync(ApiResponse<UserDto>.Ok(SampleUserDto));

        await _sut.GetById(id, default);

        _userService.Verify(s => s.GetByIdAsync(id, default), Times.Once);
    }
}
