using Microsoft.EntityFrameworkCore;
using Project_Bloodwave_Backend.Data;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Models;

namespace Project_Bloodwave_Backend.Services;

public interface IGameCrudService
{
    Task<List<AchievmentDto>> GetAchievmentsAsync();
    Task<AchievmentDto?> GetAchievmentByIdAsync(int achievmentId);
    Task<AchievmentDto> CreateAchievmentAsync(AchievmentDto dto);
    Task<AchievmentDto?> UpdateAchievmentAsync(int achievmentId, AchievmentDto dto);
    Task<bool> DeleteAchievmentAsync(int achievmentId);
    Task<List<UserAchievmentDto>> GetUserAchievmentsAsync(int userId);
    Task<UserAchievmentDto?> UnlockAchievmentAsync(int userId, int achievmentId);

    Task<List<ItemDto>> GetItemsAsync();
    Task<ItemDto?> GetItemByIdAsync(int itemId);
    Task<ItemDto> CreateItemAsync(ItemUpsertDto dto);
    Task<ItemDto?> UpdateItemAsync(int itemId, ItemUpsertDto dto);
    Task<bool> DeleteItemAsync(int itemId);

    Task<List<WeaponDto>> GetWeaponsAsync();
    Task<WeaponDto?> GetWeaponByIdAsync(int weaponId);
    Task<WeaponDto> CreateWeaponAsync(WeaponUpsertDto dto);
    Task<WeaponDto?> UpdateWeaponAsync(int weaponId, WeaponUpsertDto dto);
    Task<bool> DeleteWeaponAsync(int weaponId);

    Task<List<MatchDto>> GetAllMatchesAsync();
    Task<List<MatchDto>> GetMatchesByUserAsync(int userId);
    Task<MatchDto?> GetMatchByIdAsync(int userId, int matchId);
    Task<MatchDto> CreateMatchAsync(int userId, CreateMatchDto dto);
    Task<MatchDto?> UpdateMatchAsync(int userId, int matchId, UpdateMatchDto dto);
    Task<bool> DeleteMatchAsync(int userId, int matchId, bool canDeleteAny = false);

    Task<UserDto?> GetUserByIdAsync(int userId);
    Task<UserDto?> UpdateUserAsync(int userId, UpdateUserDto dto);
    Task<bool> DeleteUserAsync(int userId);
}

public class GameCrudService : IGameCrudService
{
    private readonly BloodwaveDbContext _context;
    private readonly IMailService _mailService;
    private readonly ILogger<GameCrudService> _logger;

    public GameCrudService(
        BloodwaveDbContext context,
        IMailService mailService,
        ILogger<GameCrudService> logger)
    {
        _context = context;
        _mailService = mailService;
        _logger = logger;
    }

