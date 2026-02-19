using Project_Bloodwave_Backend.Data;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Models;
using Microsoft.EntityFrameworkCore;

namespace Project_Bloodwave_Backend.Services;

public interface IPlayerService
{
    Task<PlayerStatsDto?> GetPlayerStatsAsync(int userId);
    Task<MatchDto> CreateMatchAsync(int userId, CreateMatchDto createMatchDto);
    Task UpdatePlayerStatsAsync(int userId, int kills, int level);
    Task<List<MatchDto>> GetAllMatchesAsync(int userId);
    Task<MatchDto?> GetMatchByIdAsync(int matchId, int userId);
    Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int limit = 100);
}

public class PlayerService : IPlayerService
{
    private readonly BloodwaveDbContext _context;

    public PlayerService(BloodwaveDbContext context)
    {
        _context = context;
    }

    public async Task<PlayerStatsDto?> GetPlayerStatsAsync(int userId)
    {
        var playerStats = await _context.PlayerStats
            .FirstOrDefaultAsync(ps => ps.UserId == userId);

        return playerStats == null ? null : MapToPlayerStatsDto(playerStats);
    }

    public async Task<MatchDto> CreateMatchAsync(int userId, CreateMatchDto createMatchDto)
    {
        var match = new Match
        {
            UserId = userId,
            Time = createMatchDto.Time,
            Level = createMatchDto.Level,
            MaxHealth = createMatchDto.MaxHealth,
            Weapon1 = createMatchDto.Weapon1,
            Weapon2 = createMatchDto.Weapon2,
            Weapon3 = createMatchDto.Weapon3,
            CreatedAt = DateTime.UtcNow
        };

        _context.Matches.Add(match);
        await _context.SaveChangesAsync();

        await AddMatchItemsAsync(match.Id, createMatchDto.ItemIds);
        await UpdatePlayerStatsAsync(userId, createMatchDto.Level, createMatchDto.Level);

        return MapToMatchDto(match, createMatchDto.ItemIds);
    }

    public async Task UpdatePlayerStatsAsync(int userId, int kills, int level)
    {
        var playerStats = await _context.PlayerStats
            .FirstOrDefaultAsync(ps => ps.UserId == userId);

        if (playerStats == null)
        {
            playerStats = new PlayerStats
            {
                UserId = userId,
                TotalKills = kills,
                HighestLevel = level,
                UpdatedAt = DateTime.UtcNow
            };
            _context.PlayerStats.Add(playerStats);
        }
        else
        {
            playerStats.TotalKills += kills;
            playerStats.HighestLevel = Math.Max(playerStats.HighestLevel, level);
            playerStats.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<MatchDto>> GetAllMatchesAsync(int userId)
    {
        var matches = await _context.Matches
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();

        return matches.Select(match => MapToMatchDtoWithItems(match)).ToList();
    }

    public async Task<MatchDto?> GetMatchByIdAsync(int matchId, int userId)
    {
        var match = await _context.Matches
            .FirstOrDefaultAsync(m => m.Id == matchId && m.UserId == userId);

        return match == null ? null : MapToMatchDtoWithItems(match);
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

    private async Task<List<int>> GetMatchItemIdsAsync(int matchId)
    {
        return await _context.MatchItems
            .Where(mi => mi.MatchId == matchId)
            .Select(mi => mi.ItemId)
            .ToListAsync();
    }

    private MatchDto MapToMatchDto(Match match, List<int>? itemIds = null)
    {
        return new MatchDto
        {
            Id = match.Id,
            UserId = match.UserId,
            Time = match.Time,
            Level = match.Level,
            MaxHealth = match.MaxHealth,
            Weapon1 = match.Weapon1,
            Weapon2 = match.Weapon2,
            Weapon3 = match.Weapon3,
            CreatedAt = match.CreatedAt,
            ItemIds = itemIds ?? new List<int>()
        };
    }

    private MatchDto MapToMatchDtoWithItems(Match match)
    {
        var itemIds = _context.MatchItems
            .Where(mi => mi.MatchId == match.Id)
            .Select(mi => mi.ItemId)
            .ToList();

        return MapToMatchDto(match, itemIds);
    }

    private static PlayerStatsDto MapToPlayerStatsDto(PlayerStats playerStats)
    {
        return new PlayerStatsDto
        {
            Id = playerStats.Id,
            UserId = playerStats.UserId,
            TotalKills = playerStats.TotalKills,
            HighestLevel = playerStats.HighestLevel,
            UpdatedAt = playerStats.UpdatedAt
        };
    }

    public async Task<List<LeaderboardEntryDto>> GetLeaderboardAsync(int limit = 100)
    {
        var leaderboard = await _context.PlayerStats
            .Include(ps => ps.User)
            .OrderByDescending(ps => ps.TotalKills)
            .ThenByDescending(ps => ps.HighestLevel)
            .Take(limit)
            .ToListAsync();

        return leaderboard
            .Select((stat, index) => new LeaderboardEntryDto
            {
                Rank = index + 1,
                UserId = stat.UserId,
                Username = stat.User?.Username ?? "Unknown",
                TotalKills = stat.TotalKills,
                HighestLevel = stat.HighestLevel,
                UpdatedAt = stat.UpdatedAt
            })
            .ToList();
    }
}
