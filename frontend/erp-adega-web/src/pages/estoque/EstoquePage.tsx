import { useState, useEffect, useCallback } from 'react';
import { Package, Plus, Minus, ClipboardList, AlertTriangle, History, Search, Calendar } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import type { EstoqueProduto, PagedResult, MovimentacaoEstoque as MovType, Lote } from '@/types';
import { NivelAlertaEstoque, TipoMovimentacao } from '@/types';
import api from '@/services/api';
import styles from './EstoquePage.module.css';

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

const tipoSaidaOptions = [
  { value: TipoMovimentacao.Perda, label: 'Perda' },
  { value: TipoMovimentacao.Dano, label: 'Dano' },
];

function useFilialId() {
  const [filialId, setFilialId] = useState('');
  useEffect(() => {
    const token = localStorage.getItem('erp_token');
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const filiais = Array.isArray(payload.filial_id) ? payload.filial_id : [payload.filial_id];
        if (filiais[0]) setFilialId(filiais[0]);
      } catch { /* ignore */ }
    }
  }, []);
  return filialId;
}

export function EstoquePage() {
  const [tab, setTab] = useState<'saldos' | 'entrada' | 'saida' | 'ajuste' | 'alertas' | 'lotes' | 'movimentacoes'>('saldos');
  const filialId = useFilialId();

  const tabs = [
    { id: 'saldos' as const, label: 'Saldos', icon: Package },
    { id: 'entrada' as const, label: 'Entrada', icon: Plus },
    { id: 'saida' as const, label: 'Saída', icon: Minus },
    { id: 'ajuste' as const, label: 'Ajuste', icon: ClipboardList },
    { id: 'alertas' as const, label: 'Alertas', icon: AlertTriangle },
    { id: 'lotes' as const, label: 'Lotes Vencendo', icon: Calendar },
    { id: 'movimentacoes' as const, label: 'Movimentações', icon: History },
  ];

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>Estoque</h1>
          <p className={styles.subtitle}>Controle de saldos, entradas, saídas e movimentações — FEFO ativo</p>
        </div>
      </div>

      <div className={styles.tabs}>
        {tabs.map(t => {
          const Icon = t.icon;
          return (
            <button key={t.id} className={`${styles.tab} ${tab === t.id ? styles.tabActive : ''}`}
              onClick={() => setTab(t.id)}>
              <Icon size={15} /> {t.label}
            </button>
          );
        })}
      </div>

      {tab === 'saldos' && <TabSaldos filialId={filialId} />}
      {tab === 'entrada' && <TabEntrada filialId={filialId} />}
      {tab === 'saida' && <TabSaida filialId={filialId} />}
      {tab === 'ajuste' && <TabAjuste filialId={filialId} />}
      {tab === 'alertas' && <TabAlertas filialId={filialId} />}
      {tab === 'lotes' && <TabLotesVencendo filialId={filialId} />}
      {tab === 'movimentacoes' && <TabMovimentacoes filialId={filialId} />}
    </div>
  );
}

