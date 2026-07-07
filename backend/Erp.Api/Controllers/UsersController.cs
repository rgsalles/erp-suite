using Erp.Api.Data;
using Erp.Api.Dtos;
using Erp.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Erp.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Admin")]
public sealed class UsersController(ErpDbContext db) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyCollection<UserSummaryDto>>> Get()
    {
        var users = await db.Users
            .AsNoTracking()
            .OrderBy(x => x.FullName)
            .Select(x => new UserSummaryDto(x.Id, x.FullName, x.Email, x.Role, x.IsActive))
            .ToListAsync();

        return Ok(users);
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<UserSummaryDto>> Update(Guid id, UpdateUserRequest request)
    {
        var user = await db.Users.FirstOrDefaultAsync(x => x.Id == id);

        if (user is null)
        {
            return NotFound();
        }

        var email = NormalizeEmail(request.Email);
        var emailExists = await db.Users.AnyAsync(x => x.Id != id && x.Email == email);

        if (emailExists)
        {
            return Conflict("This email is already registered.");
        }

        user.FullName = request.FullName.Trim();
        user.Email = email;
        user.Role = request.Role;
        user.IsActive = request.IsActive;
        user.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync();

        return Ok(new UserSummaryDto(user.Id, user.FullName, user.Email, user.Role, user.IsActive));
    }

    private static string NormalizeEmail(string email)
    {
        return email.Trim().ToLowerInvariant();
    }
}
