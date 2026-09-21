using System.ComponentModel.DataAnnotations;

namespace TasinmazYönetimi.Application.Users;

public class UserQuery
{
    [Range(1, 1_000_000)]
    public int Page { get; set; } = 1;

    [Range(1, 100)]
    public int PageSize { get; set; } = 20;
}