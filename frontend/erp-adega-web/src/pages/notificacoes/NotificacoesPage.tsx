import { useState, useEffect } from 'react';
import { Bell, Package, Calendar, DollarSign, ClipboardList, AlertTriangle } from 'lucide-react';
import { Badge } from '@/components/ui';
import api from '@/services/api';
import styles from './NotificacoesPage.module.css';

const iconMap: Record<string, any> = {
  estoque_critico: Package, estoque_baixo: Package,
  lote_vencido: AlertTriangle, lote_vencendo: Calendar,
  compra_pendente: ClipboardList, conta_vencida: DollarSign, reserva_expirada: Calendar,
};
const variantMap: Record<string, string> = {
  alta: 'critical', media: 'warning', baixa: 'info',
};

function useFilialId() {
  const [id, setId] = useState('');
  useEffect(() => { const t = localStorage.getItem('erp_token'); if (t) { try { const p = JSON.parse(atob(t.split('.')[1])); const f = Array.isArray(p.filial_id) ? p.filial_id : [p.filial_id]; if (f[0]) setId(f[0]); } catch {} } }, []);
  return id;
}

export function NotificacoesPage() {
  const filialId = useFilialId();
  const [notificacoes, setNotificacoes] = useState<any[]>([]);
  const [total, setTotal] = useState(0);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    if (!filialId) return;
    setLoading(true);
    api.get(`/notificacoes/${filialId}`)
      .then(({ data }) => { setNotificacoes(data.itens || []); setTotal(data.total || 0); })
      .catch(console.error)
      .finally(() => setLoading(false));
  }, [filialId]);

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <Bell size={22} />
        <div>
          <h1 className={styles.title}>Notificações</h1>
          <p className={styles.subtitle}>{total} alerta{total !== 1 ? 's' : ''} no sistema</p>
        </div>
      </div>

      {loading ? <p className={styles.loading}>Carregando...</p> :
      notificacoes.length === 0 ? (
        <div className={styles.emptyBox}>
          <Bell size={48} strokeWidth={1} />
          <span className={styles.emptyText}>Nenhum alerta — tudo em ordem!</span>
        </div>
      ) : (
        <div className={styles.list}>
          {notificacoes.map((n: any, i: number) => {
            const Icon = iconMap[n.tipo] || Bell;
            const variant = variantMap[n.prioridade] || 'default';
            return (
              <div key={i} className={styles.item}>
                <div className={`${styles.iconBox} ${styles[variant]}`}>
                  <Icon size={18} />
                </div>
                <div className={styles.itemContent}>
                  <span className={styles.itemTitle}>{n.titulo}</span>
                  <span className={styles.itemDetalhe}>{n.detalhe}</span>
                </div>
                <Badge label={n.prioridade === 'alta' ? 'Urgente' : 'Atenção'} variant={variant as any} />
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}
