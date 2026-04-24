using JoyReactor.Accordion.Logic.ApiClient.Models;
using JoyReactor.Accordion.Logic.Database.Sql.Entities;

namespace JoyReactor.Accordion.Logic.MQ.Messages;

public record ApiPostMessage
{
    public required Api Api { get; init; }
    public required Post Post { get; init; }
}