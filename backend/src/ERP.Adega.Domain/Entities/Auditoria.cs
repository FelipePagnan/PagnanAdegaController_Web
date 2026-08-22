using ERP.Adega.Domain.Enums;

namespace ERP.Adega.Domain.Entities;

/// <summary>
/// Registro de auditoria imutável (append-only).
/// RN-011: Operações críticas geram auditoria.
/// Separada de logs técnicos.
/// </summary>
public class Auditoria
{
    public Guid Id { get; private set; } = Guid.NewGuid();
    public Guid EmpresaId { get; private set; }
    public Guid? FilialId { get; private set; }
    public Guid UsuarioId { get; private set; }
    public string Entidade { get; private set; } = default!;
    public Guid EntidadeId { get; private set; }
    public AcaoAuditoria Acao { get; private set; }
    public string? ValorAnterior { get; private set; }
    public string? ValorPosterior { get; private set; }
    public string? Motivo { get; private set; }
    public Guid? AutorizadoPor { get; private set; }
    public string? IP { get; private set; }
    public DateTime CriadoEm { get; private set; } = DateTime.UtcNow;

    // Navegação
    public Usuario Usuario { get; private set; } = default!;

    private Auditoria() { }

    public static Auditoria Criar(
        Guid empresaId,
        Guid usuarioId,
        string entidade,
        Guid entidadeId,
        AcaoAuditoria acao,
        Guid? filialId = null,
        string? valorAnterior = null,
        string? valorPosterior = null,
        string? motivo = null,
        Guid? autorizadoPor = null,
        string? ip = null)
    {
        return new Auditoria
        {
            EmpresaId = empresaId,
            FilialId = filialId,
            UsuarioId = usuarioId,
            Entidade = entidade,
            EntidadeId = entidadeId,
            Acao = acao,
            ValorAnterior = valorAnterior,
            ValorPosterior = valorPosterior,
            Motivo = motivo,
            AutorizadoPor = autorizadoPor,
            IP = ip
        };
    }
}
