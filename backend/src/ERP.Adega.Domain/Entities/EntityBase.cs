namespace ERP.Adega.Domain.Entities;

public abstract class EntityBase
{
    public Guid Id { get; protected set; } = Guid.NewGuid();
    public DateTime CriadoEm { get; protected set; } = DateTime.UtcNow;
    public DateTime? AtualizadoEm { get; protected set; }

    protected void MarcarAtualizado() => AtualizadoEm = DateTime.UtcNow;

    public override bool Equals(object? obj)
    {
        if (obj is not EntityBase other) return false;
        if (ReferenceEquals(this, other)) return true;
        return Id == other.Id;
    }

    public override int GetHashCode() => Id.GetHashCode();
}
