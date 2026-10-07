namespace TasinmazYönetimi.Domain.Entities;

// İlçeyi bağlı olduğu il ile eşleştirir.
public class District
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public int CityId { get; set; }
    public City City { get; set; } = null!;
}