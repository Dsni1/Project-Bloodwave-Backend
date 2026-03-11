using Project_Bloodwave_Backend.Data;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Project_Bloodwave_Backend.Services;

public interface IPlayerService
{
    Task<PlayerDto> DeleteUserAsync(int userId);
    Task<MatchDto> CreateMatchAsync(int userId, CreateMatchDto createMatchDto);
    Task<List<MatchDto>> GetAllMatchesAsync(int userId);
    Task<MatchDto?> GetMatchByIdAsync(int matchId, int userId);
}

public class PlayerService : IPlayerService
{
    private readonly BloodwaveDbContext _context;

    public PlayerService(BloodwaveDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerDto> DeleteUserAsync(int userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
            return new PlayerDto { Success = false, Message = "User not found" };
        user.IsActive = false;
        await _context.SaveChangesAsync();

        return new PlayerDto { Success = true, Message = "User deleted successfully" };
    }

    public async Task<MatchDto> CreateMatchAsync(int userId, CreateMatchDto createMatchDto)
    {
        var match = new Match
        {
            UserId = userId,
            Time = createMatchDto.Time,
            Level = createMatchDto.Level,
            MaxHealth = createMatchDto.MaxHealth,
            CreatedAt = DateTime.UtcNow
        };

        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        await AddMatchItemsAsync(match.Id, createMatchDto.ItemIds);
        await AddMatchWeaponsAsync(match.Id, createMatchDto.WeaponIds);

        var createdMatch = await _context.Matches
            .Include(m => m.MatchItems)
                .ThenInclude(mi => mi.Item)
            .Include(m => m.MatchWeapons)
                .ThenInclude(mw => mw.Weapon)
            .FirstAsync(m => m.Id == match.Id);

        return MapToMatchDtoWithRelations(createdMatch);
    }

    public async Task<List<MatchDto>> GetAllMatchesAsync(int userId)
    {
        var matches = await _context.Matches
            .Where(m => m.UserId == userId)
            .Include(m => m.MatchItems)
                .ThenInclude(mi => mi.Item)
            .Include(m => m.MatchWeapons)
                .ThenInclude(mw => mw.Weapon)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return matches.Select(MapToMatchDtoWithRelations).ToList();
    }

    public async Task<MatchDto?> GetMatchByIdAsync(int matchId, int userId)
    {
        var match = await _context.Matches
            .Include(m => m.MatchItems)
                .ThenInclude(mi => mi.Item)
            .Include(m => m.MatchWeapons)
                .ThenInclude(mw => mw.Weapon)
            .FirstOrDefaultAsync(m => m.Id == matchId && m.UserId == userId);

        return match == null ? null : MapToMatchDtoWithRelations(match);
    }

    private async Task AddMatchItemsAsync(int matchId, List<int>? itemIds)
    {
        if (itemIds == null || itemIds.Count == 0)
            return;

        var matchItems = itemIds
            .Select(itemId => new MatchItem { MatchId = matchId, ItemId = itemId })
            .ToList();

        _context.MatchItems.AddRange(matchItems);
        await _context.SaveChangesAsync();
    }

    private async Task AddMatchWeaponsAsync(int matchId, List<int>? weaponIds)
    {
        if (weaponIds == null || weaponIds.Count == 0)
            return;

        var distinctWeaponIds = weaponIds.Distinct().ToList();

        var matchWeapons = distinctWeaponIds
            .Select(weaponId => new MatchWeapon
            {
                MatchId = matchId,
                WeaponId = weaponId
            })
            .ToList();

        _context.MatchWeapons.AddRange(matchWeapons);
        await _context.SaveChangesAsync();
    }

    private async Task<List<int>> GetMatchItemIdsAsync(int matchId)
    {
        return await _context.MatchItems
            .Where(mi => mi.MatchId == matchId)
            .Select(mi => mi.ItemId)
            .ToListAsync();
    }

    private MatchDto MapToMatchDto(
        Match match,
        List<int>? itemIds = null,
        List<MatchItemDto>? matchItems = null,
        List<MatchWeaponDto>? matchWeapons = null)
    {
        return new MatchDto
        {
            Id = match.Id,
            UserId = match.UserId,
            Time = match.Time,
            Level = match.Level,
            MaxHealth = match.MaxHealth,
            CreatedAt = match.CreatedAt,
            ItemIds = itemIds ?? new List<int>(),
            MatchItems = matchItems ?? new List<MatchItemDto>(),
            MatchWeapons = matchWeapons ?? new List<MatchWeaponDto>()
        };
    }

    private MatchDto MapToMatchDtoWithRelations(Match match)
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

        return MapToMatchDto(
            match,
            matchItems.Select(mi => mi.ItemId).ToList(),
            matchItems,
            matchWeapons);
    }

}
