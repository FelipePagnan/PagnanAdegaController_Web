import { useState, useEffect, useCallback } from 'react';
import { useNavigate } from 'react-router-dom';
import { Plus, Search, Wine, BarChart3, Edit, Power } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import type { ProdutoResumo, PagedResult, Categoria } from '@/types';
import api from '@/services/api';
import styles from './ProdutosPage.module.css';

export function ProdutosPage() {
  const [produtos, setProdutos] = useState<ProdutoResumo[]>([]);
  const [total, setTotal] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [termo, setTermo] = useState('');
  const [filtroAtivo, setFiltroAtivo] = useState<string>('todos');
  const [loading, setLoading] = useState(true);
  const navigate = useNavigate();

  const carregarProdutos = useCallback(async () => {
    setLoading(true);
    try {
      const params: Record<string, string> = { pagina: String(pagina), tamanhoPagina: '15' };
      if (termo) params.termo = termo;
      if (filtroAtivo === 'ativos') params.ativo = 'true';
      if (filtroAtivo === 'inativos') params.ativo = 'false';

      const { data } = await api.get<PagedResult<ProdutoResumo>>('/produtos', { params });
      setProdutos(data.items);
      setTotal(data.total);
    } catch (err) {
      console.error('Erro ao carregar produtos:', err);
    } finally {
      setLoading(false);
    }
  }, [pagina, termo, filtroAtivo]);

  useEffect(() => { carregarProdutos(); }, [carregarProdutos]);

  const handleBuscar = (e: React.FormEvent) => {
    e.preventDefault();
    setPagina(1);
    carregarProdutos();
  };

  const handleInativar = async (id: string) => {
    if (!confirm('Deseja inativar este produto?')) return;
    try {
      await api.patch(`/produtos/${id}/inativar`);
      carregarProdutos();
    } catch (err) {
      console.error('Erro ao inativar:', err);
    }
  };

  const totalPaginas = Math.ceil(total / 15);

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>Produtos</h1>
          <p className={styles.subtitle}>{total} produtos cadastrados</p>
        </div>
        <Button variant="primary" icon={<Plus size={16} />} onClick={() => navigate('/produtos/novo')}>
          Novo Produto
        </Button>
      </div>

      {/* Filters */}
      <div className={styles.filters}>
        <form onSubmit={handleBuscar} className={styles.searchForm}>
          <Input
            placeholder="Buscar por nome ou código de barras..."
            icon={<Search size={16} />}
            value={termo}
            onChange={(e) => setTermo(e.target.value)}
          />
          <Button type="submit" variant="secondary" size="sm">Buscar</Button>
        </form>
        <div className={styles.filterTabs}>
          {(['todos', 'ativos', 'inativos'] as const).map(f => (
            <button key={f} className={`${styles.tab} ${filtroAtivo === f ? styles.tabActive : ''}`}
              onClick={() => { setFiltroAtivo(f); setPagina(1); }}>
              {f.charAt(0).toUpperCase() + f.slice(1)}
            </button>
          ))}
        </div>
      </div>

      {/* Table */}
      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Produto</th>
              <th>Categoria</th>
              <th>Preço</th>
              <th>Status</th>
              <th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={5} className={styles.loading}>Carregando...</td></tr>
            ) : produtos.length === 0 ? (
              <tr><td colSpan={5} className={styles.empty}>
                <Wine size={32} strokeWidth={1.5} />
                <span>Nenhum produto encontrado</span>
              </td></tr>
            ) : produtos.map(p => (
              <tr key={p.id}>
                <td>
                  <span className={styles.produtoNome}>{p.nome}</span>
                </td>
                <td>{p.categoriaNome}</td>
                <td className={styles.preco}>R$ {p.precoVenda.toFixed(2)}</td>
                <td>
                  <Badge label={p.ativo ? 'Ativo' : 'Inativo'} variant={p.ativo ? 'success' : 'inactive'} />
                </td>
                <td>
                  <div className={styles.actions}>
                    <button className={styles.actionBtn} title="Editar"
                      onClick={() => navigate(`/produtos/${p.id}`)}>
                      <Edit size={15} />
                    </button>
                    {p.ativo && (
                      <button className={styles.actionBtn} title="Inativar"
                        onClick={() => handleInativar(p.id)}>
                        <Power size={15} />
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>

      {/* Pagination */}
      {totalPaginas > 1 && (
        <div className={styles.pagination}>
          <Button variant="ghost" size="sm" disabled={pagina <= 1}
            onClick={() => setPagina(p => p - 1)}>Anterior</Button>
          <span className={styles.pageInfo}>Página {pagina} de {totalPaginas}</span>
          <Button variant="ghost" size="sm" disabled={pagina >= totalPaginas}
            onClick={() => setPagina(p => p + 1)}>Próxima</Button>
        </div>
      )}
    </div>
  );
}
