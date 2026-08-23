import { useState, useEffect, useCallback } from 'react';
import { Calendar, Plus, Eye, Check, X, Package } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import api from '@/services/api';
import styles from './ReservasPage.module.css';

const statusLabel: Record<number, { label: string; variant: string }> = {
  1: { label: 'Ativa', variant: 'reserved' }, 2: { label: 'Expirada', variant: 'critical' },
  3: { label: 'Retirada', variant: 'success' }, 4: { label: 'Cancelada', variant: 'inactive' },
};

function useFilialId() {
  const [id, setId] = useState('');
  useEffect(() => { const t = localStorage.getItem('erp_token'); if (t) { try { const p = JSON.parse(atob(t.split('.')[1])); const f = Array.isArray(p.filial_id) ? p.filial_id : [p.filial_id]; if (f[0]) setId(f[0]); } catch {} } }, []);
  return id;
}

export function ReservasPage() {
  const [tab, setTab] = useState<'lista' | 'nova' | 'detalhe'>('lista');
  const filialId = useFilialId();
  const [reservas, setReservas] = useState<any[]>([]); const [total, setTotal] = useState(0);
  const [pagina, setPagina] = useState(1); const [loading, setLoading] = useState(true);
  const [detalhe, setDetalhe] = useState<any>(null);
  // Form
  const [clienteId, setClienteId] = useState(''); const [adiantamento, setAdiantamento] = useState('');
  const [dataLimite, setDataLimite] = useState(''); const [obs, setObs] = useState('');
  const [itens, setItens] = useState<{produtoId:string;produtoNome:string;quantidade:string;precoUnitario:string}[]>([]);
  const [salvando, setSalvando] = useState(false); const [erro, setErro] = useState(''); const [msg, setMsg] = useState('');

  const carregar = useCallback(async () => {
    if (!filialId) return; setLoading(true);
    try { const { data } = await api.get(`/reservas/${filialId}`, { params: { pagina, tamanhoPagina: 15 } }); setReservas(data.items); setTotal(data.total); }
    catch {} finally { setLoading(false); }
  }, [filialId, pagina]);

  useEffect(() => { if (tab === 'lista') carregar(); }, [carregar, tab]);

  const verDetalhe = async (id: string) => {
    try { const { data } = await api.get(`/reservas/detalhe/${id}`); setDetalhe(data); setTab('detalhe'); } catch {}
  };

  const criar = async (e: React.FormEvent) => {
    e.preventDefault(); setErro(''); setMsg(''); setSalvando(true);
    try {
      await api.post('/reservas', {
        clienteId, filialId, valorAdiantamento: parseFloat(adiantamento) || 0,
        dataLimite, observacoes: obs || null,
        itens: itens.filter(i => i.produtoId && i.quantidade).map(i => ({
          produtoId: i.produtoId, produtoNome: i.produtoNome || 'Produto',
          quantidade: parseInt(i.quantidade), precoUnitario: parseFloat(i.precoUnitario) }))
      });
      setMsg('Reserva criada!'); setClienteId(''); setAdiantamento(''); setDataLimite(''); setObs(''); setItens([]);
    } catch (err: any) { setErro(err.response?.data?.erro || 'Erro.'); } finally { setSalvando(false); }
  };

  const retirar = async () => { if (!detalhe) return; try { await api.post(`/reservas/${detalhe.id}/retirar`); verDetalhe(detalhe.id); } catch (err: any) { alert(err.response?.data?.erro || 'Erro'); } };
  const cancelar = async () => {
    if (!detalhe) return; const motivo = prompt('Motivo do cancelamento:'); if (!motivo) return;
    try { await api.post(`/reservas/${detalhe.id}/cancelar`, { motivo }); verDetalhe(detalhe.id); } catch (err: any) { alert(err.response?.data?.erro || 'Erro'); }
  };

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div><h1 className={styles.title}>Reservas</h1><p className={styles.subtitle}>Reservas com adiantamento e prazo</p></div>
        {tab === 'lista' && <Button variant="primary" icon={<Plus size={16} />} onClick={() => { setTab('nova'); setErro(''); setMsg(''); }}>Nova Reserva</Button>}
      </div>

      <div className={styles.tabs}>
        <button className={`${styles.tab} ${tab === 'lista' ? styles.tabActive : ''}`} onClick={() => setTab('lista')}><Calendar size={15} /> Reservas</button>
        {tab === 'nova' && <button className={`${styles.tab} ${styles.tabActive}`}><Plus size={15} /> Nova</button>}
        {tab === 'detalhe' && <button className={`${styles.tab} ${styles.tabActive}`}><Eye size={15} /> #{detalhe?.numero}</button>}
      </div>

      {tab === 'lista' && (
        <div className={styles.tableWrapper}>
          <table className={styles.table}>
            <thead><tr><th>Reserva</th><th>Cliente</th><th>Total</th><th>Prazo</th><th>Itens</th><th>Status</th><th>Ações</th></tr></thead>
            <tbody>
              {loading ? <tr><td colSpan={7} className={styles.empty}>Carregando...</td></tr> :
              reservas.length === 0 ? <tr><td colSpan={7} className={styles.empty}>Nenhuma reserva</td></tr> :
              reservas.map((r: any) => {
                const st = r.expirada ? { label: 'Expirada', variant: 'critical' } : statusLabel[r.status] || statusLabel[1];
                return (
                  <tr key={r.id}>
                    <td className={styles.num}>#{r.numero}</td>
                    <td className={styles.bold}>{r.clienteNome}</td>
                    <td className={styles.total}>R$ {r.valorTotal.toFixed(2)}</td>
                    <td className={styles.mono}>{new Date(r.dataLimite).toLocaleDateString('pt-BR')}</td>
                    <td>{r.totalItens}</td>
                    <td><Badge label={st.label} variant={st.variant as any} /></td>
                    <td><button className={styles.actionBtn} onClick={() => verDetalhe(r.id)}><Eye size={15} /></button></td>
                  </tr>);
              })}
            </tbody>
          </table>
        </div>
      )}

      {tab === 'nova' && (
        <div className={styles.formCard}>
          <h2 className={styles.formTitle}>Nova Reserva</h2>
          <form onSubmit={criar} className={styles.form}>
            <Input label="ID do Cliente *" value={clienteId} onChange={e => setClienteId(e.target.value)} required />
            <div className={styles.grid2}>
              <Input label="Adiantamento (R$) *" type="number" step="0.01" value={adiantamento} onChange={e => setAdiantamento(e.target.value)} required />
              <Input label="Prazo limite *" type="date" value={dataLimite} onChange={e => setDataLimite(e.target.value)} required />
            </div>
            <Input label="Observações" value={obs} onChange={e => setObs(e.target.value)} />
            <div className={styles.sectionHeader}><h3 className={styles.sectionTitle}>Itens</h3>
              <Button type="button" variant="ghost" size="sm" icon={<Plus size={14} />} onClick={() => setItens([...itens, { produtoId: '', produtoNome: '', quantidade: '', precoUnitario: '' }])}>Adicionar</Button>
            </div>
            {itens.map((item, i) => (
              <div key={i} className={styles.itemRow}>
                <Input placeholder="ID Produto" value={item.produtoId} onChange={e => { const n = [...itens]; n[i].produtoId = e.target.value; setItens(n); }} />
                <Input placeholder="Nome" value={item.produtoNome} onChange={e => { const n = [...itens]; n[i].produtoNome = e.target.value; setItens(n); }} />
                <Input placeholder="Qtd" type="number" value={item.quantidade} onChange={e => { const n = [...itens]; n[i].quantidade = e.target.value; setItens(n); }} />
                <Input placeholder="Preço" type="number" step="0.01" value={item.precoUnitario} onChange={e => { const n = [...itens]; n[i].precoUnitario = e.target.value; setItens(n); }} />
              </div>
            ))}
            {itens.length === 0 && <p className={styles.hint}>Adicione itens à reserva</p>}
            {erro && <div className={styles.error}>{erro}</div>}
            {msg && <div className={styles.success}>{msg}</div>}
            <div className={styles.footerBtns}><Button type="button" variant="outline" onClick={() => setTab('lista')}>Cancelar</Button><Button type="submit" variant="primary" loading={salvando}>Criar Reserva</Button></div>
          </form>
        </div>
      )}

      {tab === 'detalhe' && detalhe && (
        <div className={styles.formCard} style={{ maxWidth: 700 }}>
          <div className={styles.detailHeader}><h2 className={styles.formTitle}>Reserva #{detalhe.numero}</h2>
            <Badge label={detalhe.expirada ? 'Expirada' : statusLabel[detalhe.status]?.label || ''} variant={(detalhe.expirada ? 'critical' : statusLabel[detalhe.status]?.variant || 'default') as any} />
          </div>
          <div className={styles.meta}>
            <span>Cliente: <strong>{detalhe.clienteNome}</strong></span><span>Prazo: {new Date(detalhe.dataLimite).toLocaleDateString('pt-BR')}</span>
            <span>Operador: {detalhe.usuarioNome}</span>{detalhe.retiradoEm && <span>Retirado: {new Date(detalhe.retiradoEm).toLocaleDateString('pt-BR')}</span>}
          </div>
          {detalhe.motivoCancelamento && <div className={styles.error}>Cancelamento: {detalhe.motivoCancelamento}</div>}
          <table className={styles.table} style={{ marginTop: 12 }}>
            <thead><tr><th>Produto</th><th>Qtd</th><th>Preço Un.</th><th>Total</th></tr></thead>
            <tbody>{detalhe.itens.map((i: any) => (<tr key={i.id}><td className={styles.bold}>{i.produtoNome}</td><td>{i.quantidade}</td><td className={styles.mono}>R$ {i.precoUnitario.toFixed(2)}</td><td className={styles.total}>R$ {i.total.toFixed(2)}</td></tr>))}</tbody>
          </table>
          <div className={styles.totalBox}>
            <span>Total: <strong>R$ {detalhe.valorTotal.toFixed(2)}</strong></span>
            <span>Adiantamento: <strong style={{color:'#2D8A4E'}}>R$ {detalhe.valorAdiantamento.toFixed(2)}</strong></span>
            <span>Restante: <strong style={{color:'#722F37'}}>R$ {detalhe.valorRestante.toFixed(2)}</strong></span>
          </div>
          <div className={styles.footerBtns}>
            <Button variant="outline" onClick={() => setTab('lista')}>Voltar</Button>
            {detalhe.status === 1 && !detalhe.expirada && <><Button variant="danger" icon={<X size={16} />} onClick={cancelar}>Cancelar</Button><Button variant="secondary" icon={<Package size={16} />} onClick={retirar}>Registrar Retirada</Button></>}
          </div>
        </div>
      )}
    </div>
  );
}
