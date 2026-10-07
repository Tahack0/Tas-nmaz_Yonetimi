using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TasinmazYönetimi.Domain.Entities;

namespace TasinmazYönetimi.Infrastructure.Persistence;

public static class LocationDataSeeder
{
    public static async Task SeedAsync(
        ApplicationDbContext db,
        string filePath,
        CancellationToken cancellationToken = default)
    {
        // Veriler zaten aktarılmışsa tekrar eklemez.
        if (await db.Cities.AnyAsync(cancellationToken))
            return;

        // JSON dosyasındaki il → ilçe → mahalle yapısını okur.
        await using var stream = File.OpenRead(filePath);

        var data = await JsonSerializer.DeserializeAsync<
            Dictionary<string, Dictionary<string, List<string>>>>(
                stream,
                cancellationToken: cancellationToken);

        if (data is null || data.Count != 81)
            throw new InvalidOperationException(
                "Konum dosyası geçerli değil veya 81 il içermiyor.");

        foreach (var (cityName, districts) in data)
        {
            var city = new City { Name = cityName.Trim() };
            db.Cities.Add(city);

            foreach (var (districtName, neighborhoods) in districts)
            {
                // İli navigation üzerinden bağlar; ID'yi EF oluşturur.
                var district = new District
                {
                    Name = districtName.Trim(),
                    City = city
                };

                db.Districts.Add(district);

                foreach (string name in neighborhoods
                    .Select(name => name.Trim())
                    .Where(name => name.Length > 0)
                    .Distinct(StringComparer.Ordinal))
                {
                    db.Neighborhoods.Add(new Neighborhood
                    {
                        Name = name,
                        District = district
                    });
                }
            }
        }

        // Bütün kayıtları tek işlemde veritabanına aktarır.
        await db.SaveChangesAsync(cancellationToken);
    }
}