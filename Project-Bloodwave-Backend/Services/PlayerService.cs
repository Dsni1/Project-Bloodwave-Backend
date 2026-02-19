using Project_Bloodwave_Backend.Data;
using Project_Bloodwave_Backend.DTOs;
using Project_Bloodwave_Backend.Models;

namespace Project_Bloodwave_Backend.Services;

public interface IPlayerService
{
    Task<PlayerStatsDto?> GetPlayerStatsAsync(int userId);
    Task<MatchDto> CreateMatchAsync(int userId, CreateMatchDto createMatchDto);
    Task UpdatePlayerStatsAsync(int userId, int kills, int level);
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

    public async Task<PlayerStatsDto?> GetPlayerStatsAsync(int userId)
    {
        var playerStats = _context.PlayerStats.FirstOrDefault(ps => ps.UserId == userId);
        
        if (playerStats == null)
            return null;

        return new PlayerStatsDto
        {
            Id = playerStats.Id,
            UserId = playerStats.UserId,
            TotalKills = playerStats.TotalKills,
            HighestLevel = playerStats.HighestLevel,
            UpdatedAt = playerStats.UpdatedAt
        };
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

        // Hozzáadunk itemeket ha vannak
        if (createMatchDto.ItemIds != null && createMatchDto.ItemIds.Count > 0)
        {
            foreach (var itemId in createMatchDto.ItemIds)
            {
                var matchItem = new MatchItem
                {
                    MatchId = match.Id,
                    ItemId = itemId
                };
                _context.MatchItems.Add(matchItem);
            }
            await _context.SaveChangesAsync();
        }

        // PlayerStats frissítése
        var playerStats = _context.PlayerStats.FirstOrDefault(ps => ps.UserId == userId);
        if (playerStats == null)
        {
            playerStats = new PlayerStats
            {
                UserId = userId,
                TotalKills = createMatchDto.Level, // Feltételezve, hogy level = kills
                HighestLevel = createMatchDto.Level,
                UpdatedAt = DateTime.UtcNow
            };
            _context.PlayerStats.Add(playerStats);
        }
        else
        {
            playerStats.TotalKills += createMatchDto.Level;
            if (createMatchDto.Level > playerStats.HighestLevel)
                playerStats.HighestLevel = createMatchDto.Level;
            playerStats.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

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
            ItemIds = createMatchDto.ItemIds
        };
    }

    public async Task UpdatePlayerStatsAsync(int userId, int kills, int level)
    {
        var playerStats = _context.PlayerStats.FirstOrDefault(ps => ps.UserId == userId);
        
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
            if (level > playerStats.HighestLevel)
                playerStats.HighestLevel = level;
            playerStats.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
    }

    public async Task<List<MatchDto>> GetAllMatchesAsync(int userId)
    {
        var matches = _context.Matches
            .Where(m => m.UserId == userId)
            .OrderByDescending(m => m.CreatedAt)
            .ToList();

        var matchDtos = new List<MatchDto>();
        foreach (var match in matches)
        {
            var itemIds = _context.MatchItems
                .Where(mi => mi.MatchId == match.Id)
                .Select(mi => mi.ItemId)
                .ToList();

            matchDtos.Add(new MatchDto
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
                ItemIds = itemIds
            });
        }

        return matchDtos;
    }

    public async Task<MatchDto?> GetMatchByIdAsync(int matchId, int userId)
    {
        var match = _context.Matches.FirstOrDefault(m => m.Id == matchId && m.UserId == userId);
        
        if (match == null)
            return null;

        var itemIds = _context.MatchItems
            .Where(mi => mi.MatchId == match.Id)
            .Select(mi => mi.ItemId)
            .ToList();

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
            ItemIds = itemIds
        };
    }
}
