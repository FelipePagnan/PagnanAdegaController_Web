import { useState, useRef, useEffect } from 'react';
import { ShoppingCart, Search, Trash2, CreditCard, Banknote, QrCode, Plus, X, Check } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import { FormaPagamento } from '@/types';
import api from '@/services/api';
import styles from './PDVPage.module.css';

interface ItemPDV {
  produtoId: string;
  nome: string;
  quantidade: number;
  precoUnitario: number;
  total: number;
  embalagemId?: string;
  embalagemNome?: string;
  unidadesPorEmbalagem?: number;
}

interface PagamentoPDV {
  forma: FormaPagamento;
  valor: string;
}

const formaLabel: Record<number, { label: string; icon: any }> = {
  [FormaPagamento.Dinheiro]: { label: 'Dinheiro', icon: Banknote },
  [FormaPagamento.PIX]: { label: 'PIX', icon: QrCode },
  [FormaPagamento.CartaoCredito]: { label: 'Crédito', icon: CreditCard },
  [FormaPagamento.CartaoDebito]: { label: 'Débito', icon: CreditCard },
};

export function PDVPage() {
  const [busca, setBusca] = useState('');
  const [itens, setItens] = useState<ItemPDV[]>([]);
  const [pagamentos, setPagamentos] = useState<PagamentoPDV[]>([]);
  const [desconto, setDesconto] = useState('');
  const [erro, setErro] = useState('');
  const [sucesso, setSucesso] = useState('');
  const [salvando, setSalvando] = useState(false);
  const [userFilialId, setUserFilialId] = useState('');
  const buscaRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    const token = localStorage.getItem('erp_token');
    if (token) {
      try {
        const payload = JSON.parse(atob(token.split('.')[1]));
        const filiais = Array.isArray(payload.filial_id) ? payload.filial_id : [payload.filial_id];
        if (filiais[0]) setUserFilialId(filiais[0]);
      } catch {}
    }
    buscaRef.current?.focus();
  }, []);

  const subtotal = itens.reduce((s, i) => s + i.total, 0);
  const descontoVal = parseFloat(desconto) || 0;
  const total = Math.max(0, subtotal - descontoVal);
  const totalPago = pagamentos.reduce((s, p) => s + (parseFloat(p.valor) || 0), 0);
  const troco = Math.max(0, totalPago - total);
  const totalItens = itens.reduce((s, i) => s + i.quantidade, 0);

  const buscarProduto = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!busca.trim()) return;
    setErro('');

    try {
      // Tentar por código de barras primeiro
      let produto: any;
      try {
        const { data } = await api.get(`/produtos/barcode/${busca.trim()}`);
        produto = data;
      } catch {
        // Se não achou por barcode, tentar por nome (pegar o primeiro)
        const { data } = await api.get('/produtos', { params: { termo: busca.trim(), tamanhoPagina: 1 } });
        if (data.items?.length > 0) {
          const { data: det } = await api.get(`/produtos/${data.items[0].id}`);
          produto = det;
        }
      }

      if (!produto) {
        setErro('Produto não encontrado.');
        return;
      }

      // Verificar se já existe na lista
      const existente = itens.findIndex(i => i.produtoId === produto.id && !i.embalagemId);
      if (existente >= 0) {
        const novos = [...itens];
        novos[existente].quantidade += 1;
        novos[existente].total = novos[existente].quantidade * novos[existente].precoUnitario;
        setItens(novos);
      } else {
        setItens([...itens, {
          produtoId: produto.id,
          nome: produto.nome,
          quantidade: 1,
          precoUnitario: produto.precoVenda,
          total: produto.precoVenda,
        }]);
      }

      setBusca('');
      buscaRef.current?.focus();
    } catch {
      setErro('Erro ao buscar produto.');
    }
  };

  const alterarQuantidade = (idx: number, qtd: number) => {
    if (qtd <= 0) { removerItem(idx); return; }
    const novos = [...itens];
    novos[idx].quantidade = qtd;
    novos[idx].total = qtd * novos[idx].precoUnitario;
    setItens(novos);
  };

  const removerItem = (idx: number) => {
    setItens(itens.filter((_, i) => i !== idx));
  };

  const adicionarPagamento = (forma: FormaPagamento) => {
    const restante = total - totalPago;
    setPagamentos([...pagamentos, { forma, valor: restante > 0 ? restante.toFixed(2) : '' }]);
  };

  const removerPagamento = (idx: number) => {
    setPagamentos(pagamentos.filter((_, i) => i !== idx));
  };

  const finalizarVenda = async () => {
    setErro(''); setSucesso(''); setSalvando(true);

    try {
      const { data } = await api.post('/vendas', {
        filialId: userFilialId,
        itens: itens.map(i => ({
          produtoId: i.produtoId,
          quantidade: i.quantidade,
          precoUnitario: i.precoUnitario,
          embalagemId: i.embalagemId || null,
        })),
        pagamentos: pagamentos.map(p => ({
          forma: p.forma,
          valor: parseFloat(p.valor) || 0,
        })),
        desconto: descontoVal,
      });

      setSucesso(`Venda #${data.numero} finalizada! Total: R$ ${data.total.toFixed(2)}${troco > 0 ? ` — Troco: R$ ${troco.toFixed(2)}` : ''}`);
      setItens([]);
      setPagamentos([]);
      setDesconto('');
      buscaRef.current?.focus();
    } catch (err: any) {
      setErro(err.response?.data?.erro || 'Erro ao finalizar venda.');
    } finally {
      setSalvando(false);
    }
  };

  const novaVenda = () => {
    setItens([]); setPagamentos([]); setDesconto('');
    setErro(''); setSucesso('');
    buscaRef.current?.focus();
  };

  return (
    <div className={styles.page}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.headerLeft}>
          <ShoppingCart size={20} />
          <span className={styles.headerTitle}>PDV — Ponto de Venda</span>
        </div>
        <Button variant="outline" size="sm" onClick={novaVenda}>Nova Venda</Button>
      </div>

      <div className={styles.pdvLayout}>
        {/* Coluna esquerda — itens */}
        <div className={styles.colItens}>
          {/* Busca */}
          <form onSubmit={buscarProduto} className={styles.buscaForm}>
            <div className={styles.buscaInput}>
              <Search size={18} />
              <input ref={buscaRef} type="text" placeholder="Código de barras ou nome do produto..."
                value={busca} onChange={e => setBusca(e.target.value)} className={styles.buscaField} />
            </div>
            <Button type="submit" variant="secondary" size="md">Adicionar</Button>
          </form>

          {erro && <div className={styles.error}>{erro}</div>}
          {sucesso && <div className={styles.success}>{sucesso}</div>}

          {/* Lista de itens */}
          <div className={styles.itensList}>
            {itens.length === 0 ? (
              <div className={styles.empty}>
                <Search size={40} strokeWidth={1} />
                <span>Escaneie ou busque um produto para começar</span>
              </div>
            ) : itens.map((item, i) => (
              <div key={i} className={styles.itemRow}>
                <div className={styles.itemInfo}>
                  <span className={styles.itemNome}>{item.nome}</span>
                  <span className={styles.itemPreco}>R$ {item.precoUnitario.toFixed(2)} /un</span>
                </div>
                <div className={styles.itemQtd}>
                  <button className={styles.qtdBtn} onClick={() => alterarQuantidade(i, item.quantidade - 1)}>−</button>
                  <span className={styles.qtdValor}>{item.quantidade}</span>
                  <button className={styles.qtdBtn} onClick={() => alterarQuantidade(i, item.quantidade + 1)}>+</button>
                </div>
                <span className={styles.itemTotal}>R$ {item.total.toFixed(2)}</span>
                <button className={styles.removeBtn} onClick={() => removerItem(i)}>
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
          </div>
        </div>

        {/* Coluna direita — totais e pagamento */}
        <div className={styles.colPagamento}>
          {/* Resumo */}
          <div className={styles.resumo}>
            <div className={styles.resumoRow}>
              <span>Subtotal</span>
              <span>R$ {subtotal.toFixed(2)}</span>
            </div>
            {descontoVal > 0 && (
              <div className={styles.resumoRow}>
                <span>Desconto</span>
                <span className={styles.descontoValor}>- R$ {descontoVal.toFixed(2)}</span>
              </div>
            )}
            <div className={styles.totalRow}>
              <span>Total</span>
              <span className={styles.totalValor}>R$ {total.toFixed(2)}</span>
            </div>
            <div className={styles.resumoRow}>
              <span className={styles.resumoLabel}>{totalItens} {totalItens === 1 ? 'item' : 'itens'}</span>
            </div>
          </div>

          {/* Desconto */}
          <div className={styles.descontoBox}>
            <Input label="Desconto (R$)" placeholder="0.00" type="number" step="0.01"
              value={desconto} onChange={e => setDesconto(e.target.value)} />
          </div>

          {/* Formas de pagamento */}
          <div className={styles.formasBox}>
            <span className={styles.formasTitle}>Pagamento</span>
            <div className={styles.formasBtns}>
              {Object.entries(formaLabel).map(([key, { label, icon: Icon }]) => (
                <button key={key} className={styles.formaBtn}
                  onClick={() => adicionarPagamento(Number(key))}>
                  <Icon size={16} /> {label}
                </button>
              ))}
            </div>

            {pagamentos.map((p, i) => {
              const info = formaLabel[p.forma];
              return (
                <div key={i} className={styles.pgtoRow}>
                  <Badge label={info?.label || ''} variant="default" dot={false} />
                  <input type="number" step="0.01" placeholder="0.00" className={styles.pgtoInput}
                    value={p.valor}
                    onChange={e => { const n = [...pagamentos]; n[i].valor = e.target.value; setPagamentos(n); }} />
                  <button className={styles.pgtoRemove} onClick={() => removerPagamento(i)}>
                    <X size={14} />
                  </button>
                </div>
              );
            })}

            {pagamentos.length > 0 && (
              <div className={styles.pgtoResumo}>
                <div className={styles.resumoRow}>
                  <span>Pago</span>
                  <span className={styles.pgtoTotal}>R$ {totalPago.toFixed(2)}</span>
                </div>
                {troco > 0 && (
                  <div className={styles.resumoRow}>
                    <span>Troco</span>
                    <span className={styles.trocoValor}>R$ {troco.toFixed(2)}</span>
                  </div>
                )}
              </div>
            )}
          </div>

          {/* Finalizar */}
          <Button variant="primary" size="lg" fullWidth icon={<Check size={18} />}
            loading={salvando} disabled={itens.length === 0 || totalPago < total}
            onClick={finalizarVenda}>
            Finalizar Venda
          </Button>
        </div>
      </div>
    </div>
  );
}
