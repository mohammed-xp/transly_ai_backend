using Microsoft.AspNetCore.Mvc;
using TranslyAI.Api.Dtos;
using TranslyAI.Api.Services;

namespace TranslyAI.Api.Controllers;

[ApiController]
[Route("v1/[controller]")]
public class AuthController(AuthService authService) : ControllerBase
{

    [HttpPost("register")]
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
                Title = "Email Already registered."
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
}