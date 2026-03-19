namespace JoyReactor.Accordion.WebAPI.Models.Responses;

public record PagedResponse<T>
    where T : class
{
    public T[] Values { get; set; }
    public int Pages { get; set; }
}