// ════════════════════════════════════
// TAB SALDOS
// ════════════════════════════════════
function TabSaldos({ filialId }: { filialId: string }) {
  const [estoque, setEstoque] = useState<EstoqueProduto[]>([]);
  const [total, setTotal] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [termo, setTermo] = useState('');
  const [loading, setLoading] = useState(true);

  const carregar = useCallback(async () => {
    if (!filialId) return;
    setLoading(true);
    try {
      const params: Record<string, string> = { pagina: String(pagina), tamanhoPagina: '20' };
      if (termo) params.termo = termo;
      const { data } = await api.get<PagedResult<EstoqueProduto>>(`/estoque/${filialId}`, { params });
      setEstoque(data.items);
      setTotal(data.total);
    } catch (err) { console.error(err); }
    finally { setLoading(false); }
  }, [filialId, pagina, termo]);

  useEffect(() => { carregar(); }, [carregar]);

  const totalPaginas = Math.ceil(total / 20);

  return (
    <>
      <div className={styles.searchBar}>
        <Input placeholder="Buscar produto..." icon={<Search size={16} />}
          value={termo} onChange={e => setTermo(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') { setPagina(1); carregar(); } }} />
        <Button variant="secondary" size="sm" onClick={() => { setPagina(1); carregar(); }}>Buscar</Button>
      </div>

      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Produto</th><th>Físico</th><th>Reservado</th><th>Disponível</th><th>Fardos + Un</th><th>Status</th>
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
  );
}

// ════════════════════════════════════
// TAB ENTRADA
// ════════════════════════════════════
function TabEntrada({ filialId }: { filialId: string }) {
  const [produtoId, setProdutoId] = useState('');
  const [qtd, setQtd] = useState('');
  const [custo, setCusto] = useState('');
  const [lote, setLote] = useState('');
  const [validade, setValidade] = useState('');
  const [nf, setNf] = useState('');
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState('');
  const [sucesso, setSucesso] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErro(''); setSucesso(''); setSalvando(true);
    try {
      await api.post('/estoque/entrada', {
        produtoId, filialId,
        quantidade: parseInt(qtd), custoUnitario: parseFloat(custo),
        codigoLote: lote || null, dataValidade: validade || null, notaFiscal: nf || null,
      });
      setSucesso(`Entrada de ${qtd} unidades registrada com sucesso!`);
      setProdutoId(''); setQtd(''); setCusto(''); setLote(''); setValidade(''); setNf('');
    } catch (err: any) {
      setErro(err.response?.data?.erro || 'Erro ao registrar entrada.');
    } finally { setSalvando(false); }
  };

  return (
    <div className={styles.formCard}>
      <h2 className={styles.formTitle}>Registrar Entrada de Estoque</h2>
      <p className={styles.formDesc}>Cria um lote com rastreabilidade e alimenta o estoque automaticamente.</p>
      <form onSubmit={handleSubmit} className={styles.form}>
        <Input label="ID do Produto *" placeholder="Cole o ID do produto" value={produtoId}
          onChange={e => setProdutoId(e.target.value)} required />
        <div className={styles.grid2}>
          <Input label="Quantidade *" placeholder="Ex: 48" type="number" value={qtd}
            onChange={e => setQtd(e.target.value)} required />
          <Input label="Custo Unitário *" placeholder="Ex: 3.50" type="number" step="0.01" value={custo}
            onChange={e => setCusto(e.target.value)} required />
        </div>
        <div className={styles.grid2}>
          <Input label="Código do Lote" placeholder="Gerado automaticamente se vazio" value={lote}
            onChange={e => setLote(e.target.value)} />
          <Input label="Data de Validade" type="date" value={validade}
            onChange={e => setValidade(e.target.value)} />
        </div>
        <Input label="Nota Fiscal" placeholder="Número da NF" value={nf}
          onChange={e => setNf(e.target.value)} />
        {erro && <div className={styles.error}>{erro}</div>}
        {sucesso && <div className={styles.success}>{sucesso}</div>}
        <Button type="submit" variant="secondary" icon={<Plus size={16} />} loading={salvando}>
          Registrar Entrada
        </Button>
      </form>
    </div>
  );
}

// ════════════════════════════════════
// TAB SAÍDA (Perda / Dano)
// ════════════════════════════════════
function TabSaida({ filialId }: { filialId: string }) {
  const [produtoId, setProdutoId] = useState('');
  const [qtd, setQtd] = useState('');
  const [tipo, setTipo] = useState<TipoMovimentacao>(TipoMovimentacao.Perda);
  const [motivo, setMotivo] = useState('');
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState('');
  const [sucesso, setSucesso] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErro(''); setSucesso(''); setSalvando(true);
    try {
      await api.post('/estoque/saida', {
        produtoId, filialId,
        quantidade: parseInt(qtd), tipo, motivo,
      });
      setSucesso(`Saída de ${qtd} unidades registrada como ${tipoLabel[tipo]}.`);
      setProdutoId(''); setQtd(''); setMotivo('');
    } catch (err: any) {
      setErro(err.response?.data?.erro || 'Erro ao registrar saída.');
    } finally { setSalvando(false); }
  };

  return (
    <div className={styles.formCard}>
      <h2 className={styles.formTitle}>Registrar Saída de Estoque</h2>
      <p className={styles.formDesc}>Perda ou dano — baixa estoque com FEFO e exige motivo obrigatório (RN-012).</p>
      <form onSubmit={handleSubmit} className={styles.form}>
        <Input label="ID do Produto *" placeholder="Cole o ID do produto" value={produtoId}
          onChange={e => setProdutoId(e.target.value)} required />
        <div className={styles.grid2}>
          <Input label="Quantidade *" placeholder="Ex: 5" type="number" value={qtd}
            onChange={e => setQtd(e.target.value)} required />
          <div className={styles.field}>
            <label className={styles.label}>Tipo de Saída *</label>
            <select className={styles.select} value={tipo} onChange={e => setTipo(Number(e.target.value))}>
              {tipoSaidaOptions.map(o => (
                <option key={o.value} value={o.value}>{o.label}</option>
              ))}
            </select>
          </div>
        </div>
        <Input label="Motivo *" placeholder="Descreva o motivo da saída" value={motivo}
          onChange={e => setMotivo(e.target.value)} required />
        {erro && <div className={styles.error}>{erro}</div>}
        {sucesso && <div className={styles.success}>{sucesso}</div>}
        <Button type="submit" variant="danger" icon={<Minus size={16} />} loading={salvando}>
          Registrar Saída
        </Button>
      </form>
    </div>
  );
}

