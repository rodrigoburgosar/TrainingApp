namespace SportFlow.Domain.Shared.ValueObjects;

public readonly record struct LocationId(Guid Value)
{
    public static LocationId New() => new(Guid.NewGuid());
    public static LocationId From(Guid value) => new(value);
    public override string ToString() => Value.ToString();
}
