namespace ERP.Adega.Application.Common;

public class Result
{
    public bool Sucesso { get; }
    public string? Erro { get; }
    public string? Codigo { get; }

    protected Result(bool sucesso, string? erro, string? codigo)
    {
        Sucesso = sucesso;
        Erro = erro;
        Codigo = codigo;
    }

    public static Result Ok() => new(true, null, null);
    public static Result Fail(string erro, string? codigo = null) => new(false, erro, codigo);
    public static Result<T> Ok<T>(T valor) => new(valor, true, null, null);
    public static Result<T> Fail<T>(string erro, string? codigo = null) => new(default, false, erro, codigo);
}

public class Result<T> : Result
{
    public T? Valor { get; }

    internal Result(T? valor, bool sucesso, string? erro, string? codigo)
        : base(sucesso, erro, codigo)
    {
        Valor = valor;
    }
}

public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; }
    public int Total { get; }
    public int Pagina { get; }
    public int TamanhoPagina { get; }
    public int TotalPaginas => (int)Math.Ceiling((double)Total / TamanhoPagina);

    public PagedResult(IReadOnlyList<T> items, int total, int pagina, int tamanhoPagina)
    {
        Items = items;
        Total = total;
        Pagina = pagina;
        TamanhoPagina = tamanhoPagina;
    }
}