// ════════════════════════════════════
// TAB AJUSTE (Inventário)
// ════════════════════════════════════
function TabAjuste({ filialId }: { filialId: string }) {
  const [produtoId, setProdutoId] = useState('');
  const [novaQtd, setNovaQtd] = useState('');
  const [motivo, setMotivo] = useState('');
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState('');
  const [sucesso, setSucesso] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErro(''); setSucesso(''); setSalvando(true);
    try {
      await api.post('/estoque/ajuste', {
        produtoId, filialId,
        novaQuantidade: parseInt(novaQtd), motivo,
      });
      setSucesso(`Estoque ajustado para ${novaQtd} unidades.`);
      setProdutoId(''); setNovaQtd(''); setMotivo('');
    } catch (err: any) {
      setErro(err.response?.data?.erro || 'Erro ao ajustar estoque.');
    } finally { setSalvando(false); }
  };

  return (
    <div className={styles.formCard}>
      <h2 className={styles.formTitle}>Ajuste de Inventário</h2>
      <p className={styles.formDesc}>Corrige a quantidade física após contagem. Gera movimentação rastreável (RN-010). Motivo obrigatório (RN-012).</p>
      <form onSubmit={handleSubmit} className={styles.form}>
        <Input label="ID do Produto *" placeholder="Cole o ID do produto" value={produtoId}
          onChange={e => setProdutoId(e.target.value)} required />
        <Input label="Nova Quantidade Física *" placeholder="Quantidade real contada" type="number" value={novaQtd}
          onChange={e => setNovaQtd(e.target.value)} required
          helper="O sistema calculará a diferença automaticamente" />
        <Input label="Motivo *" placeholder="Ex: Divergência encontrada no inventário" value={motivo}
          onChange={e => setMotivo(e.target.value)} required />
        {erro && <div className={styles.error}>{erro}</div>}
        {sucesso && <div className={styles.success}>{sucesso}</div>}
        <Button type="submit" variant="gold" icon={<ClipboardList size={16} />} loading={salvando}>
          Registrar Ajuste
        </Button>
      </form>
    </div>
  );
}

