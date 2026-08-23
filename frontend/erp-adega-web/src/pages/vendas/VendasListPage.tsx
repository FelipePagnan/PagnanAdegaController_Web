import { useState, useEffect, useCallback } from 'react';
import { ShoppingCart, Eye, XCircle, Search, Receipt } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import type { VendaResumo, Venda, PagedResult } from '@/types';
import { StatusVenda, FormaPagamento } from '@/types';
import api from '@/services/api';
import styles from './VendasListPage.module.css';

const statusLabel: Record<number, { label: string; variant: string }> = {
  [StatusVenda.Aberta]: { label: 'Aberta', variant: 'info' },
  [StatusVenda.Finalizada]: { label: 'Finalizada', variant: 'success' },
  [StatusVenda.Cancelada]: { label: 'Cancelada', variant: 'critical' },
};

const formaLabel: Record<number, string> = {
  [FormaPagamento.Dinheiro]: 'Dinheiro',
  [FormaPagamento.PIX]: 'PIX',
  [FormaPagamento.CartaoCredito]: 'Crédito',
  [FormaPagamento.CartaoDebito]: 'Débito',
};

export function VendasListPage() {
  const [vendas, setVendas] = useState<VendaResumo[]>([]);
  const [total, setTotal] = useState(0);
  const [pagina, setPagina] = useState(1);
  const [loading, setLoading] = useState(true);
  const [filialId, setFilialId] = useState('');

  // Modal detalhe
  const [vendaDetalhe, setVendaDetalhe] = useState<Venda | null>(null);
  const [modalAberto, setModalAberto] = useState(false);

  // Cancelamento
  const [cancelando, setCancelando] = useState<string | null>(null);
  const [motivoCancelamento, setMotivoCancelamento] = useState('');
  const [erroCancelamento, setErroCancelamento] = useState('');

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

  const carregar = useCallback(async () => {
    if (!filialId) return;
    setLoading(true);
    try {
      const { data } = await api.get<PagedResult<VendaResumo>>(`/vendas/${filialId}`, {
        params: { pagina: String(pagina), tamanhoPagina: '15' }
      });
      setVendas(data.items);
      setTotal(data.total);
    } catch (err) { console.error(err); }
    finally { setLoading(false); }
  }, [filialId, pagina]);

  useEffect(() => { carregar(); }, [carregar]);

  const verDetalhe = async (id: string) => {
    try {
      const { data } = await api.get<Venda>(`/vendas/detalhe/${id}`);
      setVendaDetalhe(data);
      setModalAberto(true);
    } catch (err) { console.error(err); }
  };

  const iniciarCancelamento = (id: string) => {
    setCancelando(id);
    setMotivoCancelamento('');
    setErroCancelamento('');
  };

  const confirmarCancelamento = async () => {
    if (!cancelando || !motivoCancelamento.trim()) {
      setErroCancelamento('Motivo é obrigatório para cancelamento.');
      return;
    }
    try {
      await api.post(`/vendas/${cancelando}/cancelar`, { motivo: motivoCancelamento });
      setCancelando(null);
      setMotivoCancelamento('');
      carregar();
    } catch (err: any) {
      setErroCancelamento(err.response?.data?.erro || 'Erro ao cancelar.');
    }
  };

  const totalPaginas = Math.ceil(total / 15);

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>Vendas Realizadas</h1>
          <p className={styles.subtitle}>{total} vendas registradas</p>
        </div>
        <Button variant="primary" icon={<ShoppingCart size={16} />}
          onClick={() => window.location.href = '/vendas'}>
          Ir para PDV
        </Button>
      </div>

      {/* Tabela */}
      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr>
              <th>Venda</th><th>Data</th><th>Itens</th><th>Total</th><th>Pagamento</th><th>Operador</th><th>Status</th><th>Ações</th>
            </tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={8} className={styles.loading}>Carregando...</td></tr>
            ) : vendas.length === 0 ? (
              <tr><td colSpan={8} className={styles.empty}>
                <Receipt size={32} strokeWidth={1.5} />
                <span>Nenhuma venda registrada</span>
              </td></tr>
            ) : vendas.map(v => {
              const st = statusLabel[v.status] || statusLabel[1];
              return (
                <tr key={v.id}>
                  <td className={styles.vendaNum}>#{v.numero}</td>
                  <td className={styles.mono}>{new Date(v.criadoEm).toLocaleString('pt-BR')}</td>
                  <td>{v.totalItens}</td>
                  <td className={styles.totalVal}>R$ {v.total.toFixed(2)}</td>
                  <td><Badge label={v.formaPagamentoPrincipal} variant="default" dot={false} /></td>
                  <td>{v.usuarioNome}</td>
                  <td><Badge label={st.label} variant={st.variant as any} /></td>
                  <td>
                    <div className={styles.actions}>
                      <button className={styles.actionBtn} title="Ver detalhe" onClick={() => verDetalhe(v.id)}>
                        <Eye size={15} />
                      </button>
                      {v.status === StatusVenda.Finalizada && (
                        <button className={styles.actionBtn} title="Cancelar" onClick={() => iniciarCancelamento(v.id)}>
                          <XCircle size={15} />
                        </button>
                      )}
                    </div>
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

      {/* Modal de cancelamento */}
      {cancelando && (
        <div className={styles.overlay} onClick={() => setCancelando(null)}>
          <div className={styles.modal} onClick={e => e.stopPropagation()}>
            <h3 className={styles.modalTitle}>Cancelar Venda</h3>
            <p className={styles.modalDesc}>O estoque será estornado automaticamente. Esta ação não pode ser desfeita.</p>
            <Input label="Motivo do cancelamento *" placeholder="Descreva o motivo..."
              value={motivoCancelamento} onChange={e => setMotivoCancelamento(e.target.value)} />
            {erroCancelamento && <div className={styles.error}>{erroCancelamento}</div>}
            <div className={styles.modalActions}>
              <Button variant="outline" onClick={() => setCancelando(null)}>Voltar</Button>
              <Button variant="danger" icon={<XCircle size={16} />} onClick={confirmarCancelamento}>
                Confirmar Cancelamento
              </Button>
            </div>
          </div>
        </div>
      )}

      {/* Modal de detalhe */}
      {modalAberto && vendaDetalhe && (
        <div className={styles.overlay} onClick={() => setModalAberto(false)}>
          <div className={styles.modalLarge} onClick={e => e.stopPropagation()}>
            <div className={styles.modalHeader}>
              <h3 className={styles.modalTitle}>Venda #{vendaDetalhe.numero}</h3>
              <Badge label={statusLabel[vendaDetalhe.status]?.label || ''}
                variant={statusLabel[vendaDetalhe.status]?.variant as any} />
            </div>
            <div className={styles.modalMeta}>
              <span>Data: {new Date(vendaDetalhe.criadoEm).toLocaleString('pt-BR')}</span>
              <span>Operador: {vendaDetalhe.usuarioNome}</span>
              {vendaDetalhe.clienteNome && <span>Cliente: {vendaDetalhe.clienteNome}</span>}
            </div>

            {vendaDetalhe.motivoCancelamento && (
              <div className={styles.error}>Motivo do cancelamento: {vendaDetalhe.motivoCancelamento}</div>
            )}

            <h4 className={styles.sectionTitle}>Itens</h4>
            <table className={styles.tableSmall}>
              <thead>
                <tr><th>Produto</th><th>Qtd</th><th>Preço Un.</th><th>Total</th></tr>
              </thead>
              <tbody>
                {vendaDetalhe.itens.map(i => (
                  <tr key={i.id}>
                    <td className={styles.prodNome}>
                      {i.produtoNome}
                      {i.embalagemNome && <span className={styles.embInfo}> ({i.embalagemNome} ×{i.unidadesPorEmbalagem})</span>}
                    </td>
                    <td>{i.quantidade}</td>
                    <td className={styles.mono}>R$ {i.precoUnitario.toFixed(2)}</td>
                    <td className={styles.totalVal}>R$ {i.total.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            <h4 className={styles.sectionTitle}>Pagamentos</h4>
            <table className={styles.tableSmall}>
              <thead>
                <tr><th>Forma</th><th>Valor</th><th>Taxa</th><th>Líquido</th></tr>
              </thead>
              <tbody>
                {vendaDetalhe.pagamentos.map(p => (
                  <tr key={p.id}>
                    <td><Badge label={formaLabel[p.forma] || String(p.forma)} variant="default" dot={false} /></td>
                    <td className={styles.mono}>R$ {p.valor.toFixed(2)}</td>
                    <td className={styles.mono}>{p.taxaPercentual > 0 ? `${p.taxaPercentual}% (R$ ${p.taxaValor.toFixed(2)})` : '—'}</td>
                    <td className={styles.totalVal}>R$ {p.valorLiquido.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>

            <div className={styles.totalBox}>
              <div className={styles.totalRow}><span>Subtotal</span><span>R$ {vendaDetalhe.subTotal.toFixed(2)}</span></div>
              {vendaDetalhe.desconto > 0 && (
                <div className={styles.totalRow}><span>Desconto</span><span className={styles.desconto}>- R$ {vendaDetalhe.desconto.toFixed(2)}</span></div>
              )}
              <div className={styles.totalRowFinal}><span>Total</span><span>R$ {vendaDetalhe.total.toFixed(2)}</span></div>
              {vendaDetalhe.troco > 0 && (
                <div className={styles.totalRow}><span>Troco</span><span className={styles.desconto}>R$ {vendaDetalhe.troco.toFixed(2)}</span></div>
              )}
            </div>

            <div className={styles.modalActions}>
              <Button variant="outline" onClick={() => setModalAberto(false)}>Fechar</Button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
}
