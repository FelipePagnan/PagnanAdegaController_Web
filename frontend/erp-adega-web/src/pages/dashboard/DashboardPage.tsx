import {
  ShoppingCart, Package, DollarSign, AlertTriangle,
  TrendingUp, Clock, Wine, ArrowUpRight
} from 'lucide-react';
import { Badge } from '@/components/ui';
import styles from './DashboardPage.module.css';

// Dados mock para demonstração — serão substituídos por API
const kpis = [
  { label: 'Vendas Hoje', valor: 'R$ 12.480', sub: '47 vendas', icon: ShoppingCart, cor: 'primary' },
  { label: 'Ticket Médio', valor: 'R$ 265,53', sub: '+8% vs ontem', icon: TrendingUp, cor: 'teal' },
  { label: 'Estoque Total', valor: '4.832 un', sub: '312 produtos', icon: Package, cor: 'gold' },
  { label: 'Alertas', valor: '7', sub: '3 críticos', icon: AlertTriangle, cor: 'critical' },
];

const alertas = [
  { produto: 'Heineken Long Neck', tipo: 'Estoque Crítico', detalhe: '4 un (mín: 48)', variante: 'critical' as const },
  { produto: 'Smirnoff Ice 275ml', tipo: 'Vencendo', detalhe: 'Lote L2024-089 vence em 8 dias', variante: 'expiring' as const },
  { produto: 'Red Bull 250ml', tipo: 'Estoque Baixo', detalhe: '6 un (mín: 12)', variante: 'warning' as const },
  { produto: 'Espumante Chandon', tipo: 'Reservado', detalhe: '2 un reservadas — retirada 22/08', variante: 'reserved' as const },
];

const vendasRecentes = [
  { numero: '#1247', hora: '14:32', itens: 3, total: 'R$ 187,40', forma: 'PIX' },
  { numero: '#1246', hora: '14:18', itens: 1, total: 'R$ 48,00', forma: 'Dinheiro' },
  { numero: '#1245', hora: '13:55', itens: 7, total: 'R$ 432,60', forma: 'Crédito' },
  { numero: '#1244', hora: '13:41', itens: 2, total: 'R$ 25,80', forma: 'Débito' },
  { numero: '#1243', hora: '13:22', itens: 12, total: 'R$ 1.240,00', forma: 'PIX' },
];

export function DashboardPage() {
  return (
    <div className={styles.page}>
      {/* Header */}
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>Dashboard</h1>
          <p className={styles.subtitle}>Visão geral do dia — Filial Centro</p>
        </div>
        <div className={styles.headerActions}>
          <span className={styles.date}>
            <Clock size={14} />
            Hoje, 20 ago 2026 · 14:35
          </span>
        </div>
      </div>

      {/* KPIs */}
      <div className={styles.kpiGrid}>
        {kpis.map((kpi, i) => {
          const Icon = kpi.icon;
          return (
            <div key={i} className={`${styles.kpiCard} ${styles[kpi.cor]}`}>
              <div className={styles.kpiHeader}>
                <span className={styles.kpiLabel}>{kpi.label}</span>
                <div className={styles.kpiIcon}>
                  <Icon size={18} />
                </div>
              </div>
              <div className={styles.kpiValor}>{kpi.valor}</div>
              <div className={styles.kpiSub}>{kpi.sub}</div>
            </div>
          );
        })}
      </div>

      {/* Content Grid */}
      <div className={styles.contentGrid}>
        {/* Alertas */}
        <div className={styles.card}>
          <div className={styles.cardHeader}>
            <h2 className={styles.cardTitle}>
              <AlertTriangle size={16} /> Alertas
            </h2>
            <span className={styles.cardAction}>Ver todos</span>
          </div>
          <div className={styles.alertList}>
            {alertas.map((a, i) => (
              <div key={i} className={styles.alertItem}>
                <div className={styles.alertInfo}>
                  <span className={styles.alertProduto}>{a.produto}</span>
                  <span className={styles.alertDetalhe}>{a.detalhe}</span>
                </div>
                <Badge label={a.tipo} variant={a.variante} />
              </div>
            ))}
          </div>
        </div>

        {/* Vendas recentes */}
        <div className={styles.card}>
          <div className={styles.cardHeader}>
            <h2 className={styles.cardTitle}>
              <ShoppingCart size={16} /> Vendas Recentes
            </h2>
            <span className={styles.cardAction}>Ver todas</span>
          </div>
          <table className={styles.table}>
            <thead>
              <tr>
                <th>Venda</th>
                <th>Hora</th>
                <th>Itens</th>
                <th>Total</th>
                <th>Pgto</th>
              </tr>
            </thead>
            <tbody>
              {vendasRecentes.map((v, i) => (
                <tr key={i}>
                  <td className={styles.vendaNum}>{v.numero}</td>
                  <td>{v.hora}</td>
                  <td>{v.itens}</td>
                  <td className={styles.vendaTotal}>{v.total}</td>
                  <td><Badge label={v.forma} variant="default" dot={false} /></td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
