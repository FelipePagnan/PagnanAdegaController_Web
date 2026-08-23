import { useState, useEffect, useCallback } from 'react';
import { DollarSign, Wallet, ArrowUpCircle, ArrowDownCircle, BarChart3, Plus, Check, X } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import api from '@/services/api';
import styles from './FinanceiroPage.module.css';

const statusLabel: Record<number, { label: string; variant: string }> = {
  1: { label: 'Aberta', variant: 'warning' },
  2: { label: 'Paga', variant: 'success' },
  3: { label: 'Vencida', variant: 'critical' },
  4: { label: 'Cancelada', variant: 'inactive' },
};

function useFilialId() {
  const [id, setId] = useState('');
  useEffect(() => {
    const token = localStorage.getItem('erp_token');
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const f = Array.isArray(payload.filial_id) ? payload.filial_id : [payload.filial_id];
        if (f[0]) setId(f[0]);
      } catch {}
    }
  }, []);
  return id;
}

export function FinanceiroPage() {
  const [tab, setTab] = useState<'caixa' | 'pagar' | 'receber' | 'fluxo'>('caixa');
  const filialId = useFilialId();

  const tabs = [
    { id: 'caixa' as const, label: 'Caixa', icon: Wallet },
    { id: 'pagar' as const, label: 'Contas a Pagar', icon: ArrowUpCircle },
    { id: 'receber' as const, label: 'Contas a Receber', icon: ArrowDownCircle },
    { id: 'fluxo' as const, label: 'Fluxo de Caixa', icon: BarChart3 },
  ];

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1 className={styles.title}>Financeiro</h1>
        <p className={styles.subtitle}>Caixa, contas a pagar/receber e fluxo de caixa</p>
      </div>
      <div className={styles.tabs}>
        {tabs.map(t => {
          const Icon = t.icon;
          return (
            <button key={t.id} className={`${styles.tab} ${tab === t.id ? styles.tabActive : ''}`}
              onClick={() => setTab(t.id)}><Icon size={15} /> {t.label}</button>
          );
        })}
      </div>
      {tab === 'caixa' && <TabCaixa filialId={filialId} />}
      {tab === 'pagar' && <TabContasPagar filialId={filialId} />}
      {tab === 'receber' && <TabContasReceber filialId={filialId} />}
      {tab === 'fluxo' && <TabFluxo filialId={filialId} />}
    </div>
  );
}

