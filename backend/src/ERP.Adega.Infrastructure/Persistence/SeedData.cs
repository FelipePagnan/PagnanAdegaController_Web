using ERP.Adega.Domain.Entities;
using ERP.Adega.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ERP.Adega.Infrastructure.Persistence;

public static class SeedData
{
    public static async Task SeedAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AdegaDbContext>();

        if (await context.Empresas.AnyAsync())
            return;

        // === Empresa ===
        var empresa = Empresa.Criar("Adega Central LTDA", "12.345.678/0001-90", "Adega Central");
        await context.Empresas.AddAsync(empresa);

        // === Filial ===
        var filial = empresa.AdicionarFilial("Filial Centro", "FC01");

        // === Categorias ===
        var catCervejas = Categoria.Criar("Cervejas", "Cervejas nacionais e importadas");
        var catVinhos = Categoria.Criar("Vinhos", "Vinhos tintos, brancos e rosés");
        var catDestilados = Categoria.Criar("Destilados", "Whisky, vodka, gin, etc.");
        var catRefrigerantes = Categoria.Criar("Refrigerantes", "Refrigerantes e águas");
        var catEnergeticos = Categoria.Criar("Energéticos", "Bebidas energéticas");
        await context.Categorias.AddRangeAsync(catCervejas, catVinhos, catDestilados, catRefrigerantes, catEnergeticos);

        // === Fornecedores ===
        var fornAmbev = Fornecedor.Criar("Ambev S.A.", "07.526.557/0001-00", "Ambev");
        fornAmbev.Atualizar("Ambev S.A.", "Ambev",
            new Contato("(11) 2122-1200", null, "vendas@ambev.com.br", "Comercial Ambev"), null, "Principal fornecedor de cervejas");
        await context.Fornecedores.AddAsync(fornAmbev);

        var fornCoca = Fornecedor.Criar("Coca-Cola FEMSA Brasil Ltda", "45.997.418/0001-53", "Coca-Cola");
        fornCoca.Atualizar("Coca-Cola FEMSA Brasil Ltda", "Coca-Cola",
            new Contato("(11) 3030-6000", null, "pedidos@coca-cola.com.br", "Comercial Coca-Cola"), null, "Refrigerantes e sucos");
        await context.Fornecedores.AddAsync(fornCoca);

        var fornDistrib = Fornecedor.Criar("Distribuidora Beber Bem Ltda", "33.444.555/0001-66", "Beber Bem");
        fornDistrib.Atualizar("Distribuidora Beber Bem Ltda", "Beber Bem",
            new Contato("(44) 3344-5566", null, "compras@beberbem.com.br", "João Silva"), null, "Distribuidora regional");
        await context.Fornecedores.AddAsync(fornDistrib);

        // === Produtos de exemplo ===
        var prod1 = Produto.Criar("Heineken 600ml", catCervejas.Id, "un");
        prod1.AtualizarPrecoVenda(12.90m);
        prod1.AtualizarPrecoCusto(8.50m);
        prod1.DefinirEstoqueMinimo(24, 6);
        prod1.AdicionarCodigoBarras("7896045523153", TipoCodigoBarras.EAN13, true);
        prod1.AdicionarEmbalagem("Fardo", 12, 140.00m);
        await context.Produtos.AddAsync(prod1);

        var prod2 = Produto.Criar("Coca-Cola 350ml", catRefrigerantes.Id, "un");
        prod2.AtualizarPrecoVenda(4.50m);
        prod2.AtualizarPrecoCusto(3.20m);
        prod2.DefinirEstoqueMinimo(48, 12);
        prod2.AdicionarCodigoBarras("7894900010015", TipoCodigoBarras.EAN13, true);
        prod2.AdicionarEmbalagem("Fardo", 12, 48.00m);
        await context.Produtos.AddAsync(prod2);

        var prod3 = Produto.Criar("Red Bull 250ml", catEnergeticos.Id, "un");
        prod3.AtualizarPrecoVenda(9.90m);
        prod3.AtualizarPrecoCusto(7.50m);
        prod3.DefinirEstoqueMinimo(12, 4);
        prod3.AdicionarCodigoBarras("9002490100070", TipoCodigoBarras.EAN13, true);
        prod3.AdicionarEmbalagem("Fardo", 4, 36.00m);
        await context.Produtos.AddAsync(prod3);

