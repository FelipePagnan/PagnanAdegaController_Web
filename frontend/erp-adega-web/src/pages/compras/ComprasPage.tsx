import { useState, useEffect, useCallback } from 'react';
import { ClipboardList, Plus, Eye, Check, X, Package, Truck } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import api from '@/services/api';
import styles from './ComprasPage.module.css';

interface PedidoResumo {
  id: string; numero: number; fornecedorNome: string; status: number;
  total: number; totalItens: number; usuarioNome: string; criadoEm: string;
}
interface PedidoDetalhe {
  id: string; numero: number; fornecedorNome: string; status: number;
  subTotal: number; frete: number; desconto: number; total: number;
  observacoes?: string; notaFiscal?: string; usuarioNome: string;
  aprovadoEm?: string; motivoRejeicao?: string; recebidoEm?: string; criadoEm: string;
  itens: ItemCompra[];
}
interface ItemCompra {
  id: string; produtoId: string; produtoNome: string; quantidade: number;
  precoUnitario: number; total: number; quantidadeRecebida: number;
  quantidadeDivergente: number; codigoLote?: string; dataValidade?: string;
  observacaoRecebimento?: string;
}

const statusLabel: Record<number, { label: string; variant: string }> = {
  1: { label: 'Rascunho', variant: 'default' },
  2: { label: 'Aguardando Aprovação', variant: 'warning' },
  3: { label: 'Aprovado', variant: 'success' },
  4: { label: 'Rejeitado', variant: 'critical' },
  5: { label: 'Recebido', variant: 'info' },
  6: { label: 'Recebido Parcial', variant: 'expiring' },
  7: { label: 'Cancelado', variant: 'inactive' },
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

export function ComprasPage() {
  const [tab, setTab] = useState<'lista' | 'novo' | 'detalhe'>('lista');
  const filialId = useFilialId();

  // Lista
  const [pedidos, setPedidos] = useState<PedidoResumo[]>([]);
  const [total, setTotal] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [filtroStatus, setFiltroStatus] = useState<string>('');
  const [loading, setLoading] = useState(true);

  // Detalhe
  const [detalhe, setDetalhe] = useState<PedidoDetalhe | null>(null);
  const [recebendo, setRecebendo] = useState(false);
  const [nfRecebimento, setNfRecebimento] = useState('');
  const [qtdsRecebidas, setQtdsRecebidas] = useState<Record<string, string>>({});

  // Novo pedido
  const [fornecedorId, setFornecedorId] = useState('');
  const [frete, setFrete] = useState('0');
  const [desconto, setDesconto] = useState('0');
  const [obs, setObs] = useState('');
  const [itensNovo, setItensNovo] = useState<{ produtoId: string; produtoNome: string; quantidade: string; precoUnitario: string; codigoLote: string; dataValidade: string }[]>([]);
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState('');
  const [sucesso, setSucesso] = useState('');

  const carregar = useCallback(async () => {
    if (!filialId) return;
    setLoading(true);
    try {
      const params: Record<string, string> = { pagina: String(pagina), tamanhoPagina: '15' };
      if (filtroStatus) params.status = filtroStatus;
      const { data } = await api.get(`/compras/${filialId}`, { params });
      setPedidos(data.items); setTotal(data.total);
    } catch (err) { console.error(err); }
    finally { setLoading(false); }
  }, [filialId, pagina, filtroStatus]);

  useEffect(() => { if (tab === 'lista') carregar(); }, [carregar, tab]);

  const verDetalhe = async (id: string) => {
    try {
      const { data } = await api.get<PedidoDetalhe>(`/compras/detalhe/${id}`);
      setDetalhe(data);
      setTab('detalhe');
      setRecebendo(false);
      const qtds: Record<string, string> = {};
      data.itens.forEach(i => { qtds[i.id] = String(i.quantidade); });
      setQtdsRecebidas(qtds);
    } catch (err) { console.error(err); }
  };

  const aprovar = async () => {
    if (!detalhe) return;
    try { await api.post(`/compras/${detalhe.id}/aprovar`); verDetalhe(detalhe.id); }
    catch (err: any) { alert(err.response?.data?.erro || 'Erro'); }
  };

  const rejeitar = async () => {
    if (!detalhe) return;
    const motivo = prompt('Motivo da rejeição:');
    if (!motivo) return;
    try { await api.post(`/compras/${detalhe.id}/rejeitar`, { motivo }); verDetalhe(detalhe.id); }
    catch (err: any) { alert(err.response?.data?.erro || 'Erro'); }
  };

  const receber = async () => {
    if (!detalhe) return;
    try {
      await api.post(`/compras/${detalhe.id}/receber`, {
        notaFiscal: nfRecebimento || null,
        itens: detalhe.itens.map(i => ({
          itemId: i.id,
          quantidadeRecebida: parseInt(qtdsRecebidas[i.id] || '0'),
        })),
      });
      verDetalhe(detalhe.id);
    } catch (err: any) { alert(err.response?.data?.erro || 'Erro'); }
  };

  const cancelar = async () => {
    if (!detalhe || !confirm('Cancelar este pedido?')) return;
    try { await api.post(`/compras/${detalhe.id}/cancelar`); verDetalhe(detalhe.id); }
    catch (err: any) { alert(err.response?.data?.erro || 'Erro'); }
  };

  const addItemNovo = () => {
    setItensNovo([...itensNovo, { produtoId: '', produtoNome: '', quantidade: '', precoUnitario: '', codigoLote: '', dataValidade: '' }]);
  };

  const criarPedido = async (e: React.FormEvent) => {
    e.preventDefault(); setErro(''); setSucesso(''); setSalvando(true);
    try {
      await api.post('/compras', {
        fornecedorId, filialId, frete: parseFloat(frete) || 0, desconto: parseFloat(desconto) || 0,
        observacoes: obs || null,
        itens: itensNovo.filter(i => i.produtoId && i.quantidade).map(i => ({
          produtoId: i.produtoId, produtoNome: i.produtoNome || 'Produto',
          quantidade: parseInt(i.quantidade), precoUnitario: parseFloat(i.precoUnitario),
          codigoLote: i.codigoLote || null, dataValidade: i.dataValidade || null,
        })),
      });
      setSucesso('Pedido de compra criado com sucesso!');
      setFornecedorId(''); setFrete('0'); setDesconto('0'); setObs(''); setItensNovo([]);
    } catch (err: any) {
      setErro(err.response?.data?.erro || 'Erro ao criar pedido.');
    } finally { setSalvando(false); }
  };

  const totalPaginas = Math.ceil(total / 15);

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>Compras</h1>
          <p className={styles.subtitle}>Pedidos de compra, aprovação e recebimento</p>
        </div>
        {tab !== 'novo' && (
          <Button variant="primary" icon={<Plus size={16} />} onClick={() => { setTab('novo'); setErro(''); setSucesso(''); }}>
            Novo Pedido
          </Button>
        )}
      </div>

      {/* TABS */}
      <div className={styles.tabs}>
        <button className={`${styles.tab} ${tab === 'lista' ? styles.tabActive : ''}`}
          onClick={() => setTab('lista')}><ClipboardList size={15} /> Pedidos</button>
        {tab === 'novo' && <button className={`${styles.tab} ${styles.tabActive}`}><Plus size={15} /> Novo Pedido</button>}
        {tab === 'detalhe' && <button className={`${styles.tab} ${styles.tabActive}`}><Eye size={15} /> Detalhe #{detalhe?.numero}</button>}
      </div>

      {/* LISTA */}
      {tab === 'lista' && (
        <>
          <div className={styles.filterRow}>
            <select className={styles.select} value={filtroStatus} onChange={e => { setFiltroStatus(e.target.value); setPagina(1); }}>
              <option value="">Todos os status</option>
              <option value="2">Aguardando Aprovação</option>
              <option value="3">Aprovados</option>
              <option value="5">Recebidos</option>
              <option value="7">Cancelados</option>
            </select>
          </div>

          <div className={styles.tableWrapper}>
            <table className={styles.table}>
              <thead><tr><th>Pedido</th><th>Fornecedor</th><th>Itens</th><th>Total</th><th>Solicitante</th><th>Data</th><th>Status</th><th>Ações</th></tr></thead>
              <tbody>
                {loading ? (
                  <tr><td colSpan={8} className={styles.loading}>Carregando...</td></tr>
                ) : pedidos.length === 0 ? (
                  <tr><td colSpan={8} className={styles.empty}>Nenhum pedido encontrado</td></tr>
                ) : pedidos.map(p => {
                  const st = statusLabel[p.status] || statusLabel[1];
                  return (
                    <tr key={p.id}>
                      <td className={styles.num}>#{p.numero}</td>
                      <td className={styles.bold}>{p.fornecedorNome}</td>
                      <td>{p.totalItens}</td>
                      <td className={styles.total}>R$ {p.total.toFixed(2)}</td>
                      <td>{p.usuarioNome}</td>
                      <td className={styles.mono}>{new Date(p.criadoEm).toLocaleDateString('pt-BR')}</td>
                      <td><Badge label={st.label} variant={st.variant as any} /></td>
                      <td>
                        <button className={styles.actionBtn} onClick={() => verDetalhe(p.id)}><Eye size={15} /></button>
                      </td>
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

      {/* NOVO PEDIDO */}
      {tab === 'novo' && (
        <div className={styles.formCard}>
          <h2 className={styles.formTitle}>Novo Pedido de Compra</h2>
          <p className={styles.formDesc}>Pedidos até R$ 5.000 são aprovados automaticamente. Acima disso, aguardam aprovação.</p>
          <form onSubmit={criarPedido} className={styles.form}>
            <Input label="ID do Fornecedor *" placeholder="Cole o ID do fornecedor" value={fornecedorId}
              onChange={e => setFornecedorId(e.target.value)} required />
            <div className={styles.grid2}>
              <Input label="Frete (R$)" type="number" step="0.01" value={frete} onChange={e => setFrete(e.target.value)} />
              <Input label="Desconto (R$)" type="number" step="0.01" value={desconto} onChange={e => setDesconto(e.target.value)} />
            </div>
            <Input label="Observações" placeholder="Opcional" value={obs} onChange={e => setObs(e.target.value)} />

            <div className={styles.sectionHeader}>
              <h3 className={styles.sectionTitle}>Itens</h3>
              <Button type="button" variant="ghost" size="sm" icon={<Plus size={14} />} onClick={addItemNovo}>Adicionar</Button>
            </div>
            {itensNovo.map((item, i) => (
              <div key={i} className={styles.itemRow}>
                <Input placeholder="ID Produto" value={item.produtoId}
                  onChange={e => { const n = [...itensNovo]; n[i].produtoId = e.target.value; setItensNovo(n); }} />
                <Input placeholder="Nome" value={item.produtoNome}
                  onChange={e => { const n = [...itensNovo]; n[i].produtoNome = e.target.value; setItensNovo(n); }} />
                <Input placeholder="Qtd" type="number" value={item.quantidade}
                  onChange={e => { const n = [...itensNovo]; n[i].quantidade = e.target.value; setItensNovo(n); }} />
                <Input placeholder="Preço Un." type="number" step="0.01" value={item.precoUnitario}
                  onChange={e => { const n = [...itensNovo]; n[i].precoUnitario = e.target.value; setItensNovo(n); }} />
              </div>
            ))}
            {itensNovo.length === 0 && <p className={styles.hint}>Clique em "Adicionar" para incluir itens</p>}

            {erro && <div className={styles.error}>{erro}</div>}
            {sucesso && <div className={styles.success}>{sucesso}</div>}
            <div className={styles.footerBtns}>
              <Button type="button" variant="outline" onClick={() => setTab('lista')}>Cancelar</Button>
              <Button type="submit" variant="primary" loading={salvando}>Criar Pedido</Button>
            </div>
          </form>
        </div>
      )}

      {/* DETALHE */}
      {tab === 'detalhe' && detalhe && (
        <div className={styles.formCard} style={{ maxWidth: 800 }}>
          <div className={styles.detailHeader}>
            <h2 className={styles.formTitle}>Pedido #{detalhe.numero}</h2>
            <Badge label={statusLabel[detalhe.status]?.label || ''} variant={statusLabel[detalhe.status]?.variant as any} />
          </div>
          <div className={styles.meta}>
            <span>Fornecedor: <strong>{detalhe.fornecedorNome}</strong></span>
            <span>Solicitante: {detalhe.usuarioNome}</span>
            <span>Data: {new Date(detalhe.criadoEm).toLocaleDateString('pt-BR')}</span>
            {detalhe.aprovadoEm && <span>Aprovado: {new Date(detalhe.aprovadoEm).toLocaleDateString('pt-BR')}</span>}
            {detalhe.recebidoEm && <span>Recebido: {new Date(detalhe.recebidoEm).toLocaleDateString('pt-BR')}</span>}
            {detalhe.notaFiscal && <span>NF: {detalhe.notaFiscal}</span>}
          </div>
          {detalhe.motivoRejeicao && <div className={styles.error}>Motivo da rejeição: {detalhe.motivoRejeicao}</div>}
          {detalhe.observacoes && <p className={styles.obs}>{detalhe.observacoes}</p>}

          <table className={styles.table} style={{ marginTop: 16 }}>
            <thead><tr>
              <th>Produto</th><th>Qtd Pedida</th><th>Preço Un.</th><th>Total</th>
              {(detalhe.status === 5 || detalhe.status === 6) && <><th>Recebido</th><th>Diverg.</th></>}
              {recebendo && <th>Qtd Recebida</th>}
            </tr></thead>
            <tbody>
              {detalhe.itens.map(i => (
                <tr key={i.id}>
                  <td className={styles.bold}>{i.produtoNome}</td>
                  <td>{i.quantidade}</td>
                  <td className={styles.mono}>R$ {i.precoUnitario.toFixed(2)}</td>
                  <td className={styles.total}>R$ {i.total.toFixed(2)}</td>
                  {(detalhe.status === 5 || detalhe.status === 6) && (
                    <>
                      <td className={styles.num}>{i.quantidadeRecebida}</td>
                      <td className={i.quantidadeDivergente !== 0 ? styles.negativo : ''}>
                        {i.quantidadeDivergente !== 0 ? i.quantidadeDivergente : '—'}
                      </td>
                    </>
                  )}
                  {recebendo && (
                    <td>
                      <input type="number" className={styles.inputSmall}
                        value={qtdsRecebidas[i.id] || ''}
                        onChange={e => setQtdsRecebidas({ ...qtdsRecebidas, [i.id]: e.target.value })} />
                    </td>
                  )}
                </tr>
              ))}
            </tbody>
          </table>

          <div className={styles.totalBox}>
            <span>Subtotal: R$ {detalhe.subTotal.toFixed(2)}</span>
            {detalhe.frete > 0 && <span>Frete: R$ {detalhe.frete.toFixed(2)}</span>}
            {detalhe.desconto > 0 && <span>Desconto: - R$ {detalhe.desconto.toFixed(2)}</span>}
            <strong>Total: R$ {detalhe.total.toFixed(2)}</strong>
          </div>

          {recebendo && (
            <div style={{ marginTop: 12 }}>
              <Input label="Nota Fiscal" placeholder="Número da NF" value={nfRecebimento}
                onChange={e => setNfRecebimento(e.target.value)} />
            </div>
          )}

          <div className={styles.footerBtns}>
            <Button variant="outline" onClick={() => setTab('lista')}>Voltar</Button>

            {detalhe.status === 2 && (
              <>
                <Button variant="danger" icon={<X size={16} />} onClick={rejeitar}>Rejeitar</Button>
                <Button variant="secondary" icon={<Check size={16} />} onClick={aprovar}>Aprovar</Button>
              </>
            )}

            {detalhe.status === 3 && !recebendo && (
              <Button variant="secondary" icon={<Package size={16} />} onClick={() => setRecebendo(true)}>
                Registrar Recebimento
              </Button>
            )}

            {recebendo && (
              <Button variant="secondary" icon={<Truck size={16} />} onClick={receber}>
                Confirmar Recebimento
              </Button>
            )}

            {(detalhe.status === 1 || detalhe.status === 2 || detalhe.status === 3) && (
              <Button variant="ghost" onClick={cancelar}>Cancelar Pedido</Button>
            )}
          </div>
        </div>
      )}
    </div>
  );
}
