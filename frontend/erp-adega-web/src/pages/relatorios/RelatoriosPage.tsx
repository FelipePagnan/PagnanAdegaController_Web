import { useState, useEffect } from 'react';
import { BarChart3, Download, FileSpreadsheet, Package, ShoppingCart, DollarSign } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import api from '@/services/api';
import styles from './RelatoriosPage.module.css';

function useFilialId() {
  const [id, setId] = useState('');
  useEffect(() => { const t = localStorage.getItem('erp_token'); if (t) { try { const p = JSON.parse(atob(t.split('.')[1])); const f = Array.isArray(p.filial_id) ? p.filial_id : [p.filial_id]; if (f[0]) setId(f[0]); } catch {} } }, []);
  return id;
}

export function RelatoriosPage() {
  const [tab, setTab] = useState<'estoque' | 'vendas' | 'financeiro'>('estoque');
  const filialId = useFilialId();

  const tabs = [
    { id: 'estoque' as const, label: 'Estoque', icon: Package },
    { id: 'vendas' as const, label: 'Vendas', icon: ShoppingCart },
    { id: 'financeiro' as const, label: 'Financeiro', icon: DollarSign },
  ];

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <h1 className={styles.title}>Relatórios</h1>
        <p className={styles.subtitle}>Relatórios exportáveis com dados em tempo real</p>
      </div>
      <div className={styles.tabs}>
        {tabs.map(t => { const Icon = t.icon; return (
          <button key={t.id} className={`${styles.tab} ${tab === t.id ? styles.tabActive : ''}`}
            onClick={() => setTab(t.id)}><Icon size={15} /> {t.label}</button>
        ); })}
      </div>
      {tab === 'estoque' && <RelEstoque filialId={filialId} />}
      {tab === 'vendas' && <RelVendas filialId={filialId} />}
      {tab === 'financeiro' && <RelFinanceiro filialId={filialId} />}
    </div>
  );
}

