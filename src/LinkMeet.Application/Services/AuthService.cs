using LinkMeet.Application.DTOs;
using LinkMeet.Application.Interfaces;
using LinkMeet.Domain.Entities;
using LinkMeet.Domain.Enums;
using LinkMeet.Domain.Interfaces;

namespace LinkMeet.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly ITokenService _tokenService;

    public AuthService(IUserRepository userRepo, ITokenService tokenService)
    {
        _userRepo = userRepo;
        _tokenService = tokenService;
    }

    public async Task<AuthResponseDto> RegisterAsync(RegisterDto dto)
    {
        if (dto.Password != dto.ConfirmPassword)
            throw new Exception("Passwords do not match");

        if (await _userRepo.EmailExistsAsync(dto.Email))
            throw new Exception("Email already exists");

        var user = new User
        {
            Email = dto.Email.ToLower().Trim(),
            DisplayName = dto.DisplayName.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = UserRole.Participant
        };

        await _userRepo.CreateAsync(user);

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return new AuthResponseDto
        {
            Token = token,
            User = MapUser(user)
        };
    }

    public async Task<AuthResponseDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepo.GetByEmailAsync(dto.Email.ToLower().Trim());
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
            throw new Exception("Invalid email or password");

        var token = _tokenService.GenerateToken(user.Id, user.Email, user.Role.ToString());

        return new AuthResponseDto
        {
            Token = token,
            User = MapUser(user)
        };
    }

    private static UserDto MapUser(User u) => new()
    {
        Id = u.Id,
        Email = u.Email,
        DisplayName = u.DisplayName,
        Role = u.Role.ToString(),
        AvatarUrl = u.AvatarUrl
    };
}