// ════════ CAIXA ════════
function TabCaixa({ filialId }: { filialId: string }) {
  const [caixa, setCaixa] = useState<any>(null);
  const [loading, setLoading] = useState(true);
  const [saldoAbertura, setSaldoAbertura] = useState('200');
  const [obsFechar, setObsFechar] = useState('');
  const [erro, setErro] = useState('');
  const [msg, setMsg] = useState('');

  const carregar = useCallback(async () => {
    if (!filialId) return;
    setLoading(true);
    try {
      const { data } = await api.get(`/caixa/atual/${filialId}`);
      setCaixa(data);
    } catch { setCaixa(null); }
    finally { setLoading(false); }
  }, [filialId]);

  useEffect(() => { carregar(); }, [carregar]);

  const abrir = async () => {
    setErro(''); setMsg('');
    try {
      await api.post('/caixa/abrir', { filialId, saldoAbertura: parseFloat(saldoAbertura) });
      setMsg('Caixa aberto com sucesso!');
      carregar();
    } catch (err: any) { setErro(err.response?.data?.erro || 'Erro ao abrir caixa.'); }
  };

  const fechar = async () => {
    setErro(''); setMsg('');
    try {
      const { data } = await api.post(`/caixa/fechar/${filialId}`, { observacao: obsFechar || null });
      setMsg(`Caixa #${data.numero} fechado. Saldo: R$ ${data.saldoFechamento.toFixed(2)}`);
      setCaixa(null);
    } catch (err: any) { setErro(err.response?.data?.erro || 'Erro ao fechar caixa.'); }
  };

  if (loading) return <p className={styles.loading}>Carregando...</p>;

  if (!caixa) return (
    <div className={styles.formCard}>
      <h2 className={styles.formTitle}>Abrir Caixa</h2>
      <p className={styles.formDesc}>Nenhum caixa aberto nesta filial. Informe o saldo inicial.</p>
      <div className={styles.form}>
        <Input label="Saldo de Abertura (R$)" type="number" step="0.01" value={saldoAbertura}
          onChange={e => setSaldoAbertura(e.target.value)} />
        {erro && <div className={styles.error}>{erro}</div>}
        {msg && <div className={styles.success}>{msg}</div>}
        <Button variant="secondary" icon={<Wallet size={16} />} onClick={abrir}>Abrir Caixa</Button>
      </div>
    </div>
  );

  return (
    <div className={styles.formCard} style={{ maxWidth: 600 }}>
      <div className={styles.caixaHeader}>
        <h2 className={styles.formTitle}>Caixa #{caixa.numero}</h2>
        <Badge label="Aberto" variant="success" />
      </div>
      <div className={styles.kpiRow}>
        <div className={styles.kpi}><span className={styles.kpiLabel}>Abertura</span><span className={styles.kpiVal}>R$ {caixa.saldoAbertura.toFixed(2)}</span></div>
        <div className={styles.kpi}><span className={styles.kpiLabel}>Entradas</span><span className={styles.kpiValGreen}>+ R$ {caixa.totalEntradas.toFixed(2)}</span></div>
        <div className={styles.kpi}><span className={styles.kpiLabel}>Saídas</span><span className={styles.kpiValRed}>- R$ {caixa.totalSaidas.toFixed(2)}</span></div>
        <div className={styles.kpi}><span className={styles.kpiLabel}>Saldo Atual</span><span className={styles.kpiValBig}>R$ {caixa.saldoAtual.toFixed(2)}</span></div>
      </div>
      <div className={styles.form} style={{ marginTop: 16 }}>
        <Input label="Observação de fechamento" placeholder="Opcional" value={obsFechar}
          onChange={e => setObsFechar(e.target.value)} />
        {erro && <div className={styles.error}>{erro}</div>}
        {msg && <div className={styles.success}>{msg}</div>}
        <Button variant="danger" icon={<X size={16} />} onClick={fechar}>Fechar Caixa</Button>
      </div>
    </div>
  );
}