function RelEstoque({ filialId }: { filialId: string }) {
  const [dados, setDados] = useState<any>(null); const [loading, setLoading] = useState(false);

  const carregar = async () => {
    if (!filialId) return; setLoading(true);
    try { const { data } = await api.get(`/relatorios/estoque/${filialId}`); setDados(data); }
    catch (err) { console.error(err); } finally { setLoading(false); }
  };

  const exportarCsv = () => { window.open(`/api/relatorios/estoque/${filialId}/csv`, '_blank'); };

  return (
    <div>
      <div className={styles.actionRow}>
        <Button variant="primary" icon={<BarChart3 size={16} />} onClick={carregar} loading={loading}>Gerar Relatório</Button>
        {dados && <Button variant="outline" icon={<FileSpreadsheet size={16} />} onClick={exportarCsv}>Exportar CSV</Button>}
      </div>
      {dados && (
        <>
          <div className={styles.kpiRow}>
            <div className={styles.kpi}><span className={styles.kpiLabel}>Produtos</span><span className={styles.kpiVal}>{dados.totalProdutos}</span></div>
            <div className={styles.kpi}><span className={styles.kpiLabel}>Valor Total</span><span className={styles.kpiVal}>R$ {dados.valorTotalEstoque.toFixed(2)}</span></div>
          </div>
          <div className={styles.tableWrapper}>
            <table className={styles.table}>
              <thead><tr><th>Produto</th><th>Físico</th><th>Reservado</th><th>Disponível</th><th>Valor</th><th>Nível</th></tr></thead>
              <tbody>{dados.itens.map((i: any, idx: number) => (
                <tr key={idx}><td className={styles.bold}>{i.produto}</td><td className={styles.mono}>{i.estoqueFisico}</td><td className={styles.mono}>{i.estoqueReservado}</td>
                <td className={styles.mono}>{i.estoqueDisponivel}</td><td className={styles.total}>R$ {i.valorEstoque.toFixed(2)}</td>
                <td><Badge label={i.nivel} variant={i.nivel === 'Normal' ? 'success' : i.nivel === 'Critico' ? 'critical' : 'warning'} /></td></tr>
              ))}</tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
}

function RelVendas({ filialId }: { filialId: string }) {
  const [dados, setDados] = useState<any>(null); const [loading, setLoading] = useState(false);
  const [inicio, setInicio] = useState(''); const [fim, setFim] = useState('');

  const carregar = async () => {
    if (!filialId) return; setLoading(true);
    try {
      const params: Record<string, string> = {};
      if (inicio) params.inicio = inicio; if (fim) params.fim = fim;
      const { data } = await api.get(`/relatorios/vendas/${filialId}`, { params }); setDados(data);
    } catch (err) { console.error(err); } finally { setLoading(false); }
  };

  const exportarCsv = () => {
    let url = `/api/relatorios/vendas/${filialId}/csv`;
    const params = []; if (inicio) params.push(`inicio=${inicio}`); if (fim) params.push(`fim=${fim}`);
    if (params.length) url += '?' + params.join('&');
    window.open(url, '_blank');
  };

  return (
    <div>
      <div className={styles.filterRow}>
        <Input label="Data início" type="date" value={inicio} onChange={e => setInicio(e.target.value)} />
        <Input label="Data fim" type="date" value={fim} onChange={e => setFim(e.target.value)} />
      </div>
      <div className={styles.actionRow}>
        <Button variant="primary" icon={<BarChart3 size={16} />} onClick={carregar} loading={loading}>Gerar Relatório</Button>
        {dados && <Button variant="outline" icon={<FileSpreadsheet size={16} />} onClick={exportarCsv}>Exportar CSV</Button>}
      </div>
      {dados && (
        <>
          <div className={styles.kpiRow}>
            <div className={styles.kpi}><span className={styles.kpiLabel}>Vendas Finalizadas</span><span className={styles.kpiVal}>{dados.totalVendasFinalizadas}</span></div>
            <div className={styles.kpi}><span className={styles.kpiLabel}>Canceladas</span><span className={styles.kpiValRed}>{dados.totalVendasCanceladas}</span></div>
            <div className={styles.kpi}><span className={styles.kpiLabel}>Total</span><span className={styles.kpiVal}>R$ {dados.valorTotalVendas.toFixed(2)}</span></div>
            <div className={styles.kpi}><span className={styles.kpiLabel}>Ticket Médio</span><span className={styles.kpiVal}>R$ {dados.ticketMedio.toFixed(2)}</span></div>
          </div>
          <h3 className={styles.sectionTitle}>Top 10 Produtos</h3>
          <div className={styles.tableWrapper}>
            <table className={styles.table}>
              <thead><tr><th>Produto</th><th>Qtd Vendida</th><th>Total</th></tr></thead>
              <tbody>{dados.produtosMaisVendidos.map((p: any, i: number) => (
                <tr key={i}><td className={styles.bold}>{p.produto}</td><td className={styles.mono}>{p.quantidade}</td><td className={styles.total}>R$ {p.total.toFixed(2)}</td></tr>
              ))}</tbody>
            </table>
          </div>
          <h3 className={styles.sectionTitle}>Por Forma de Pagamento</h3>
          <div className={styles.tableWrapper}>
            <table className={styles.table}>
              <thead><tr><th>Forma</th><th>Qtd</th><th>Total</th></tr></thead>
              <tbody>{dados.porFormaPagamento.map((f: any, i: number) => (
                <tr key={i}><td><Badge label={f.forma} variant="default" dot={false} /></td><td>{f.quantidade}</td><td className={styles.total}>R$ {f.total.toFixed(2)}</td></tr>
              ))}</tbody>
            </table>
          </div>
        </>
      )}
    </div>
  );
}

function RelFinanceiro({ filialId }: { filialId: string }) {
  const [dados, setDados] = useState<any>(null); const [loading, setLoading] = useState(false);

  const carregar = async () => {
    if (!filialId) return; setLoading(true);
    try { const { data } = await api.get(`/relatorios/financeiro/${filialId}`); setDados(data); }
    catch (err) { console.error(err); } finally { setLoading(false); }
  };

  const exportarCsv = () => { window.open(`/api/relatorios/financeiro/${filialId}/csv`, '_blank'); };

  return (
    <div>
      <div className={styles.actionRow}>
        <Button variant="primary" icon={<BarChart3 size={16} />} onClick={carregar} loading={loading}>Gerar Relatório</Button>
        {dados && <Button variant="outline" icon={<FileSpreadsheet size={16} />} onClick={exportarCsv}>Exportar CSV</Button>}
      </div>
      {dados && (
        <>
          <div className={styles.kpiRow}>
            <div className={styles.kpi}><span className={styles.kpiLabel}>A Receber</span><span className={styles.kpiValGreen}>R$ {dados.totalAReceber.toFixed(2)}</span></div>
            <div className={styles.kpi}><span className={styles.kpiLabel}>A Pagar</span><span className={styles.kpiValRed}>R$ {dados.totalAPagar.toFixed(2)}</span></div>
            <div className={styles.kpi}><span className={styles.kpiLabel}>Saldo Projetado</span><span className={dados.saldoProjetado >= 0 ? styles.kpiValGreen : styles.kpiValRed}>R$ {dados.saldoProjetado.toFixed(2)}</span></div>
          </div>
        </>
      )}
    </div>
  );
}
