using JoyReactor.Accordion.Logic.Database.Sql.Entities;

namespace JoyReactor.Accordion.WebAPI.Models.Responses;

public record ApiThinResponse
{
    public ApiThinResponse() { }
    public ApiThinResponse(Api api)
    {
        HostName = api.HostName;
    }

    public string HostName { get; set; }
}