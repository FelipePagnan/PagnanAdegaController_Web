using ERP.Adega.Domain.Entities;
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

        // Empresa
        var empresa = Empresa.Criar("Adega Central LTDA", "12.345.678/0001-90", "Adega Central");
        await context.Empresas.AddAsync(empresa);

        // Filial
        var filial = empresa.AdicionarFilial("Filial Centro", "FC01");

        // Categorias
        var catCervejas = Categoria.Criar("Cervejas", "Cervejas nacionais e importadas");
        var catVinhos = Categoria.Criar("Vinhos", "Vinhos tintos, brancos e rosés");
        var catDestilados = Categoria.Criar("Destilados", "Whisky, vodka, gin, etc.");
        var catRefrigerantes = Categoria.Criar("Refrigerantes", "Refrigerantes e águas");
        var catEnergeticos = Categoria.Criar("Energéticos", "Bebidas energéticas");
        await context.Categorias.AddRangeAsync(catCervejas, catVinhos, catDestilados, catRefrigerantes, catEnergeticos);

        // Perfil Admin
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
            "reservas.criar", "reservas.cancelar", "reservas.visualizar",
            "relatorios.visualizar", "configuracoes.editar", "auditoria.visualizar",
            "usuarios.criar", "usuarios.editar"
        });
        await context.Perfis.AddAsync(perfilAdmin);

        // Usuário Admin (admin@adega.com / admin123)
        var senhaHash = BCrypt.Net.BCrypt.HashPassword("admin123");
        var admin = Usuario.Criar("Administrador", "admin@adega.com", senhaHash, perfilAdmin.Id, empresa.Id);
        admin.AdicionarFilial(filial.Id);
        await context.Usuarios.AddAsync(admin);

        await context.SaveChangesAsync();
    }
}
