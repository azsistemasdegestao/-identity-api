using IdentityApi.Application.DTOs.User;
using IdentityApi.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace IdentityApi.WebApi.Controllers;

[ApiController]
[Route("api/users")]
[Authorize]
public sealed class UserController : ControllerBase
{
    private readonly IUserApplicationService _userService;

    public UserController(IUserApplicationService userService)
    {
        _userService = userService;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id, CancellationToken ct)
    {
        var result = await _userService.GetByIdAsync(id, ct);
        if (!result.Success)
            return NotFound(result);
        return Ok(result);
    }

    [HttpPost("assign-role")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AssignRole([FromBody] AssignRoleRequest request, CancellationToken ct)
    {
        var result = await _userService.AssignRoleAsync(request, ct);
        if (!result.Success)
            return BadRequest(result);
        return Ok(result);
    }
}
