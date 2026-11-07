using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TeamTaskManager.Api.Contracts;
using TeamTaskManager.Application.Authentication;

namespace TeamTaskManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthenticationService authenticationService) : ControllerBase
{
    [AllowAnonymous]
    [HttpPost("login")]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LoginResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var result = await authenticationService.LoginAsync(request, cancellationToken);
        if (result is null)
        {
            return Unauthorized(ApiResponse<LoginResponse>.Fail("E-posta veya parola hatalı."));
        }

        return Ok(ApiResponse<LoginResponse>.Ok(result, "Giriş başarılı."));
    }

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<LoginResponse>>> Register([FromBody] RegistrationRequest request, CancellationToken cancellationToken)
    {
        var result = await authenticationService.RegisterAsync(request, cancellationToken);
        return !result.Succeeded || result.Data is null
            ? BadRequest(ApiResponse<LoginResponse>.Fail("Kayıt oluşturulamadı.", result.Errors.ToArray()))
            : StatusCode(StatusCodes.Status201Created, ApiResponse<LoginResponse>.Ok(result.Data, "Hesabın oluşturuldu. Projelere katılım isteği gönderebilirsin."));
    }

    [Authorize]
    [HttpGet("me")]
    [ProducesResponseType(typeof(ApiResponse<AuthenticatedUser>), StatusCodes.Status200OK)]
    public async Task<ActionResult<ApiResponse<AuthenticatedUser>>> Me(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<AuthenticatedUser>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        }

        var user = await authenticationService.GetUserAsync(userId, cancellationToken);
        if (user is null)
        {
            return Unauthorized(ApiResponse<AuthenticatedUser>.Fail("Kullanıcı bulunamadı veya pasif durumda."));
        }

        return Ok(ApiResponse<AuthenticatedUser>.Ok(user, "Oturum kullanıcısı getirildi."));
    }

    [Authorize]
    [HttpPost("change-password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId))
        {
            return Unauthorized(ApiResponse<object>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        }

        var errors = await authenticationService.ChangePasswordAsync(userId, request, cancellationToken);
        if (errors.Count > 0)
        {
            return BadRequest(ApiResponse<object>.Fail("Parola değiştirilemedi.", errors.ToArray()));
        }

        return NoContent();
    }

    [Authorize, HttpGet("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> Profile(CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(ApiResponse<UserProfileDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var profile = await authenticationService.GetProfileAsync(userId, cancellationToken);
        return profile is null ? NotFound(ApiResponse<UserProfileDto>.Fail("Profil bulunamadı.")) : Ok(ApiResponse<UserProfileDto>.Ok(profile, "Profil getirildi."));
    }

    [Authorize, HttpPut("profile")]
    public async Task<ActionResult<ApiResponse<UserProfileDto>>> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken cancellationToken)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrWhiteSpace(userId)) return Unauthorized(ApiResponse<UserProfileDto>.Fail("Geçerli kullanıcı bilgisi bulunamadı."));
        var result = await authenticationService.UpdateProfileAsync(userId, request, cancellationToken);
        return !result.Succeeded || result.Data is null ? BadRequest(ApiResponse<UserProfileDto>.Fail("Profil güncellenemedi.", result.Errors.ToArray())) : Ok(ApiResponse<UserProfileDto>.Ok(result.Data, "Profil güncellendi."));
    }
}