// ════════ CONTAS A PAGAR ════════
function TabContasPagar({ filialId }: { filialId: string }) {
  const [contas, setContas] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [desc, setDesc] = useState(''); const [valor, setValor] = useState('');
  const [vencimento, setVencimento] = useState(''); const [obs, setObs] = useState('');
  const [fornecedorId, setFornecedorId] = useState('');
  const [erro, setErro] = useState(''); const [msg, setMsg] = useState('');

  const carregar = useCallback(async () => {
    if (!filialId) return; setLoading(true);
    try { const { data } = await api.get(`/financeiro/pagar/${filialId}`); setContas(data.items); }
    catch (err) { console.error(err); }
    finally { setLoading(false); }
  }, [filialId]);

  useEffect(() => { carregar(); }, [carregar]);

  const criar = async (e: React.FormEvent) => {
    e.preventDefault(); setErro(''); setMsg('');
    try {
      await api.post('/financeiro/pagar', {
        filialId, descricao: desc, valor: parseFloat(valor), dataVencimento: vencimento,
        fornecedorId: fornecedorId || null, observacoes: obs || null });
      setMsg('Conta criada!'); setDesc(''); setValor(''); setVencimento(''); setObs(''); setFornecedorId('');
      setShowForm(false); carregar();
    } catch (err: any) { setErro(err.response?.data?.erro || 'Erro.'); }
  };

  const pagar = async (id: string, valor: number) => {
    try { await api.post(`/financeiro/pagar/${id}/pagar`, { valorPago: valor }); carregar(); }
    catch (err: any) { alert(err.response?.data?.erro || 'Erro ao pagar.'); }
  };

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 12 }}>
        <Button variant="primary" size="sm" icon={<Plus size={14} />} onClick={() => setShowForm(!showForm)}>
          Nova Conta a Pagar
        </Button>
      </div>
      {showForm && (
        <div className={styles.formCard} style={{ marginBottom: 16 }}>
          <form onSubmit={criar} className={styles.form}>
            <Input label="Descrição *" value={desc} onChange={e => setDesc(e.target.value)} required />
            <div className={styles.grid2}>
              <Input label="Valor *" type="number" step="0.01" value={valor} onChange={e => setValor(e.target.value)} required />
              <Input label="Vencimento *" type="date" value={vencimento} onChange={e => setVencimento(e.target.value)} required />
            </div>
            <Input label="ID Fornecedor" placeholder="Opcional" value={fornecedorId} onChange={e => setFornecedorId(e.target.value)} />
            <Input label="Observações" value={obs} onChange={e => setObs(e.target.value)} />
            {erro && <div className={styles.error}>{erro}</div>}
            {msg && <div className={styles.success}>{msg}</div>}
            <Button type="submit" variant="primary" size="sm">Salvar</Button>
          </form>
        </div>
      )}
      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead><tr><th>Descrição</th><th>Fornecedor</th><th>Valor</th><th>Vencimento</th><th>Status</th><th>Ações</th></tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={6} className={styles.loading}>Carregando...</td></tr> :
            contas.length === 0 ? <tr><td colSpan={6} className={styles.empty}>Nenhuma conta a pagar</td></tr> :
            contas.map((c: any) => {
              const st = c.vencida ? { label: 'Vencida', variant: 'critical' } : statusLabel[c.status] || statusLabel[1];
              return (
                <tr key={c.id}>
                  <td className={styles.bold}>{c.descricao}</td>
                  <td>{c.fornecedorNome || '—'}</td>
                  <td className={styles.total}>R$ {c.valorOriginal.toFixed(2)}</td>
                  <td className={styles.mono}>{new Date(c.dataVencimento).toLocaleDateString('pt-BR')}</td>
                  <td><Badge label={st.label} variant={st.variant as any} /></td>
                  <td>
                    {c.status === 1 && (
                      <Button variant="secondary" size="sm" icon={<Check size={14} />}
                        onClick={() => pagar(c.id, c.valorOriginal)}>Pagar</Button>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </>
  );
}

// ════════ CONTAS A RECEBER ════════
function TabContasReceber({ filialId }: { filialId: string }) {
  const [contas, setContas] = useState<any[]>([]);
  const [loading, setLoading] = useState(true);
  const [showForm, setShowForm] = useState(false);
  const [desc, setDesc] = useState(''); const [valor, setValor] = useState('');
  const [vencimento, setVencimento] = useState(''); const [obs, setObs] = useState('');
  const [erro, setErro] = useState(''); const [msg, setMsg] = useState('');

  const carregar = useCallback(async () => {
    if (!filialId) return; setLoading(true);
    try { const { data } = await api.get(`/financeiro/receber/${filialId}`); setContas(data.items); }
    catch (err) { console.error(err); } finally { setLoading(false); }
  }, [filialId]);

  useEffect(() => { carregar(); }, [carregar]);

  const criar = async (e: React.FormEvent) => {
    e.preventDefault(); setErro(''); setMsg('');
    try {
      await api.post('/financeiro/receber', {
        filialId, descricao: desc, valor: parseFloat(valor), dataVencimento: vencimento,
        observacoes: obs || null });
      setMsg('Conta criada!'); setDesc(''); setValor(''); setVencimento(''); setObs('');
      setShowForm(false); carregar();
    } catch (err: any) { setErro(err.response?.data?.erro || 'Erro.'); }
  };

  const receber = async (id: string) => {
    try { await api.post(`/financeiro/receber/${id}/receber`); carregar(); }
    catch (err: any) { alert(err.response?.data?.erro || 'Erro.'); }
  };

  return (
    <>
      <div style={{ display: 'flex', justifyContent: 'flex-end', marginBottom: 12 }}>
        <Button variant="primary" size="sm" icon={<Plus size={14} />} onClick={() => setShowForm(!showForm)}>
          Nova Conta a Receber
        </Button>
      </div>
      {showForm && (
        <div className={styles.formCard} style={{ marginBottom: 16 }}>
          <form onSubmit={criar} className={styles.form}>
            <Input label="Descrição *" value={desc} onChange={e => setDesc(e.target.value)} required />
            <div className={styles.grid2}>
              <Input label="Valor *" type="number" step="0.01" value={valor} onChange={e => setValor(e.target.value)} required />
              <Input label="Vencimento *" type="date" value={vencimento} onChange={e => setVencimento(e.target.value)} required />
            </div>
            <Input label="Observações" value={obs} onChange={e => setObs(e.target.value)} />
            {erro && <div className={styles.error}>{erro}</div>}
            {msg && <div className={styles.success}>{msg}</div>}
            <Button type="submit" variant="primary" size="sm">Salvar</Button>
          </form>
        </div>
      )}
      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead><tr><th>Descrição</th><th>Cliente</th><th>Valor</th><th>Vencimento</th><th>Status</th><th>Ações</th></tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={6} className={styles.loading}>Carregando...</td></tr> :
            contas.length === 0 ? <tr><td colSpan={6} className={styles.empty}>Nenhuma conta a receber</td></tr> :
            contas.map((c: any) => {
              const st = statusLabel[c.status] || statusLabel[1];
              return (
                <tr key={c.id}>
                  <td className={styles.bold}>{c.descricao}</td>
                  <td>{c.clienteNome || '—'}</td>
                  <td className={styles.total}>R$ {c.valorOriginal.toFixed(2)}</td>
                  <td className={styles.mono}>{new Date(c.dataVencimento).toLocaleDateString('pt-BR')}</td>
                  <td><Badge label={st.label} variant={st.variant as any} /></td>
                  <td>
                    {c.status === 1 && (
                      <Button variant="secondary" size="sm" icon={<Check size={14} />}
                        onClick={() => receber(c.id)}>Receber</Button>
                    )}
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </>
  );
}

// ════════ FLUXO DE CAIXA ════════
function TabFluxo({ filialId }: { filialId: string }) {
  const [fluxo, setFluxo] = useState<any>(null);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!filialId) return; setLoading(true);
    api.get(`/financeiro/fluxo/${filialId}`)
      .then(({ data }) => setFluxo(data))
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [filialId]);

  if (loading) return <p className={styles.loading}>Carregando...</p>;
  if (!fluxo) return <p className={styles.empty}>Sem dados</p>;

  return (
    <>
      <div className={styles.kpiRow}>
        <div className={styles.kpiCard}>
          <span className={styles.kpiLabel}>A Receber</span>
          <span className={styles.kpiValGreen}>R$ {fluxo.totalReceber.toFixed(2)}</span>
          <span className={styles.kpiSub}>{fluxo.contasReceberAbertas} contas abertas</span>
        </div>
        <div className={styles.kpiCard}>
          <span className={styles.kpiLabel}>A Pagar</span>
          <span className={styles.kpiValRed}>R$ {fluxo.totalPagar.toFixed(2)}</span>
          <span className={styles.kpiSub}>{fluxo.contasPagarAbertas} contas abertas</span>
        </div>
        <div className={styles.kpiCard}>
          <span className={styles.kpiLabel}>Saldo Projetado</span>
          <span className={fluxo.saldo >= 0 ? styles.kpiValGreen : styles.kpiValRed}>
            R$ {fluxo.saldo.toFixed(2)}
          </span>
          {fluxo.contasVencidas > 0 && <span className={styles.kpiAlert}>{fluxo.contasVencidas} vencida(s)!</span>}
        </div>
      </div>

      <div className={styles.tableWrapper} style={{ marginTop: 16 }}>
        <table className={styles.table}>
          <thead><tr><th>Tipo</th><th>Descrição</th><th>Valor</th><th>Data</th><th>Status</th></tr></thead>
          <tbody>
            {fluxo.itens.length === 0 ? (
              <tr><td colSpan={5} className={styles.empty}>Nenhuma movimentação</td></tr>
            ) : fluxo.itens.map((item: any, i: number) => {
              const st = statusLabel[item.status] || statusLabel[1];
              return (
                <tr key={i}>
                  <td><Badge label={item.tipo} variant={item.tipo === 'Receber' ? 'success' : 'critical'} /></td>
                  <td className={styles.bold}>{item.descricao}</td>
                  <td className={item.tipo === 'Receber' ? styles.valGreen : styles.valRed}>
                    R$ {item.valor.toFixed(2)}
                  </td>
                  <td className={styles.mono}>{new Date(item.data).toLocaleDateString('pt-BR')}</td>
                  <td><Badge label={st.label} variant={st.variant as any} /></td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </>
  );
}
