using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Extensions;
using TranslyAI.Api.Services.IServices;

namespace TranslyAI.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{

    [HttpPost("register")]
    [ProducesResponseType<ApiResponse<UserDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<UserDto>>> Register(
        RegisterRequestDto request,
        CancellationToken cancellationToken)
    {
        var userDto = await authService.RegisterAsync(
        request,
        cancellationToken
    );

        if (userDto is null)
        {
            return this.ConflictProblem($"User with email '{request.Email}' already exists");
        }
        // var response = ApiResponse<UserDto>.CreatedAt(userDto, "User registered successfully");
        // return CreatedAtAction(nameof(Register), response);
        return StatusCode(
        StatusCodes.Status201Created,
        ApiResponse<UserDto>.CreatedAt(userDto, "User registered successfully"));
    }

    [HttpPost("login")]
    [ProducesResponseType<ApiResponse<LoginResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        LoginRequestDto request,
        CancellationToken cancellationToken
    )
    {
        var loginResponse = await authService.LoginAsync(
            request,
            cancellationToken);

        if (loginResponse is null)
        {
            return this.UnauthorizedProblem("Email or password is incorrect");
        }

        return Ok(ApiResponse<LoginResponseDto>.Ok(loginResponse, "User logged in successfully"));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<ApiResponse<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me(CancellationToken cancellationToken)
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return this.UnauthorizedProblem("The access token is invalid");
        }

        var profile = await authService.GetProfileAsync(userId.Value, cancellationToken);

        if (profile is null)
        {
            return this.UnauthorizedProblem("The access token is invalid");
        }

        var response = ApiResponse<UserDto>.Ok(profile, "User received successfully");

        return Ok(response);
    }
}
