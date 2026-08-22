namespace ERP.Adega.Domain.ValueObjects;

public record Endereco(
    string Logradouro,
    string Numero,
    string? Complemento,
    string Bairro,
    string Cidade,
    string UF,
    string CEP
);

public record Contato(
    string? Telefone,
    string? Celular,
    string? Email,
    string? NomeContato
);
