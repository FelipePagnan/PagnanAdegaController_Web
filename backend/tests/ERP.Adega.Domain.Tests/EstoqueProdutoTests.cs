using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.Enums;
using ERP.Adega.Domain.Exceptions;
using FluentAssertions;
using Xunit;

namespace ERP.Adega.Domain.Tests;

public class EstoqueProdutoTests
{
    private static EstoqueProduto CriarEstoque(int fisico = 100, int reservado = 0)
    {
        var estoque = EstoqueProduto.Criar(Guid.NewGuid(), Guid.NewGuid());
        if (fisico > 0) estoque.Entrada(fisico);
        if (reservado > 0) estoque.Reservar(reservado);
        return estoque;
    }

    [Fact]
    public void Disponivel_Deve_Ser_Fisico_Menos_Reservado()
    {
        // RN-002
        var estoque = CriarEstoque(fisico: 100, reservado: 20);

        estoque.EstoqueFisico.Should().Be(100);
        estoque.EstoqueReservado.Should().Be(20);
        estoque.EstoqueDisponivel.Should().Be(80);
    }

    [Fact]
    public void Saida_Deve_Bloquear_Se_Estoque_Insuficiente()
    {
        // RN-001
        var estoque = CriarEstoque(fisico: 100, reservado: 20);

        var act = () => estoque.Saida(90); // Disponível = 80

        act.Should().Throw<EstoqueInsuficienteException>();
    }

    [Fact]
    public void Saida_Deve_Reduzir_Estoque_Fisico()
    {
        var estoque = CriarEstoque(fisico: 100);

        estoque.Saida(30);

        estoque.EstoqueFisico.Should().Be(70);
        estoque.EstoqueDisponivel.Should().Be(70);
    }

    [Fact]
    public void Reservar_Deve_Reduzir_Disponivel_Sem_Alterar_Fisico()
    {
        // RN-013
        var estoque = CriarEstoque(fisico: 100);

        estoque.Reservar(25);

        estoque.EstoqueFisico.Should().Be(100);
        estoque.EstoqueReservado.Should().Be(25);
        estoque.EstoqueDisponivel.Should().Be(75);
    }

    [Fact]
    public void Reservar_Deve_Bloquear_Se_Maior_Que_Disponivel()
    {
        var estoque = CriarEstoque(fisico: 100, reservado: 80);
        // Disponível = 20

        var act = () => estoque.Reservar(25);

        act.Should().Throw<EstoqueInsuficienteException>();
    }

    [Fact]
    public void Entrada_Deve_Aumentar_Estoque_Fisico()
    {
        var estoque = CriarEstoque(fisico: 50);

        estoque.Entrada(100);

        estoque.EstoqueFisico.Should().Be(150);
    }

    [Fact]
    public void LiberarReserva_Deve_Aumentar_Disponivel()
    {
        var estoque = CriarEstoque(fisico: 100, reservado: 30);

        estoque.LiberarReserva(30);

        estoque.EstoqueReservado.Should().Be(0);
        estoque.EstoqueDisponivel.Should().Be(100);
    }

    [Fact]
    public void AjusteInventario_Deve_Bloquear_Se_Menor_Que_Reservado()
    {
        // RN-010
        var estoque = CriarEstoque(fisico: 100, reservado: 40);

        var act = () => estoque.AjustarInventario(30); // < reservado (40)

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void AjusteInventario_Deve_Bloquear_Valor_Negativo()
    {
        var estoque = CriarEstoque(fisico: 100);

        var act = () => estoque.AjustarInventario(-10);

        act.Should().Throw<EstoqueNegativoException>();
    }

    [Fact]
    public void CalcularFardosUnidades_Deve_Separar_Corretamente()
    {
        var estoque = CriarEstoque(fisico: 52);

        var (fardos, unidades) = estoque.CalcularFardosUnidades(12);

        fardos.Should().Be(4);
        unidades.Should().Be(4);
        // 4 × 12 + 4 = 52 ✓
    }
}
