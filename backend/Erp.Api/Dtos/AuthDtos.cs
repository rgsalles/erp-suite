using System.ComponentModel.DataAnnotations;
using Erp.Api.Models;

namespace Erp.Api.Dtos;

public sealed record RegisterRequest(
    [Required, MaxLength(160)] string FullName,
    [Required, EmailAddress, MaxLength(200)] string Email,
    [Required, MinLength(6), MaxLength(80)] string Password,
    UserRole? Role);

public sealed record LoginRequest(
    [Required, EmailAddress] string Email,
    [Required] string Password);

public sealed record AuthResponse(
    string Token,
    DateTime ExpiresAt,
    UserSummaryDto User);

public sealed record UserSummaryDto(
    Guid Id,
    string FullName,
    string Email,
    UserRole Role,
    bool IsActive);

public sealed record UpdateUserRequest(
    [Required, MaxLength(160)] string FullName,
    UserRole Role,
    bool IsActive);
