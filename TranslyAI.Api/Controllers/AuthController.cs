using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Extensions;
using TranslyAI.Api.Services;

namespace TranslyAI.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class AuthController(AuthService authService, JwtTokenService jwtTokenService) : ControllerBase
{

    [HttpPost("register")]
    [ProducesResponseType<RegisterResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RegisterResponse>> Register(
        RegisterRequest request,
        CancellationToken cancellationToken)
    {
        var user = await authService.RegisterAsync(
            request.Email,
            request.UserName,
            request.Password,
            cancellationToken
        );

        if (user is null)
        {
            return Conflict(new ProblemDetails
            {
                Status = StatusCodes.Status409Conflict,
                Title = "Email already registered."
            });
        }

        var response = new RegisterResponse
        {
            Id = user.Id,
            Email = user.Email,
            UserName = user.UserName,
            CreatedAt = new DateTimeOffset(user.CreatedAtUtc, TimeSpan.Zero),
        };

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpPost("login")]
    [ProducesResponseType<LoginResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<LoginResponse>> Login(
        LoginRequest request,
        CancellationToken cancellationToken
    )
    {
        var user = await authService.LoginAsync(
            request.Email,
            request.Password,
            cancellationToken);

        if (user is null)
        {
            return Unauthorized(new ProblemDetails
            {
                Status = StatusCodes.Status401Unauthorized,
                Title = "Invalid email or password."
            });
        }

        var (token, expiresAt) = jwtTokenService.CreateAccessToken(user);

        return Ok(new LoginResponse
        {
            AccessToken = token,
            TokenType = "Bearer",
            ExpiresAt = expiresAt
        });
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType<MeResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public ActionResult<MeResponse> Me()
    {
        var userId = User.GetUserId();

        if (userId is null)
        {
            return Unauthorized();
        }

        return Ok(new MeResponse { Id = userId.Value });
    }
}