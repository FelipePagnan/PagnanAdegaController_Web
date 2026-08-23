import { useState, useEffect, useCallback } from 'react';
import { ClipboardList, Search, Save, Check, AlertTriangle } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import api from '@/services/api';
import styles from './InventarioPage.module.css';

function useFilialId() {
  const [id, setId] = useState('');
  useEffect(() => { const t = localStorage.getItem('erp_token'); if (t) { try { const p = JSON.parse(atob(t.split('.')[1])); const f = Array.isArray(p.filial_id) ? p.filial_id : [p.filial_id]; if (f[0]) setId(f[0]); } catch {} } }, []);
  return id;
}

interface ItemContagem {
  produtoId: string; produtoNome: string; quantidadeSistema: number;
  quantidadeContada: string; divergencia: number | null; status: string;
}

export function InventarioPage() {
  const filialId = useFilialId();
  const [itens, setItens] = useState<ItemContagem[]>([]);
  const [loading, setLoading] = useState(true);
  const [motivo, setMotivo] = useState('Inventário periódico');
  const [salvando, setSalvando] = useState(false);
  const [resultado, setResultado] = useState<any>(null);
  const [erro, setErro] = useState('');

  const carregar = useCallback(async () => {
    if (!filialId) return; setLoading(true);
    try {
      const { data } = await api.get(`/inventario/${filialId}`);
      setItens(data.map((i: any) => ({ ...i, quantidadeContada: String(i.quantidadeSistema), divergencia: null, status: 'Pendente' })));
      setResultado(null);
    } catch (err) { console.error(err); }
    finally { setLoading(false); }
  }, [filialId]);

  useEffect(() => { carregar(); }, [carregar]);

  const atualizarContagem = (idx: number, valor: string) => {
    const novos = [...itens];
    novos[idx].quantidadeContada = valor;
    const contado = parseInt(valor) || 0;
    novos[idx].divergencia = contado - novos[idx].quantidadeSistema;
    novos[idx].status = novos[idx].divergencia === 0 ? 'OK' : 'Divergente';
    setItens(novos);
  };

  const registrar = async () => {
    if (!motivo.trim()) { setErro('Motivo é obrigatório.'); return; }
    setErro(''); setSalvando(true);
    try {
      const { data } = await api.post(`/inventario/${filialId}`, {
        motivo,
        itens: itens.map(i => ({
          produtoId: i.produtoId, produtoNome: i.produtoNome,
          quantidadeContada: parseInt(i.quantidadeContada) || 0
        }))
      });
      setResultado(data);
    } catch (err: any) { setErro(err.response?.data?.erro || 'Erro ao registrar.'); }
    finally { setSalvando(false); }
  };

  const totalDivergentes = itens.filter(i => i.divergencia !== null && i.divergencia !== 0).length;

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>Inventário</h1>
          <p className={styles.subtitle}>Contagem geral com ajuste automático de divergências</p>
        </div>
        <Button variant="outline" size="sm" onClick={carregar}>Recarregar Saldos</Button>
      </div>

      {resultado ? (
        <div className={styles.resultCard}>
          <div className={styles.resultHeader}>
            <Check size={24} /> <h2>Inventário Registrado</h2>
          </div>
          <div className={styles.kpiRow}>
            <div className={styles.kpi}><span className={styles.kpiLabel}>Total Produtos</span><span className={styles.kpiVal}>{resultado.totalProdutos}</span></div>
            <div className={styles.kpi}><span className={styles.kpiLabel}>Ajustes Realizados</span><span className={styles.kpiValRed}>{resultado.totalAjustes}</span></div>
            <div className={styles.kpi}><span className={styles.kpiLabel}>Sem Divergência</span><span className={styles.kpiValGreen}>{resultado.totalSemDivergencia}</span></div>
          </div>
          <div className={styles.tableWrapper}>
            <table className={styles.table}>
              <thead><tr><th>Produto</th><th>Sistema</th><th>Contado</th><th>Divergência</th><th>Status</th></tr></thead>
              <tbody>
                {resultado.itens.map((i: any, idx: number) => (
                  <tr key={idx}>
                    <td className={styles.bold}>{i.produtoNome}</td>
                    <td className={styles.mono}>{i.quantidadeSistema}</td>
                    <td className={styles.mono}>{i.quantidadeContada}</td>
                    <td className={`${styles.mono} ${i.divergencia !== 0 ? styles.negativo : styles.positivo}`}>
                      {i.divergencia > 0 ? '+' : ''}{i.divergencia}
                    </td>
                    <td><Badge label={i.status} variant={i.status === 'OK' ? 'success' : 'warning'} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          <div className={styles.footerBtns}>
            <Button variant="primary" onClick={carregar}>Novo Inventário</Button>
          </div>
        </div>
      ) : (
        <>
          <div className={styles.motivoBox}>
            <Input label="Motivo do inventário *" value={motivo} onChange={e => setMotivo(e.target.value)} />
            {totalDivergentes > 0 && (
              <div className={styles.alertBar}>
                <AlertTriangle size={16} /> {totalDivergentes} produto(s) com divergência
              </div>
            )}
          </div>

          <div className={styles.tableWrapper}>
            <table className={styles.table}>
              <thead><tr><th>Produto</th><th>Sistema</th><th>Contagem</th><th>Divergência</th><th>Status</th></tr></thead>
              <tbody>
                {loading ? <tr><td colSpan={5} className={styles.empty}>Carregando...</td></tr> :
                itens.length === 0 ? <tr><td colSpan={5} className={styles.empty}>Nenhum produto em estoque para contar</td></tr> :
                itens.map((item, idx) => (
                  <tr key={idx}>
                    <td className={styles.bold}>{item.produtoNome}</td>
                    <td className={styles.mono}>{item.quantidadeSistema}</td>
                    <td>
                      <input type="number" className={styles.inputContagem} value={item.quantidadeContada}
                        onChange={e => atualizarContagem(idx, e.target.value)} />
                    </td>
                    <td className={`${styles.mono} ${item.divergencia && item.divergencia !== 0 ? styles.negativo : styles.positivo}`}>
                      {item.divergencia !== null ? (item.divergencia > 0 ? '+' : '') + item.divergencia : '—'}
                    </td>
                    <td><Badge label={item.status} variant={item.status === 'OK' ? 'success' : item.status === 'Divergente' ? 'warning' : 'default'} /></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {erro && <div className={styles.error}>{erro}</div>}

          <div className={styles.footerBtns}>
            <Button variant="primary" icon={<Save size={16} />} loading={salvando} onClick={registrar}
              disabled={itens.length === 0}>
              Registrar Contagem ({itens.length} produtos)
            </Button>
          </div>
        </>
      )}
    </div>
  );
}
