import { useState, useEffect, useCallback } from 'react';
import { Package, AlertTriangle, Plus, History, Search } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import type { EstoqueProduto, PagedResult, MovimentacaoEstoque as MovType } from '@/types';
import { NivelAlertaEstoque, TipoMovimentacao } from '@/types';
import api from '@/services/api';
import styles from './EstoquePage.module.css';

const filialId = ''; // será preenchido dinamicamente

const nivelLabel: Record<number, { label: string; variant: string }> = {
  [NivelAlertaEstoque.Normal]: { label: 'Normal', variant: 'success' },
  [NivelAlertaEstoque.Baixo]: { label: 'Baixo', variant: 'warning' },
  [NivelAlertaEstoque.Critico]: { label: 'Crítico', variant: 'critical' },
  [NivelAlertaEstoque.Vencendo]: { label: 'Vencendo', variant: 'expiring' },
  [NivelAlertaEstoque.Vencido]: { label: 'Vencido', variant: 'critical' },
};

const tipoLabel: Record<number, string> = {
  [TipoMovimentacao.Entrada]: 'Entrada',
  [TipoMovimentacao.Venda]: 'Venda',
  [TipoMovimentacao.Devolucao]: 'Devolução',
  [TipoMovimentacao.Perda]: 'Perda',
  [TipoMovimentacao.Dano]: 'Dano',
  [TipoMovimentacao.Ajuste]: 'Ajuste',
  [TipoMovimentacao.Transferencia]: 'Transferência',
  [TipoMovimentacao.Reserva]: 'Reserva',
  [TipoMovimentacao.LiberacaoReserva]: 'Liberação',
};

