using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Exceptions;
using FluentAssertions;

namespace ERP.Adega.Domain.Tests;

public class ProdutoTests
{
    [Fact]
    public void Criar_Produto_Com_Dados_Validos()
    {
        var produto = Produto.Criar("Coca-Cola 350ml", Guid.NewGuid(),
            UnidadeBase.Unidade, 4.50m, descricao: "Refrigerante");

        produto.Nome.Should().Be("Coca-Cola 350ml");
        produto.PrecoVenda.Should().Be(4.50m);
        produto.Ativo.Should().BeTrue();
        produto.CodigosBarras.Should().BeEmpty();
        produto.Embalagens.Should().BeEmpty();
    }

    [Fact]
    public void Criar_Produto_Sem_Nome_Deve_Falhar()
    {
        var act = () => Produto.Criar("", Guid.NewGuid(), UnidadeBase.Unidade, 10m);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Estoque_Critico_Maior_Que_Minimo_Deve_Falhar()
    {
        var act = () => Produto.Criar("Test", Guid.NewGuid(), UnidadeBase.Unidade, 10m,
            estoqueMinimo: 10, estoqueCritico: 20);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Adicionar_Embalagem_Com_Quantidade_Zero_Deve_Falhar()
    {
        // RN-007
        var produto = Produto.Criar("Test", Guid.NewGuid(), UnidadeBase.Unidade, 10m);

        var act = () => produto.AdicionarEmbalagem("Fardo", 0);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Embalagem_Deve_Converter_Para_Unidade_Base()
    {
        // RN-007
        var produto = Produto.Criar("Coca-Cola 350ml", Guid.NewGuid(), UnidadeBase.Unidade, 4.50m);
        var embalagem = produto.AdicionarEmbalagem("Fardo", 12);

        var unidades = embalagem.ConverterParaUnidadeBase(3); // 3 fardos

        unidades.Should().Be(36); // 3 × 12
    }

    [Fact]
    public void CodigoBarras_Principal_Deve_Ser_Unico()
    {
        var produto = Produto.Criar("Test", Guid.NewGuid(), UnidadeBase.Unidade, 10m);

        produto.AdicionarCodigoBarras("111", TipoCodigoBarras.EAN13, principal: true);
        produto.AdicionarCodigoBarras("222", TipoCodigoBarras.EAN13, principal: true);

        produto.CodigosBarras.Count(cb => cb.Principal).Should().Be(1);
        produto.CodigosBarras.First(cb => cb.Principal).Codigo.Should().Be("222");
    }

    [Fact]
    public void Inativar_Produto_Deve_Impedir_Uso()
    {
        // RN-017
        var produto = Produto.Criar("Test", Guid.NewGuid(), UnidadeBase.Unidade, 10m);
        produto.Inativar();

        produto.Ativo.Should().BeFalse();

        var act = () => produto.ValidarAtivo();
        act.Should().Throw<ProdutoInativoException>();
    }

    [Fact]
    public void CalcularAlerta_Deve_Retornar_Nivel_Correto()
    {
        var produto = Produto.Criar("Test", Guid.NewGuid(), UnidadeBase.Unidade, 10m,
            estoqueMinimo: 24, estoqueCritico: 6);

        produto.CalcularAlerta(50).Should().Be(NivelAlertaEstoque.Normal);
        produto.CalcularAlerta(24).Should().Be(NivelAlertaEstoque.Baixo);
        produto.CalcularAlerta(10).Should().Be(NivelAlertaEstoque.Baixo);
        produto.CalcularAlerta(6).Should().Be(NivelAlertaEstoque.Critico);
        produto.CalcularAlerta(2).Should().Be(NivelAlertaEstoque.Critico);
    }
}
