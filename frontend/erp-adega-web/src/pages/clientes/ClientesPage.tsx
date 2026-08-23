import { useState, useEffect, useCallback } from 'react';
import { Users, Plus, Edit, Power, Search, ArrowLeft, Save } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import api from '@/services/api';
import styles from './ClientesPage.module.css';

interface Cliente { id: string; nome: string; cpf?: string; cnpj?: string; telefone?: string; email?: string; ativo: boolean; }

export function ClientesPage() {
  const [view, setView] = useState<'lista' | 'form'>('lista');
  const [editId, setEditId] = useState<string | null>(null);
  const [clientes, setClientes] = useState<Cliente[]>([]); const [loading, setLoading] = useState(true); const [busca, setBusca] = useState('');
  const [nome, setNome] = useState(''); const [cpf, setCpf] = useState(''); const [cnpj, setCnpj] = useState('');
  const [telefone, setTelefone] = useState(''); const [email, setEmail] = useState(''); const [obs, setObs] = useState('');
  const [salvando, setSalvando] = useState(false); const [erro, setErro] = useState('');

  const carregar = useCallback(async () => {
    setLoading(true);
    try { const { data } = await api.get('/clientes'); setClientes(data); } catch {} finally { setLoading(false); }
  }, []);

  useEffect(() => { if (view === 'lista') carregar(); }, [carregar, view]);

  const editar = async (id: string) => {
    try {
      const { data } = await api.get(`/clientes/${id}`);
      setEditId(id); setNome(data.nome); setCpf(data.cpf || ''); setCnpj(data.cnpj || '');
      setTelefone(data.contato?.telefone || ''); setEmail(data.contato?.email || ''); setObs(data.observacoes || '');
      setErro(''); setView('form');
    } catch {}
  };

  const novo = () => { setEditId(null); setNome(''); setCpf(''); setCnpj(''); setTelefone(''); setEmail(''); setObs(''); setErro(''); setView('form'); };

  const salvar = async (e: React.FormEvent) => {
    e.preventDefault(); setErro(''); setSalvando(true);
    try {
      const body = { nome, cpf: cpf || null, cnpj: cnpj || null, telefone: telefone || null, email: email || null, observacoes: obs || null };
      if (editId) await api.put(`/clientes/${editId}`, body); else await api.post('/clientes', body);
      setView('lista');
    } catch (err: any) { setErro(err.response?.data?.erro || 'Erro ao salvar.'); } finally { setSalvando(false); }
  };

  const inativar = async (id: string) => {
    if (!confirm('Inativar este cliente?')) return;
    try { await api.patch(`/clientes/${id}/inativar`); carregar(); } catch {}
  };

  const filtrados = busca ? clientes.filter(c => c.nome.toLowerCase().includes(busca.toLowerCase()) || (c.cpf || '').includes(busca)) : clientes;

  if (view === 'form') return (
    <div style={{ maxWidth: 600 }}>
      <button onClick={() => setView('lista')} style={{ display: 'flex', alignItems: 'center', gap: 6, border: 'none', background: 'none', color: '#722F37', fontSize: 13, fontWeight: 600, cursor: 'pointer', marginBottom: 16, padding: 0 }}>
        <ArrowLeft size={16} /> Voltar
      </button>
      <h1 style={{ fontSize: 26, fontWeight: 800, color: '#1A1917', margin: '0 0 20px' }}>{editId ? 'Editar' : 'Novo'} Cliente</h1>
      <div style={{ background: '#fff', border: '1px solid #ECEAE6', borderRadius: 12, padding: 24, boxShadow: '0 1px 2px rgba(26,25,23,0.06)' }}>
        <form onSubmit={salvar} style={{ display: 'flex', flexDirection: 'column', gap: 14 }}>
          <Input label="Nome *" value={nome} onChange={e => setNome(e.target.value)} required />
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
            <Input label="CPF" placeholder="000.000.000-00" value={cpf} onChange={e => setCpf(e.target.value)} />
            <Input label="CNPJ" placeholder="00.000.000/0000-00" value={cnpj} onChange={e => setCnpj(e.target.value)} />
          </div>
          <div style={{ display: 'grid', gridTemplateColumns: '1fr 1fr', gap: 14 }}>
            <Input label="Telefone" value={telefone} onChange={e => setTelefone(e.target.value)} />
            <Input label="E-mail" type="email" value={email} onChange={e => setEmail(e.target.value)} />
          </div>
          <Input label="Observações" value={obs} onChange={e => setObs(e.target.value)} />
          {erro && <div style={{ padding: '10px 14px', borderRadius: 8, background: '#FCEDEF', color: '#C03744', fontSize: 13, borderLeft: '3px solid #C03744' }}>{erro}</div>}
          <div style={{ display: 'flex', justifyContent: 'flex-end', gap: 10 }}>
            <Button type="button" variant="outline" onClick={() => setView('lista')}>Cancelar</Button>
            <Button type="submit" variant="primary" icon={<Save size={16} />} loading={salvando}>{editId ? 'Salvar' : 'Cadastrar'}</Button>
          </div>
        </form>
      </div>
    </div>
  );

  return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div><h1 className={styles.title}>Clientes</h1><p className={styles.subtitle}>{clientes.length} clientes</p></div>
        <Button variant="primary" icon={<Plus size={16} />} onClick={novo}>Novo Cliente</Button>
      </div>
      <div className={styles.searchBar}><Input placeholder="Buscar por nome ou CPF..." icon={<Search size={16} />} value={busca} onChange={e => setBusca(e.target.value)} /></div>
      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead><tr><th>Nome</th><th>CPF</th><th>Telefone</th><th>Email</th><th>Status</th><th>Ações</th></tr></thead>
          <tbody>
            {loading ? <tr><td colSpan={6} className={styles.empty}>Carregando...</td></tr> :
            filtrados.length === 0 ? <tr><td colSpan={6} className={styles.empty}><Users size={32} strokeWidth={1.5} /><span>Nenhum cliente</span></td></tr> :
            filtrados.map(c => (
              <tr key={c.id}>
                <td style={{ fontWeight: 600, color: '#1A1917' }}>{c.nome}</td>
                <td style={{ fontFamily: 'monospace', fontSize: 12 }}>{c.cpf || '—'}</td>
                <td>{c.telefone || '—'}</td><td>{c.email || '—'}</td>
                <td><Badge label={c.ativo ? 'Ativo' : 'Inativo'} variant={c.ativo ? 'success' : 'inactive'} /></td>
                <td><div style={{ display: 'flex', gap: 4 }}>
                  <button className={styles.actionBtn} onClick={() => editar(c.id)}><Edit size={15} /></button>
                  {c.ativo && <button className={styles.actionBtn} onClick={() => inativar(c.id)}><Power size={15} /></button>}
                </div></td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
