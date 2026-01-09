namespace JoyReactor.Accordion.Logic.Database.Sql.Entities;

public interface ISqlEntity
{
    public Guid Id { get; set; }
}

public interface ISqlCreatedAtEntity : ISqlEntity
{
    public DateTime CreatedAt { get; set; }
}

public interface ISqlUpdatedAtEntity : ISqlCreatedAtEntity
{
    public DateTime UpdatedAt { get; set; }
}