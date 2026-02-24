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

        return new PlayerDto { Success = true, Message = "User deactivated successfully" };
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

        return MapToMatchDto(match, createMatchDto.ItemIds);
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

}
