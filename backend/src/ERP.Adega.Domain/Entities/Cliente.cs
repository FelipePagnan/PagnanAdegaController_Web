using ERP.Adega.Domain.ValueObjects;

namespace ERP.Adega.Domain.Entities;

public class Cliente : EntityBase
{
    public string Nome { get; private set; } = default!;
    public string? CPF { get; private set; }
    public string? CNPJ { get; private set; }
    public Contato? Contato { get; private set; }
    public Endereco? Endereco { get; private set; }
    public string? Observacoes { get; private set; }
    public bool Ativo { get; private set; } = true;

    private Cliente() { }

    public static Cliente Criar(string nome, string? cpf = null, string? cnpj = null)
    {
        return new Cliente
        {
            Nome = nome.Trim(),
            CPF = cpf?.Trim(),
            CNPJ = cnpj?.Trim()
        };
    }

    public void Atualizar(string nome, string? cpf, string? cnpj,
        Contato? contato, Endereco? endereco, string? observacoes)
    {
        Nome = nome.Trim();
        CPF = cpf?.Trim();
        CNPJ = cnpj?.Trim();
        Contato = contato;
        Endereco = endereco;
        Observacoes = observacoes?.Trim();
        MarcarAtualizado();
    }

    public void Inativar() { Ativo = false; MarcarAtualizado(); }
}
