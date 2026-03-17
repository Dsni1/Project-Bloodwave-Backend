namespace Project_Bloodwave_Backend.DTOs;

public class ItemDto
{
    public int Id { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public class ItemUpsertDto
{
    public string ItemName { get; set; } = string.Empty;
    public string? Description { get; set; }
}
