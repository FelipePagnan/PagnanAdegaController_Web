using ERP.Adega.Domain.ValueObjects;

namespace ERP.Adega.Domain.Entities;

public class Filial : EntityBase
{
    public Guid EmpresaId { get; private set; }
    public string Nome { get; private set; } = default!;
    public string Codigo { get; private set; } = default!;
    public Endereco? Endereco { get; private set; }
    public bool Ativa { get; private set; } = true;

    // Navegação
    public Empresa Empresa { get; private set; } = default!;

    private Filial() { }

    internal static Filial Criar(Guid empresaId, string nome, string codigo)
    {
        if (string.IsNullOrWhiteSpace(nome))
            throw new ArgumentException("Nome da filial é obrigatório.", nameof(nome));

        return new Filial
        {
            EmpresaId = empresaId,
            Nome = nome.Trim(),
            Codigo = codigo.Trim()
        };
    }

    public void Atualizar(string nome, string codigo, Endereco? endereco)
    {
        Nome = nome.Trim();
        Codigo = codigo.Trim();
        Endereco = endereco;
        MarcarAtualizado();
    }

    public void Inativar() { Ativa = false; MarcarAtualizado(); }
    public void Ativar() { Ativa = true; MarcarAtualizado(); }
}
