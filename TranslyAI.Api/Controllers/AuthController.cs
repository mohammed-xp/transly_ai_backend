using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Extensions;
using TranslyAI.Api.Services;
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
        try
        {
            if (request == null)
            {
                return BadRequest(ApiResponse<object>.BadRequest("Registration data is required"));
            }

            if (await authService.IsEmailExistsAsync(request.Email, cancellationToken))
            {
                return Conflict(ApiResponse<object>.Conflict($"User with email '{request.Email}' already exists"));
            }

            var userDto = await authService.RegisterAsync(
            request,
            cancellationToken
        );

            if (userDto is null)
            {
                return Conflict(ApiResponse<object>.Conflict($"User with email '{request.Email}' already exists"));
            }
            var response = ApiResponse<UserDto>.CreatedAt(userDto, "User registered successfully");
            return CreatedAtAction(nameof(Register), response);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<object>.Error(500, "An error occurred during register", ex.Message);
            return StatusCode(500, errorResponse);
        }
    }

    [HttpPost("login")]
    [ProducesResponseType<ApiResponse<LoginResponseDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login(
        LoginRequestDto request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            if (request == null)
            {
                return BadRequest(ApiResponse<object>.BadRequest("Login data is required"));
            }
            var loginResponse = await authService.LoginAsync(
                request,
                cancellationToken);

            if (loginResponse is null)
            {
                return Unauthorized();

            }

            var response = ApiResponse<LoginResponseDto>.Ok(loginResponse, "User logged in successfully");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<object>.Error(500, "An error occurred during login", ex.Message);
            return StatusCode(500, errorResponse);
        }
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<ApiResponse<UserDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ApiResponse<object>>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me(CancellationToken cancellationToken)
    {
        try
        {
            var userId = User.GetUserId();

            if (userId is null)
            {
                return Unauthorized();
            }

            var profile = await authService.GetProfileAsync(userId.Value, cancellationToken);

            if (profile is null)
            {
                return Unauthorized();
            }

            var response = ApiResponse<UserDto>.Ok(profile, "User received successfully");

            return Ok(response);
        }
        catch (Exception ex)
        {
            var errorResponse = ApiResponse<object>.Error(500, "An error occurred during received user", ex.Message);
            return StatusCode(500, errorResponse);
        }
    }
}