    public async Task<List<AchievmentDto>> GetAchievmentsAsync()
    {
        return await _context.Achievments
            .OrderBy(a => a.Id)
            .Select(a => new AchievmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description
            })
            .ToListAsync();
    }

    public async Task<AchievmentDto?> GetAchievmentByIdAsync(int achievmentId)
    {
        return await _context.Achievments
            .Where(a => a.Id == achievmentId)
            .Select(a => new AchievmentDto
            {
                Id = a.Id,
                Title = a.Title,
                Description = a.Description
            })
            .FirstOrDefaultAsync();
    }

    public async Task<AchievmentDto> CreateAchievmentAsync(AchievmentDto dto)
    {
        var achievment = new Achievment
        {
            Title = dto.Title,
            Description = dto.Description
        };

        _context.Achievments.Add(achievment);
        await _context.SaveChangesAsync();

        return new AchievmentDto
        {
            Id = achievment.Id,
            Title = achievment.Title,
            Description = achievment.Description
        };
    }

    public async Task<AchievmentDto?> UpdateAchievmentAsync(int achievmentId, AchievmentDto dto)
    {
        var achievment = await _context.Achievments.FirstOrDefaultAsync(a => a.Id == achievmentId);
        if (achievment == null)
            return null;

        achievment.Title = dto.Title;
        achievment.Description = dto.Description;

        await _context.SaveChangesAsync();

        return new AchievmentDto
        {
            Id = achievment.Id,
            Title = achievment.Title,
            Description = achievment.Description
        };
    }

    public async Task<bool> DeleteAchievmentAsync(int achievmentId)
    {
        var achievment = await _context.Achievments.FirstOrDefaultAsync(a => a.Id == achievmentId);
        if (achievment == null)
            return false;

        _context.Achievments.Remove(achievment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<UserAchievmentDto>> GetUserAchievmentsAsync(int userId)
    {
        return await _context.UserAchievments
            .Where(ua => ua.UserId == userId)
            .OrderByDescending(ua => ua.UnlockedAt)
            .Select(ua => new UserAchievmentDto
            {
                Id = ua.Id,
                UserId = ua.UserId,
                AchievmentId = ua.AchievmentId,
                UnlockedAt = ua.UnlockedAt
            })
            .ToListAsync();
    }

    public async Task<UserAchievmentDto?> UnlockAchievmentAsync(int userId, int achievmentId)
    {
        var achievment = await _context.Achievments
            .FirstOrDefaultAsync(a => a.Id == achievmentId);

        if (achievment == null)
            return null;

        var existing = await _context.UserAchievments
            .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievmentId == achievmentId);

        if (existing != null)
        {
            return new UserAchievmentDto
            {
                Id = existing.Id,
                UserId = existing.UserId,
                AchievmentId = existing.AchievmentId,
                UnlockedAt = existing.UnlockedAt
            };
        }

        var userAchievment = new UserAchievment
        {
            UserId = userId,
            AchievmentId = achievmentId,
            UnlockedAt = DateTime.UtcNow
        };

        _context.UserAchievments.Add(userAchievment);
        try
        {
            await _context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            var existingAfterRace = await _context.UserAchievments
                .FirstOrDefaultAsync(ua => ua.UserId == userId && ua.AchievmentId == achievmentId);

            if (existingAfterRace == null)
                throw;

            return new UserAchievmentDto
            {
                Id = existingAfterRace.Id,
                UserId = existingAfterRace.UserId,
                AchievmentId = existingAfterRace.AchievmentId,
                UnlockedAt = existingAfterRace.UnlockedAt
            };
        }

        return new UserAchievmentDto
        {
            Id = userAchievment.Id,
            UserId = userAchievment.UserId,
            AchievmentId = userAchievment.AchievmentId,
            UnlockedAt = userAchievment.UnlockedAt
        };
    }

    public async Task<List<ItemDto>> GetItemsAsync()
    {
        return await _context.Items
            .OrderBy(i => i.ItemName)
            .Select(i => new ItemDto
            {
                Id = i.Id,
                ItemName = i.ItemName,
                Description = i.Description
            })
            .ToListAsync();
    }

    public async Task<ItemDto?> GetItemByIdAsync(int itemId)
    {
        return await _context.Items
            .Where(i => i.Id == itemId)
            .Select(i => new ItemDto
            {
                Id = i.Id,
                ItemName = i.ItemName,
                Description = i.Description
            })
            .FirstOrDefaultAsync();
    }

    public async Task<ItemDto> CreateItemAsync(ItemUpsertDto dto)
    {
        var item = new Item
        {
            ItemName = dto.ItemName,
            Description = dto.Description
        };

        _context.Items.Add(item);
        await _context.SaveChangesAsync();

        return new ItemDto
        {
            Id = item.Id,
            ItemName = item.ItemName,
            Description = item.Description
        };
    }

    public async Task<ItemDto?> UpdateItemAsync(int itemId, ItemUpsertDto dto)
    {
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        if (item == null)
            return null;

        item.ItemName = dto.ItemName;
        item.Description = dto.Description;

        await _context.SaveChangesAsync();

        return new ItemDto
        {
            Id = item.Id,
            ItemName = item.ItemName,
            Description = item.Description
        };
    }

    public async Task<bool> DeleteItemAsync(int itemId)
    {
        var item = await _context.Items.FirstOrDefaultAsync(i => i.Id == itemId);
        if (item == null)
            return false;

        _context.Items.Remove(item);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<WeaponDto>> GetWeaponsAsync()
    {
        return await _context.Weapons
            .OrderBy(w => w.ItemName)
            .Select(w => new WeaponDto
            {
                Id = w.Id,
                ItemName = w.ItemName,
                Description = w.Description
            })
            .ToListAsync();
    }

    public async Task<WeaponDto?> GetWeaponByIdAsync(int weaponId)
    {
        return await _context.Weapons
            .Where(w => w.Id == weaponId)
            .Select(w => new WeaponDto
            {
                Id = w.Id,
                ItemName = w.ItemName,
                Description = w.Description
            })
            .FirstOrDefaultAsync();
    }

    public async Task<WeaponDto> CreateWeaponAsync(WeaponUpsertDto dto)
    {
        var weapon = new Weapon
        {
            ItemName = dto.ItemName,
            Description = dto.Description
        };

        _context.Weapons.Add(weapon);
        await _context.SaveChangesAsync();

        return new WeaponDto
        {
            Id = weapon.Id,
            ItemName = weapon.ItemName,
            Description = weapon.Description
        };
    }

    public async Task<WeaponDto?> UpdateWeaponAsync(int weaponId, WeaponUpsertDto dto)
    {
        var weapon = await _context.Weapons.FirstOrDefaultAsync(w => w.Id == weaponId);
        if (weapon == null)
            return null;

        weapon.ItemName = dto.ItemName;
        weapon.Description = dto.Description;

        await _context.SaveChangesAsync();

        return new WeaponDto
        {
            Id = weapon.Id,
            ItemName = weapon.ItemName,
            Description = weapon.Description
        };
    }

    public async Task<bool> DeleteWeaponAsync(int weaponId)
    {
        var weapon = await _context.Weapons.FirstOrDefaultAsync(w => w.Id == weaponId);
        if (weapon == null)
            return false;

        _context.Weapons.Remove(weapon);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<List<MatchDto>> GetAllMatchesAsync()
    {
        var matches = await _context.Matches
            .Include(m => m.MatchItems)
                .ThenInclude(mi => mi.Item)
            .Include(m => m.MatchWeapons)
                .ThenInclude(mw => mw.Weapon)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return matches.Select(MapToMatchDto).ToList();
    }

    public async Task<List<MatchDto>> GetMatchesByUserAsync(int userId)
    {
        var matches = await _context.Matches
            .Where(m => m.UserId == userId)
            .Include(m => m.MatchItems)
                .ThenInclude(mi => mi.Item)
            .Include(m => m.MatchWeapons)
                .ThenInclude(mw => mw.Weapon)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return matches.Select(MapToMatchDto).ToList();
    }

    public async Task<MatchDto?> GetMatchByIdAsync(int userId, int matchId)
    {
        var match = await _context.Matches
            .Include(m => m.MatchItems)
                .ThenInclude(mi => mi.Item)
            .Include(m => m.MatchWeapons)
                .ThenInclude(mw => mw.Weapon)
            .FirstOrDefaultAsync(m => m.Id == matchId && m.UserId == userId);

        return match == null ? null : MapToMatchDto(match);
    }

    public async Task<MatchDto> CreateMatchAsync(int userId, CreateMatchDto dto)
    {
        var match = new Match
        {
            UserId = userId,
            Time = dto.Time,
            Level = dto.Level,
            DamageDealt = dto.DamageDealt,
            DamageTaken = dto.DamageTaken,
            EnemiesKilled = dto.EnemiesKilled,
            CoinsCollected = dto.CoinsCollected,
            MaxHealth = dto.MaxHealth,
            CreatedAt = DateTime.UtcNow
        };

        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        await ReplaceMatchItemsAsync(match.Id, dto.ItemIds);
        await ReplaceMatchWeaponsAsync(match.Id, dto.WeaponIds);

        var createdMatch = await _context.Matches
            .Include(m => m.MatchItems)
                .ThenInclude(mi => mi.Item)
            .Include(m => m.MatchWeapons)
                .ThenInclude(mw => mw.Weapon)
            .FirstAsync(m => m.Id == match.Id);

        return MapToMatchDto(createdMatch);
    }

    public async Task<MatchDto?> UpdateMatchAsync(int userId, int matchId, UpdateMatchDto dto)
    {
        var match = await _context.Matches.FirstOrDefaultAsync(m => m.Id == matchId && m.UserId == userId);
        if (match == null)
            return null;

        match.Time = dto.Time;
        match.Level = dto.Level;
        match.DamageDealt = dto.DamageDealt;
        match.DamageTaken = dto.DamageTaken;
        match.EnemiesKilled = dto.EnemiesKilled;
        match.CoinsCollected = dto.CoinsCollected;
        match.MaxHealth = dto.MaxHealth;

        await _context.SaveChangesAsync();
        await ReplaceMatchItemsAsync(matchId, dto.ItemIds);
        await ReplaceMatchWeaponsAsync(matchId, dto.WeaponIds);

        var updatedMatch = await _context.Matches
            .Include(m => m.MatchItems)
                .ThenInclude(mi => mi.Item)
            .Include(m => m.MatchWeapons)
                .ThenInclude(mw => mw.Weapon)
            .FirstAsync(m => m.Id == matchId && m.UserId == userId);

        return MapToMatchDto(updatedMatch);
    }

    public async Task<bool> DeleteMatchAsync(int userId, int matchId, bool canDeleteAny = false)
    {
        var match = await _context.Matches
            .FirstOrDefaultAsync(m => m.Id == matchId && (canDeleteAny || m.UserId == userId));

        if (match == null)
            return false;

        _context.Matches.Remove(match);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<UserDto?> GetUserByIdAsync(int userId)
    {
        return await _context.Users
            .Where(u => u.Id == userId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Username = u.Username,
                Email = u.Email,
                CreatedAt = u.CreatedAt
            })
            .FirstOrDefaultAsync();
    }

    public async Task<UserDto?> UpdateUserAsync(int userId, UpdateUserDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return null;

        var oldUsername = user.Username;
        var oldEmail = user.Email;
        var usernameChanged = !string.Equals(oldUsername, dto.Username, StringComparison.Ordinal);
        var emailChanged = !string.Equals(oldEmail, dto.Email, StringComparison.OrdinalIgnoreCase);
        var passwordChanged = !string.IsNullOrWhiteSpace(dto.Password);

        user.Username = dto.Username;
        user.Email = dto.Email;

        if (!string.IsNullOrWhiteSpace(dto.Password))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);
        }

        user.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await NotifyUserUpdateAsync(user, oldUsername, oldEmail, usernameChanged, emailChanged, passwordChanged);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            CreatedAt = user.CreatedAt
        };
    }

    private async Task NotifyUserUpdateAsync(
        User user,
        string oldUsername,
        string oldEmail,
        bool usernameChanged,
        bool emailChanged,
        bool passwordChanged)
    {
        if (!usernameChanged && !emailChanged && !passwordChanged)
            return;

        var changes = new List<string>();

        if (usernameChanged)
            changes.Add($"- Username changed from '{oldUsername}' to '{user.Username}'");

        if (emailChanged)
            changes.Add($"- Email changed from '{oldEmail}' to '{user.Email}'");

        if (passwordChanged)
            changes.Add("- Password was changed");

        var body = "A security-related update was made to your Bloodwave account.\n\n" +
                   string.Join("\n", changes) +
                   "\n\nIf this was not you, secure your account immediately.";

        var recipients = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(oldEmail))
            recipients.Add(oldEmail);
        if (!string.IsNullOrWhiteSpace(user.Email))
            recipients.Add(user.Email);

        foreach (var recipient in recipients)
        {
            try
            {
                var result = await _mailService.SendEmailAsync(
                    recipient,
                    "Bloodwave account update notification",
                    body);

                if (!result.IsSuccess)
                    _logger.LogWarning("Failed to send profile-update email. To={Recipient}, Error={Error}", recipient, result.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected profile-update email error. To={Recipient}", recipient);
            }
        }
    }

    public async Task<bool> DeleteUserAsync(int userId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == userId);
        if (user == null)
            return false;

        _context.Users.Remove(user);

        await _context.SaveChangesAsync();
        return true;
    }

    private async Task ReplaceMatchItemsAsync(int matchId, List<int>? itemIds)
    {
        var current = await _context.MatchItems.Where(mi => mi.MatchId == matchId).ToListAsync();
        if (current.Count > 0)
            _context.MatchItems.RemoveRange(current);

        if (itemIds is { Count: > 0 })
        {
            var toInsert = itemIds
                .Distinct()
                .Select(id => new MatchItem { MatchId = matchId, ItemId = id });

            await _context.MatchItems.AddRangeAsync(toInsert);
        }

        await _context.SaveChangesAsync();
    }

    private async Task ReplaceMatchWeaponsAsync(int matchId, List<int>? weaponIds)
    {
        var current = await _context.MatchWeapons.Where(mw => mw.MatchId == matchId).ToListAsync();
        if (current.Count > 0)
            _context.MatchWeapons.RemoveRange(current);

        if (weaponIds is { Count: > 0 })
        {
            var toInsert = weaponIds
                .Distinct()
                .Select(id => new MatchWeapon { MatchId = matchId, WeaponId = id });

            await _context.MatchWeapons.AddRangeAsync(toInsert);
        }

        await _context.SaveChangesAsync();
    }

    private static MatchDto MapToMatchDto(Match match)
    {
        var matchItems = match.MatchItems
            .Select(mi => new MatchItemDto
            {
                Id = mi.Id,
                ItemId = mi.ItemId,
                ItemName = mi.Item?.ItemName
            })
            .ToList();

        var matchWeapons = match.MatchWeapons
            .Select(mw => new MatchWeaponDto
            {
                Id = mw.Id,
                WeaponId = mw.WeaponId,
                WeaponName = mw.Weapon?.ItemName
            })
            .ToList();

        return new MatchDto
        {
            Id = match.Id,
            UserId = match.UserId,
            Time = match.Time,
            Level = match.Level,
            DamageDealt = match.DamageDealt,
            DamageTaken = match.DamageTaken,
            EnemiesKilled = match.EnemiesKilled,
            CoinsCollected = match.CoinsCollected,
            MaxHealth = match.MaxHealth,
            CreatedAt = match.CreatedAt,
            ItemIds = matchItems.Select(i => i.ItemId).ToList(),
            WeaponIds = matchWeapons.Select(w => w.WeaponId).ToList(),
            MatchItems = matchItems,
            MatchWeapons = matchWeapons
        };
    }
}
