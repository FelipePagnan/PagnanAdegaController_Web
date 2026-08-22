using ERP.Adega.Domain.ValueObjects;

namespace ERP.Adega.Domain.Entities;

public class Fornecedor : EntityBase
{
    public string RazaoSocial { get; private set; } = default!;
    public string? NomeFantasia { get; private set; }
    public string CNPJ { get; private set; } = default!;
    public Contato? Contato { get; private set; }
    public Endereco? Endereco { get; private set; }
    public string? Observacoes { get; private set; }
    public bool Ativo { get; private set; } = true;

    private Fornecedor() { }

    public static Fornecedor Criar(string razaoSocial, string cnpj, string? nomeFantasia = null)
    {
        return new Fornecedor
        {
            RazaoSocial = razaoSocial.Trim(),
            CNPJ = cnpj.Trim(),
            NomeFantasia = nomeFantasia?.Trim()
        };
    }

    public void Atualizar(string razaoSocial, string? nomeFantasia,
        Contato? contato, Endereco? endereco, string? observacoes)
    {
        RazaoSocial = razaoSocial.Trim();
        NomeFantasia = nomeFantasia?.Trim();
        Contato = contato;
        Endereco = endereco;
        Observacoes = observacoes?.Trim();
        MarcarAtualizado();
    }

    public void Inativar() { Ativo = false; MarcarAtualizado(); }
    public void Ativar() { Ativo = true; MarcarAtualizado(); }
}
