namespace ERP.Adega.Domain.Entities;

/// <summary>
/// Registro de auditoria — RN-011: operações críticas geram auditoria.
/// Imutável após criação. Não é log técnico — é auditoria de negócio.
/// </summary>
public class Auditoria : EntityBase
{
    public string Operacao { get; private set; } = default!;
    public string Entidade { get; private set; } = default!;
    public Guid? EntidadeId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public Guid? EmpresaId { get; private set; }
    public Guid? FilialId { get; private set; }
    public string? Detalhes { get; private set; }
    public string? ValorAnterior { get; private set; }
    public string? ValorPosterior { get; private set; }
    public string? Motivo { get; private set; }
    public string? IP { get; private set; }

    private Auditoria() { }

    public static Auditoria Criar(string operacao, string entidade, Guid usuarioId,
        Guid? empresaId = null, Guid? filialId = null, Guid? entidadeId = null,
        string? detalhes = null, string? valorAnterior = null, string? valorPosterior = null,
        string? motivo = null, string? ip = null)
    {
        return new Auditoria
        {
            Operacao = operacao,
            Entidade = entidade,
            UsuarioId = usuarioId,
            EmpresaId = empresaId,
            FilialId = filialId,
            EntidadeId = entidadeId,
            Detalhes = detalhes,
            ValorAnterior = valorAnterior,
            ValorPosterior = valorPosterior,
            Motivo = motivo,
            IP = ip
        };
    }
}
