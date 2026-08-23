import { useState, useEffect, useCallback } from 'react';
import { Truck, Plus, Eye, Check, X, Package, Send, ArrowRight } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import api from '@/services/api';
import styles from './TransferenciasPage.module.css';

const statusLabel: Record<number, { label: string; variant: string }> = {
  1: { label: 'Solicitada', variant: 'info' }, 2: { label: 'Aprovada', variant: 'gold' },
  3: { label: 'Separada', variant: 'warning' }, 4: { label: 'Enviada', variant: 'primary' },
  5: { label: 'Recebida', variant: 'success' }, 6: { label: 'Cancelada', variant: 'inactive' },
};

function useFilialId() {
  const [id, setId] = useState('');
  useEffect(() => { const t = localStorage.getItem('erp_token'); if (t) { try { const p = JSON.parse(atob(t.split('.')[1])); const f = Array.isArray(p.filial_id) ? p.filial_id : [p.filial_id]; if (f[0]) setId(f[0]); } catch {} } }, []);
  return id;
}

export function TransferenciasPage() {
  const [tab, setTab] = useState<'lista' | 'nova' | 'detalhe'>('lista');
  const filialId = useFilialId();
  const [transferencias, setTransferencias] = useState<any[]>([]); const [total, setTotal] = useState(0);
  const [pagina, setPagina] = useState(1); const [loading, setLoading] = useState(true);
  const [detalhe, setDetalhe] = useState<any>(null);
  // Form
  const [destino, setDestino] = useState(''); const [obs, setObs] = useState('');
  const [itens, setItens] = useState<{produtoId:string;produtoNome:string;quantidade:string}[]>([]);
  const [salvando, setSalvando] = useState(false); const [erro, setErro] = useState(''); const [msg, setMsg] = useState('');

  const carregar = useCallback(async () => {
    if (!filialId) return; setLoading(true);
    try { const { data } = await api.get('/transferencias', { params: { filialId, pagina, tamanhoPagina: 15 } }); setTransferencias(data.items); setTotal(data.total); }
    catch {} finally { setLoading(false); }
  }, [filialId, pagina]);

  useEffect(() => { if (tab === 'lista') carregar(); }, [carregar, tab]);

  const verDetalhe = async (id: string) => {
    try { const { data } = await api.get(`/transferencias/${id}`); setDetalhe(data); setTab('detalhe'); } catch {}
  };

  const criar = async (e: React.FormEvent) => {
    e.preventDefault(); setErro(''); setMsg(''); setSalvando(true);
    try {
      await api.post('/transferencias', {
        filialOrigemId: filialId, filialDestinoId: destino, observacoes: obs || null,
        itens: itens.filter(i => i.produtoId && i.quantidade).map(i => ({
          produtoId: i.produtoId, produtoNome: i.produtoNome || 'Produto', quantidade: parseInt(i.quantidade) }))
      });
      setMsg('Transferência criada!'); setDestino(''); setObs(''); setItens([]);
    } catch (err: any) { setErro(err.response?.data?.erro || 'Erro.'); } finally { setSalvando(false); }
  };

  const executarAcao = async (acao: string) => {
    if (!detalhe) return;
    try {
      if (acao === 'cancelar') {
        const motivo = prompt('Motivo:'); if (!motivo) return;
        await api.post(`/transferencias/${detalhe.id}/cancelar`, { motivo });
      } else {
        await api.post(`/transferencias/${detalhe.id}/${acao}`);
      }
      verDetalhe(detalhe.id);
    } catch (err: any) { alert(err.response?.data?.erro || 'Erro'); }
  };

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div><h1 className={styles.title}>Transferências</h1><p className={styles.subtitle}>Movimentação de estoque entre filiais</p></div>
        {tab === 'lista' && <Button variant="primary" icon={<Plus size={16} />} onClick={() => { setTab('nova'); setErro(''); setMsg(''); }}>Nova Transferência</Button>}
      </div>

      <div className={styles.tabs}>
        <button className={`${styles.tab} ${tab === 'lista' ? styles.tabActive : ''}`} onClick={() => setTab('lista')}><Truck size={15} /> Lista</button>
        {tab === 'nova' && <button className={`${styles.tab} ${styles.tabActive}`}><Plus size={15} /> Nova</button>}
        {tab === 'detalhe' && <button className={`${styles.tab} ${styles.tabActive}`}><Eye size={15} /> #{detalhe?.numero}</button>}
      </div>

      {tab === 'lista' && (
        <div className={styles.tableWrapper}>
          <table className={styles.table}>
            <thead><tr><th>Nº</th><th>Origem</th><th>Destino</th><th>Itens</th><th>Solicitante</th><th>Data</th><th>Status</th><th></th></tr></thead>
            <tbody>
              {loading ? <tr><td colSpan={8} className={styles.empty}>Carregando...</td></tr> :
              transferencias.length === 0 ? <tr><td colSpan={8} className={styles.empty}>Nenhuma transferência</td></tr> :
              transferencias.map((t: any) => {
                const st = statusLabel[t.status] || statusLabel[1];
                return (<tr key={t.id}>
                  <td className={styles.num}>#{t.numero}</td>
                  <td className={styles.bold}>{t.filialOrigemNome}</td>
                  <td className={styles.bold}>{t.filialDestinoNome}</td>
                  <td>{t.totalItens}</td><td>{t.solicitanteNome}</td>
                  <td className={styles.mono}>{new Date(t.criadoEm).toLocaleDateString('pt-BR')}</td>
                  <td><Badge label={st.label} variant={st.variant as any} /></td>
                  <td><button className={styles.actionBtn} onClick={() => verDetalhe(t.id)}><Eye size={15} /></button></td>
                </tr>);
              })}
            </tbody>
          </table>
        </div>
      )}

      {tab === 'nova' && (
        <div className={styles.formCard}>
          <h2 className={styles.formTitle}>Nova Transferência</h2>
          <p className={styles.formDesc}>Origem: sua filial atual. Informe o ID da filial de destino.</p>
          <form onSubmit={criar} className={styles.form}>
            <Input label="ID Filial Destino *" value={destino} onChange={e => setDestino(e.target.value)} required />
            <Input label="Observações" value={obs} onChange={e => setObs(e.target.value)} />
            <div className={styles.sectionHeader}><h3 className={styles.sectionTitle}>Itens</h3>
              <Button type="button" variant="ghost" size="sm" icon={<Plus size={14} />} onClick={() => setItens([...itens, { produtoId: '', produtoNome: '', quantidade: '' }])}>Adicionar</Button>
            </div>
            {itens.map((item, i) => (
              <div key={i} className={styles.itemRow}>
                <Input placeholder="ID Produto" value={item.produtoId} onChange={e => { const n = [...itens]; n[i].produtoId = e.target.value; setItens(n); }} />
                <Input placeholder="Nome" value={item.produtoNome} onChange={e => { const n = [...itens]; n[i].produtoNome = e.target.value; setItens(n); }} />
                <Input placeholder="Qtd" type="number" value={item.quantidade} onChange={e => { const n = [...itens]; n[i].quantidade = e.target.value; setItens(n); }} />
              </div>
            ))}
            {itens.length === 0 && <p className={styles.hint}>Adicione produtos para transferir</p>}
            {erro && <div className={styles.error}>{erro}</div>}
            {msg && <div className={styles.success}>{msg}</div>}
            <div className={styles.footerBtns}><Button type="button" variant="outline" onClick={() => setTab('lista')}>Cancelar</Button><Button type="submit" variant="primary" loading={salvando}>Criar</Button></div>
          </form>
        </div>
      )}

      {tab === 'detalhe' && detalhe && (
        <div className={styles.formCard} style={{ maxWidth: 700 }}>
          <div className={styles.detailHeader}><h2 className={styles.formTitle}>Transferência #{detalhe.numero}</h2>
            <Badge label={statusLabel[detalhe.status]?.label || ''} variant={statusLabel[detalhe.status]?.variant as any} /></div>
          <div className={styles.meta}>
            <span>Origem: <strong>{detalhe.filialOrigemNome}</strong></span>
            <span><ArrowRight size={14} /></span>
            <span>Destino: <strong>{detalhe.filialDestinoNome}</strong></span>
            <span>Solicitante: {detalhe.solicitanteNome}</span>
          </div>
          {detalhe.motivoCancelamento && <div className={styles.error}>Cancelamento: {detalhe.motivoCancelamento}</div>}
          <table className={styles.table} style={{ marginTop: 12 }}>
            <thead><tr><th>Produto</th><th>Quantidade</th></tr></thead>
            <tbody>{detalhe.itens.map((i: any) => (<tr key={i.id}><td className={styles.bold}>{i.produtoNome}</td><td>{i.quantidade} un</td></tr>))}</tbody>
          </table>

          {/* Fluxo visual */}
          <div className={styles.flowBar}>
            {['Solicitada','Aprovada','Separada','Enviada','Recebida'].map((step, idx) => (
              <div key={step} className={`${styles.flowStep} ${detalhe.status > idx ? styles.flowDone : detalhe.status === idx + 1 ? styles.flowCurrent : ''}`}>
                <div className={styles.flowDot} /><span>{step}</span>
              </div>
            ))}
          </div>

          <div className={styles.footerBtns}>
            <Button variant="outline" onClick={() => setTab('lista')}>Voltar</Button>
            {detalhe.status === 1 && <><Button variant="danger" onClick={() => executarAcao('cancelar')}>Cancelar</Button><Button variant="secondary" icon={<Check size={16} />} onClick={() => executarAcao('aprovar')}>Aprovar</Button></>}
            {detalhe.status === 2 && <Button variant="gold" icon={<Package size={16} />} onClick={() => executarAcao('separar')}>Separar</Button>}
            {detalhe.status === 3 && <Button variant="secondary" icon={<Send size={16} />} onClick={() => executarAcao('enviar')}>Enviar</Button>}
            {detalhe.status === 4 && <Button variant="primary" icon={<Check size={16} />} onClick={() => executarAcao('receber')}>Confirmar Recebimento</Button>}
          </div>
        </div>
      )}
    </div>
  );
}
