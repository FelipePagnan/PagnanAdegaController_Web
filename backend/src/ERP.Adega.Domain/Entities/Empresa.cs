using ERP.Adega.Domain.ValueObjects;

namespace ERP.Adega.Domain.Entities;

public class Empresa : EntityBase
{
    public string RazaoSocial { get; private set; } = default!;
    public string? NomeFantasia { get; private set; }
    public string CNPJ { get; private set; } = default!;
    public string? InscricaoEstadual { get; private set; }
    public Endereco? Endereco { get; private set; }
    public string? Telefone { get; private set; }
    public string? Email { get; private set; }
    public bool Ativa { get; private set; } = true;

    // Navegação
    private readonly List<Filial> _filiais = new();
    public IReadOnlyCollection<Filial> Filiais => _filiais.AsReadOnly();

    private Empresa() { } // EF Core

    public static Empresa Criar(string razaoSocial, string cnpj, string? nomeFantasia = null)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial))
            throw new ArgumentException("Razão social é obrigatória.", nameof(razaoSocial));

        if (string.IsNullOrWhiteSpace(cnpj))
            throw new ArgumentException("CNPJ é obrigatório.", nameof(cnpj));

        return new Empresa
        {
            RazaoSocial = razaoSocial.Trim(),
            CNPJ = cnpj.Trim(),
            NomeFantasia = nomeFantasia?.Trim()
        };
    }

    public void Atualizar(string razaoSocial, string? nomeFantasia, string? inscricaoEstadual,
        Endereco? endereco, string? telefone, string? email)
    {
        RazaoSocial = razaoSocial.Trim();
        NomeFantasia = nomeFantasia?.Trim();
        InscricaoEstadual = inscricaoEstadual?.Trim();
        Endereco = endereco;
        Telefone = telefone?.Trim();
        Email = email?.Trim();
        MarcarAtualizado();
    }

    public void Inativar() { Ativa = false; MarcarAtualizado(); }
    public void Ativar() { Ativa = true; MarcarAtualizado(); }

    public Filial AdicionarFilial(string nome, string codigo)
    {
        var filial = Filial.Criar(Id, nome, codigo);
        _filiais.Add(filial);
        return filial;
    }
}
