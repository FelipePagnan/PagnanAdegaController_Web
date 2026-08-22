import { useState, useEffect, useCallback } from 'react';
import { Truck, Plus, Edit, Power, Search, ArrowLeft, Save } from 'lucide-react';
import { Button, Input, Badge } from '@/components/ui';
import api from '@/services/api';
import styles from './FornecedoresPage.module.css';

interface Fornecedor {
  id: string; razaoSocial: string; nomeFantasia?: string; cnpj: string;
  contatoTelefone?: string; contatoEmail?: string; ativo: boolean;
}

interface FornecedorDetalhe extends Fornecedor {
  observacoes?: string; contato?: { telefone?: string; email?: string; nomeContato?: string };
}

export function FornecedoresPage() {
  const [view, setView] = useState<'lista' | 'form'>('lista');
  const [editId, setEditId] = useState<string | null>(null);

  // Lista
  const [fornecedores, setFornecedores] = useState<Fornecedor[]>([]);
  const [loading, setLoading] = useState(true);
  const [busca, setBusca] = useState('');

  // Form
  const [razaoSocial, setRazaoSocial] = useState('');
  const [nomeFantasia, setNomeFantasia] = useState('');
  const [cnpj, setCnpj] = useState('');
  const [telefone, setTelefone] = useState('');
  const [email, setEmail] = useState('');
  const [nomeContato, setNomeContato] = useState('');
  const [observacoes, setObservacoes] = useState('');
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState('');

  const carregar = useCallback(async () => {
    setLoading(true);
    try {
      const { data } = await api.get('/fornecedores');
      setFornecedores(data);
    } catch (err) { console.error(err); }
    finally { setLoading(false); }
  }, []);

  useEffect(() => { if (view === 'lista') carregar(); }, [carregar, view]);

  const iniciarEdicao = async (id: string) => {
    try {
      const { data } = await api.get<FornecedorDetalhe>(`/fornecedores/${id}`);
      setEditId(id);
      setRazaoSocial(data.razaoSocial);
      setNomeFantasia(data.nomeFantasia || '');
      setCnpj(data.cnpj);
      setTelefone(data.contato?.telefone || '');
      setEmail(data.contato?.email || '');
      setNomeContato(data.contato?.nomeContato || '');
      setObservacoes(data.observacoes || '');
      setView('form');
    } catch (err) { console.error(err); }
  };

  const novoFornecedor = () => {
    setEditId(null);
    setRazaoSocial(''); setNomeFantasia(''); setCnpj('');
    setTelefone(''); setEmail(''); setNomeContato(''); setObservacoes('');
    setErro('');
    setView('form');
  };

  const inativar = async (id: string) => {
    if (!confirm('Inativar este fornecedor?')) return;
    try { await api.patch(`/fornecedores/${id}/inativar`); carregar(); }
    catch (err) { console.error(err); }
  };

  const salvar = async (e: React.FormEvent) => {
    e.preventDefault(); setErro(''); setSalvando(true);
    try {
      if (editId) {
        await api.put(`/fornecedores/${editId}`, {
          razaoSocial, nomeFantasia: nomeFantasia || null,
          telefone: telefone || null, email: email || null,
          nomeContato: nomeContato || null, observacoes: observacoes || null,
        });
      } else {
        await api.post('/fornecedores', {
          razaoSocial, cnpj, nomeFantasia: nomeFantasia || null,
          telefone: telefone || null, email: email || null,
          nomeContato: nomeContato || null, observacoes: observacoes || null,
        });
      }
      setView('lista');
    } catch (err: any) {
      setErro(err.response?.data?.erro || 'Erro ao salvar fornecedor.');
    } finally { setSalvando(false); }
  };

  const filtrados = busca
    ? fornecedores.filter(f =>
        f.razaoSocial.toLowerCase().includes(busca.toLowerCase()) ||
        (f.nomeFantasia || '').toLowerCase().includes(busca.toLowerCase()) ||
        f.cnpj.includes(busca))
    : fornecedores;

  // === LISTA ===
  if (view === 'lista') return (
    <div className={styles.page}>
      <div className={styles.header}>
        <div>
          <h1 className={styles.title}>Fornecedores</h1>
          <p className={styles.subtitle}>{fornecedores.length} fornecedores cadastrados</p>
        </div>
        <Button variant="primary" icon={<Plus size={16} />} onClick={novoFornecedor}>
          Novo Fornecedor
        </Button>
      </div>

      <div className={styles.searchBar}>
        <Input placeholder="Buscar por nome, fantasia ou CNPJ..." icon={<Search size={16} />}
          value={busca} onChange={e => setBusca(e.target.value)} />
      </div>

      <div className={styles.tableWrapper}>
        <table className={styles.table}>
          <thead>
            <tr><th>Razão Social</th><th>Fantasia</th><th>CNPJ</th><th>Telefone</th><th>Status</th><th>Ações</th></tr>
          </thead>
          <tbody>
            {loading ? (
              <tr><td colSpan={6} className={styles.loading}>Carregando...</td></tr>
            ) : filtrados.length === 0 ? (
              <tr><td colSpan={6} className={styles.empty}>
                <Truck size={32} strokeWidth={1.5} />
                <span>Nenhum fornecedor encontrado</span>
              </td></tr>
            ) : filtrados.map(f => (
              <tr key={f.id}>
                <td className={styles.bold}>{f.razaoSocial}</td>
                <td>{f.nomeFantasia || '—'}</td>
                <td className={styles.mono}>{f.cnpj}</td>
                <td>{f.contatoTelefone || '—'}</td>
                <td><Badge label={f.ativo ? 'Ativo' : 'Inativo'} variant={f.ativo ? 'success' : 'inactive'} /></td>
                <td>
                  <div className={styles.actions}>
                    <button className={styles.actionBtn} title="Editar" onClick={() => iniciarEdicao(f.id)}>
                      <Edit size={15} />
                    </button>
                    {f.ativo && (
                      <button className={styles.actionBtn} title="Inativar" onClick={() => inativar(f.id)}>
                        <Power size={15} />
                      </button>
                    )}
                  </div>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );

  // === FORMULÁRIO ===
  return (
    <div className={styles.page}>
      <button className={styles.backBtn} onClick={() => setView('lista')}>
        <ArrowLeft size={16} /> Voltar para fornecedores
      </button>

      <h1 className={styles.title}>{editId ? 'Editar Fornecedor' : 'Novo Fornecedor'}</h1>

      <div className={styles.formCard}>
        <form onSubmit={salvar} className={styles.form}>
          <div className={styles.grid2}>
            <Input label="Razão Social *" placeholder="Nome da empresa" value={razaoSocial}
              onChange={e => setRazaoSocial(e.target.value)} required />
            <Input label="Nome Fantasia" placeholder="Nome comercial" value={nomeFantasia}
              onChange={e => setNomeFantasia(e.target.value)} />
          </div>

          <Input label="CNPJ *" placeholder="00.000.000/0000-00" value={cnpj}
            onChange={e => setCnpj(e.target.value)} required disabled={!!editId} />

          <div className={styles.grid2}>
            <Input label="Telefone" placeholder="(00) 0000-0000" value={telefone}
              onChange={e => setTelefone(e.target.value)} />
            <Input label="E-mail" placeholder="contato@empresa.com" type="email" value={email}
              onChange={e => setEmail(e.target.value)} />
          </div>

          <Input label="Nome do Contato" placeholder="Pessoa de referência" value={nomeContato}
            onChange={e => setNomeContato(e.target.value)} />

          <Input label="Observações" placeholder="Notas sobre o fornecedor" value={observacoes}
            onChange={e => setObservacoes(e.target.value)} />

          {erro && <div className={styles.error}>{erro}</div>}

          <div className={styles.footerBtns}>
            <Button type="button" variant="outline" onClick={() => setView('lista')}>Cancelar</Button>
            <Button type="submit" variant="primary" icon={<Save size={16} />} loading={salvando}>
              {editId ? 'Salvar Alterações' : 'Cadastrar Fornecedor'}
            </Button>
          </div>
        </form>
      </div>
    </div>
  );
}