// ════════════════════════════════════
// TAB ALERTAS
// ════════════════════════════════════
function TabAlertas({ filialId }: { filialId: string }) {
  const [alertas, setAlertas] = useState<EstoqueProduto[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!filialId) return;
    setLoading(true);
    api.get(`/estoque/alertas/${filialId}`)
      .then(({ data }) => setAlertas(data))
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [filialId]);

  return (
    <div className={styles.tableWrapper}>
      <table className={styles.table}>
        <thead>
          <tr><th>Produto</th><th>Disponível</th><th>Status</th></tr>
        </thead>
        <tbody>
          {loading ? (
            <tr><td colSpan={3} className={styles.loading}>Carregando...</td></tr>
          ) : alertas.length === 0 ? (
            <tr><td colSpan={3} className={styles.empty}>
              <span style={{ color: '#2D8A4E' }}>✓ Nenhum alerta — todos os produtos com estoque normal</span>
            </td></tr>
          ) : alertas.map(a => {
            const nivel = nivelLabel[a.nivelAlerta] || nivelLabel[1];
            return (
              <tr key={a.id}>
                <td className={styles.prodNome}>{a.produtoNome}</td>
                <td className={styles.numBold}>{a.estoqueDisponivel} un</td>
                <td><Badge label={nivel.label} variant={nivel.variant as any} /></td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

// ════════════════════════════════════
// TAB LOTES VENCENDO
// ════════════════════════════════════
function TabLotesVencendo({ filialId }: { filialId: string }) {
  const [lotes, setLotes] = useState<Lote[]>([]);
  const [dias, setDias] = useState('30');
  const [loading, setLoading] = useState(true);

  const carregar = useCallback(async () => {
    if (!filialId) return;
    setLoading(true);
    try {
      const { data } = await api.get(`/estoque/lotes-vencendo/${filialId}`, { params: { dias } });
      setLotes(data);
    } catch (err) { console.error(err); }
    finally { setLoading(false); }
  }, [filialId, dias]);

  useEffect(() => { carregar(); }, [carregar]);

  return (
    <>
      <div className={styles.searchBar}>
        <Input placeholder="Dias para vencer" type="number" value={dias}
          onChange={e => setDias(e.target.value)} />
        <Button variant="secondary" size="sm" onClick={carregar}>Filtrar</Button>
      </div>

      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr><th>Lote</th><th>Produto</th><th>Validade</th><th>Qtd Atual</th><th>Fornecedor</th><th>Status</th></tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={6} className={styles.loading}>Carregando...</td></tr>
            ) : lotes.length === 0 ? (
              <tr><td colSpan={6} className={styles.empty}>
                <span style={{ color: '#2D8A4E' }}>✓ Nenhum lote vencendo nos próximos {dias} dias</span>
              </td></tr>
            ) : lotes.map(l => (
              <tr key={l.id}>
                <td className={styles.mono}>{l.codigo}</td>
                <td className={styles.prodNome}>{l.produtoId.substring(0, 8)}...</td>
                <td className={styles.mono}>
                  {l.dataValidade ? new Date(l.dataValidade).toLocaleDateString('pt-BR') : '—'}
                </td>
                <td className={styles.numBold}>{l.quantidadeAtual}</td>
                <td>{l.fornecedorNome || '—'}</td>
                <td>
                  <Badge label={l.vencido ? 'Vencido' : 'Vencendo'}
                    variant={l.vencido ? 'critical' : 'expiring'} />
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </>
  );
}

// ════════════════════════════════════
// TAB MOVIMENTAÇÕES
// ════════════════════════════════════
function TabMovimentacoes({ filialId }: { filialId: string }) {
  const [produtoId, setProdutoId] = useState('');
  const [movimentos, setMovimentos] = useState<MovType[]>([]);
  const [loading, setLoading] = useState(false);

  const buscar = async () => {
    if (!produtoId || !filialId) return;
    setLoading(true);
    try {
      const { data } = await api.get(`/estoque/movimentacoes/${produtoId}/${filialId}`);
      setMovimentos(data.items || []);
    } catch (err) { console.error(err); }
    finally { setLoading(false); }
  };

  return (
    <>
      <div className={styles.searchBar}>
        <Input placeholder="ID do produto para ver histórico..." value={produtoId}
          onChange={e => setProdutoId(e.target.value)}
          onKeyDown={e => { if (e.key === 'Enter') buscar(); }} />
        <Button variant="secondary" size="sm" onClick={buscar}>Buscar</Button>
      </div>

      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr><th>Data</th><th>Tipo</th><th>Qtd</th><th>Anterior</th><th>Posterior</th><th>Lote</th><th>Motivo</th><th>Usuário</th></tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={8} className={styles.loading}>Carregando...</td></tr>
            ) : movimentos.length === 0 ? (
              <tr><td colSpan={8} className={styles.empty}>Informe o ID de um produto e clique Buscar</td></tr>
            ) : movimentos.map(m => (
              <tr key={m.id}>
                <td className={styles.mono}>{new Date(m.criadoEm).toLocaleString('pt-BR')}</td>
                <td>
                  <Badge label={tipoLabel[m.tipo] || String(m.tipo)}
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
  );
}