        var prod4 = Produto.Criar("Vinho Miolo Merlot 750ml", catVinhos.Id, "un");
        prod4.AtualizarPrecoVenda(49.90m);
        prod4.AtualizarPrecoCusto(32.00m);
        prod4.DefinirEstoqueMinimo(6, 2);
        prod4.AdicionarCodigoBarras("7896756803506", TipoCodigoBarras.EAN13, true);
        prod4.AdicionarEmbalagem("Caixa", 6, 270.00m);
        await context.Produtos.AddAsync(prod4);

        var prod5 = Produto.Criar("Whisky Jack Daniel's 1L", catDestilados.Id, "un");
        prod5.AtualizarPrecoVenda(189.90m);
        prod5.AtualizarPrecoCusto(140.00m);
        prod5.DefinirEstoqueMinimo(3, 1);
        prod5.AdicionarCodigoBarras("0082184090466", TipoCodigoBarras.EAN13, true);
        await context.Produtos.AddAsync(prod5);

        var prod6 = Produto.Criar("Água Mineral 500ml", catRefrigerantes.Id, "un");
        prod6.AtualizarPrecoVenda(2.50m);
        prod6.AtualizarPrecoCusto(1.20m);
        prod6.DefinirEstoqueMinimo(96, 24);
        prod6.AdicionarCodigoBarras("7898357410015", TipoCodigoBarras.EAN13, true);
        prod6.AdicionarEmbalagem("Fardo", 24, 48.00m);
        await context.Produtos.AddAsync(prod6);

        await context.SaveChangesAsync();

        // === Estoque inicial para cada produto ===
        var produtos = new[] { prod1, prod2, prod3, prod4, prod5, prod6 };
        var quantidades = new[] { 48, 120, 24, 18, 6, 240 };

        for (int i = 0; i < produtos.Length; i++)
        {
            var estoque = EstoqueProduto.Criar(produtos[i].Id, filial.Id);
            estoque.Entrada(quantidades[i]);
            await context.Set<EstoqueProduto>().AddAsync(estoque);

            var lote = Lote.Criar(produtos[i].Id, filial.Id, $"SEED-{i + 1:000}",
                quantidades[i], produtos[i].PrecoCusto,
                DateTime.UtcNow.AddMonths(6 + i));
            await context.Set<Lote>().AddAsync(lote);

            await context.Set<MovimentacaoEstoque>().AddAsync(
                MovimentacaoEstoque.Criar(produtos[i].Id, filial.Id,
                    Domain.Enums.TipoMovimentacao.Entrada,
                    quantidades[i], 0, quantidades[i], Guid.Empty, lote.Id,
                    documentoOrigem: "Estoque inicial"));
        }

        // === Perfil Admin ===
        var perfilAdmin = Perfil.Criar("Administrador", empresa.Id, "Acesso total ao sistema", sistema: true);
        perfilAdmin.DefinirPermissoes(new[]
        {
            "dashboard.visualizar",
            "produtos.visualizar", "produtos.criar", "produtos.editar", "produtos.inativar",
            "estoque.visualizar", "estoque.ajustar", "estoque.transferir",
            "vendas.criar", "vendas.cancelar", "vendas.desconto", "vendas.visualizar",
            "compras.criar", "compras.aprovar", "compras.receber", "compras.visualizar",
            "financeiro.visualizar", "financeiro.pagar", "financeiro.receber",
            "clientes.visualizar", "clientes.criar", "clientes.editar",
            "fornecedores.visualizar", "fornecedores.criar", "fornecedores.editar",
            "reservas.criar", "reservas.cancelar", "reservas.visualizar",
            "relatorios.visualizar", "configuracoes.editar", "auditoria.visualizar",
            "usuarios.criar", "usuarios.editar"
        });
        await context.Perfis.AddAsync(perfilAdmin);

        // === Usuário Admin ===
        var senhaHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        var admin = Usuario.Criar("Administrador", "admin@adega.com", senhaHash, perfilAdmin.Id, empresa.Id);
        admin.AdicionarFilial(filial.Id);
        await context.Usuarios.AddAsync(admin);

        await context.SaveChangesAsync();
    }
}