export function EstoquePage() {
  const [tab, setTab] = useState<'saldos' | 'entrada' | 'movimentacoes'>('saldos');
  const [estoque, setEstoque] = useState<EstoqueProduto[]>([]);
  const [total, setTotal] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [termo, setTermo] = useState('');
  const [loading, setLoading] = useState(true);
  const [userFilialId, setUserFilialId] = useState('');

  // Entrada form
  const [entProdutoId, setEntProdutoId] = useState('');
  const [entQtd, setEntQtd] = useState('');
  const [entCusto, setEntCusto] = useState('');
  const [entLote, setEntLote] = useState('');
  const [entValidade, setEntValidade] = useState('');
  const [entNF, setEntNF] = useState('');
  const [entSalvando, setEntSalvando] = useState(false);
  const [entErro, setEntErro] = useState('');
  const [entSucesso, setEntSucesso] = useState('');

  // Movimentações
  const [movProdutoId, setMovProdutoId] = useState('');
  const [movimentos, setMovimentos] = useState<MovType[]>([]);

  useEffect(() => {
    // Pegar filial do token
    const token = localStorage.getItem('erp_token');
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const filiais = Array.isArray(payload.filial_id) ? payload.filial_id : [payload.filial_id];
        if (filiais[0]) setUserFilialId(filiais[0]);
      } catch {}
    }
  }, []);

  const carregarEstoque = useCallback(async () => {
    if (!userFilialId) return;
    setLoading(true);
    try {
      const params: Record<string, string> = { pagina: String(pagina), tamanhoPagina: '20' };
      if (termo) params.termo = termo;
      const { data } = await api.get<PagedResult<EstoqueProduto>>(`/estoque/${userFilialId}`, { params });
      setEstoque(data.items);
      setTotal(data.total);
    } catch (err) {
      console.error(err);
    } finally {
      setLoading(false);
    }
  }, [userFilialId, pagina, termo]);

  useEffect(() => { if (tab === 'saldos') carregarEstoque(); }, [carregarEstoque, tab]);

  const handleEntrada = async (e: React.FormEvent) => {
    e.preventDefault();
    setEntErro(''); setEntSucesso(''); setEntSalvando(true);
    try {
      await api.post('/estoque/entrada', {
        produtoId: entProdutoId, filialId: userFilialId,
        quantidade: parseInt(entQtd), custoUnitario: parseFloat(entCusto),
        codigoLote: entLote || null,
        dataValidade: entValidade || null,
        notaFiscal: entNF || null,
      });
      setEntSucesso(`Entrada de ${entQtd} unidades registrada com sucesso!`);
      setEntProdutoId(''); setEntQtd(''); setEntCusto(''); setEntLote(''); setEntValidade(''); setEntNF('');
    } catch (err: any) {
      setEntErro(err.response?.data?.erro || 'Erro ao registrar entrada.');
    } finally {
      setEntSalvando(false);
    }
  };

  const buscarMovimentacoes = async () => {
    if (!movProdutoId || !userFilialId) return;
    try {
      const { data } = await api.get(`/estoque/movimentacoes/${movProdutoId}/${userFilialId}`);
      setMovimentos(data.items || []);
    } catch (err) {
      console.error(err);
    }
  };

  const totalPaginas = Math.ceil(total / 20);

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>Estoque</h1>
          <p className={styles.subtitle}>Controle de saldos, entradas e movimentações — FEFO ativo</p>
        </div>
      </div>

      {/* Tabs */}
      <div className={styles.tabs}>
        {([
          { id: 'saldos', label: 'Saldos', icon: Package },
          { id: 'entrada', label: 'Nova Entrada', icon: Plus },
          { id: 'movimentacoes', label: 'Movimentações', icon: History },
        ] as const).map(t => {
          const Icon = t.icon;
          return (
            <button key={t.id} className={`${styles.tab} ${tab === t.id ? styles.tabActive : ''}`}
              onClick={() => setTab(t.id)}>
              <Icon size={15} /> {t.label}
            </button>
          );
        })}
      </div>

      {/* === SALDOS === */}
      {tab === 'saldos' && (
        <>
          <div className={styles.searchBar}>
            <Input placeholder="Buscar produto..." icon={<Search size={16} />}
              value={termo} onChange={e => setTermo(e.target.value)} />
            <Button variant="secondary" size="sm" onClick={() => { setPagina(1); carregarEstoque(); }}>Buscar</Button>
          </div>

          <div className={styles.tableWrapper}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Produto</th>
                  <th>Físico</th>
                  <th>Reservado</th>
                  <th>Disponível</th>
                  <th>Fardos + Un</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={6} className={styles.loading}>Carregando...</td></tr>
                ) : estoque.length === 0 ? (
                  <tr><td colSpan={6} className={styles.empty}>Nenhum produto em estoque</td></tr>
                ) : estoque.map(e => {
                  const nivel = nivelLabel[e.nivelAlerta] || nivelLabel[1];
                  return (
                    <tr key={e.id}>
                      <td className={styles.prodNome}>{e.produtoNome}</td>
                      <td className={styles.num}>{e.estoqueFisico}</td>
                      <td className={styles.num}>{e.estoqueReservado}</td>
                      <td className={styles.numBold}>{e.estoqueDisponivel}</td>
                      <td className={styles.fardos}>
                        {e.fardos != null ? `${e.fardos} fardos + ${e.unidadesRestantes} un` : `${e.estoqueFisico} un`}
                      </td>
                      <td><Badge label={nivel.label} variant={nivel.variant as any} /></td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>

          {totalPaginas > 1 && (
            <div className={styles.pagination}>
              <Button variant="ghost" size="sm" disabled={pagina <= 1} onClick={() => setPagina(p => p - 1)}>Anterior</Button>
              <span className={styles.pageInfo}>Página {pagina} de {totalPaginas}</span>
              <Button variant="ghost" size="sm" disabled={pagina >= totalPaginas} onClick={() => setPagina(p => p + 1)}>Próxima</Button>
            </div>
          )}
        </>
      )}

      {/* === ENTRADA === */}
      {tab === 'entrada' && (
        <div className={styles.formCard}>
          <h2 className={styles.formTitle}>Registrar Entrada de Estoque</h2>
          <p className={styles.formDesc}>A entrada cria um lote com rastreabilidade e alimenta o estoque automaticamente.</p>

          <form onSubmit={handleEntrada} className={styles.form}>
            <Input label="ID do Produto *" placeholder="Cole o ID do produto" value={entProdutoId}
              onChange={e => setEntProdutoId(e.target.value)} required />
            <div className={styles.grid2}>
              <Input label="Quantidade *" placeholder="Ex: 48" type="number" value={entQtd}
                onChange={e => setEntQtd(e.target.value)} required />
              <Input label="Custo Unitário *" placeholder="Ex: 3.50" type="number" step="0.01" value={entCusto}
                onChange={e => setEntCusto(e.target.value)} required />
            </div>
            <div className={styles.grid2}>
              <Input label="Código do Lote" placeholder="Gerado automaticamente se vazio" value={entLote}
                onChange={e => setEntLote(e.target.value)} />
              <Input label="Data de Validade" type="date" value={entValidade}
                onChange={e => setEntValidade(e.target.value)} />
            </div>
            <Input label="Nota Fiscal" placeholder="Número da NF" value={entNF}
              onChange={e => setEntNF(e.target.value)} />

            {entErro && <div className={styles.error}>{entErro}</div>}
            {entSucesso && <div className={styles.success}>{entSucesso}</div>}

            <Button type="submit" variant="secondary" icon={<Plus size={16} />} loading={entSalvando}>
              Registrar Entrada
            </Button>
          </form>
        </div>
      )}

      {/* === MOVIMENTAÇÕES === */}
      {tab === 'movimentacoes' && (
        <>
          <div className={styles.searchBar}>
            <Input placeholder="ID do produto para ver histórico..." value={movProdutoId}
              onChange={e => setMovProdutoId(e.target.value)} />
            <Button variant="secondary" size="sm" onClick={buscarMovimentacoes}>Buscar</Button>
          </div>

          <div className={styles.tableWrapper}>
            <table className={styles.table}>
              <thead>
                <tr>
                  <th>Data</th>
                  <th>Tipo</th>
                  <th>Qtd</th>
                  <th>Saldo Anterior</th>
                  <th>Saldo Posterior</th>
                  <th>Lote</th>
                  <th>Motivo</th>
                  <th>Usuário</th>
                </tr>
              </thead>
              <tbody>
                {movimentos.length === 0 ? (
                  <tr><td colSpan={8} className={styles.empty}>Informe o ID de um produto para ver o histórico</td></tr>
                ) : movimentos.map(m => (
                  <tr key={m.id}>
                    <td className={styles.mono}>{new Date(m.criadoEm).toLocaleString('pt-BR')}</td>
                    <td>
                      <Badge label={tipoLabel[m.tipo] || m.tipo.toString()}
                        variant={m.quantidade > 0 ? 'success' : 'critical'} />
                    </td>
                    <td className={`${styles.numBold} ${m.quantidade > 0 ? styles.positivo : styles.negativo}`}>
                      {m.quantidade > 0 ? '+' : ''}{m.quantidade}
                    </td>
                    <td className={styles.num}>{m.saldoAnterior}</td>
                    <td className={styles.numBold}>{m.saldoPosterior}</td>
                    <td className={styles.mono}>{m.loteCodigo || '—'}</td>
                    <td>{m.motivo || '—'}</td>
                    <td>{m.usuarioNome}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
}
