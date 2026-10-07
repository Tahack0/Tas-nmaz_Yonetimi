namespace TasinmazYönetimi.Domain.Entities;

// Mahalleyi bağlı olduğu ilçe ile eşleştirir.
public class Neighborhood
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int DistrictId { get; set; }
    public District District { get; set; } = null!;
}