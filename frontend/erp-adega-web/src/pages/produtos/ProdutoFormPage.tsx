import { useState, useEffect } from 'react';
import { useNavigate, useParams } from 'react-router-dom';
import { ArrowLeft, Plus, Trash2, Save } from 'lucide-react';
import { Button, Input } from '@/components/ui';
import type { Categoria, CriarProdutoRequest, Produto } from '@/types';
import { UnidadeBase, TipoCodigoBarras } from '@/types';
import api from '@/services/api';
import styles from './ProdutoFormPage.module.css';

export function ProdutoFormPage() {
  const { id } = useParams();
  const isEdit = !!id && id !== 'novo';
  const navigate = useNavigate();

  const [categorias, setCategorias] = useState<Categoria[]>([]);
  const [loading, setLoading] = useState(false);
  const [salvando, setSalvando] = useState(false);
  const [erro, setErro] = useState('');

  // Form state
  const [nome, setNome] = useState('');
  const [descricao, setDescricao] = useState('');
  const [categoriaId, setCategoriaId] = useState('');
  const [unidadeBase, setUnidadeBase] = useState<UnidadeBase>(UnidadeBase.Unidade);
  const [precoVenda, setPrecoVenda] = useState('');
  const [controlaValidade, setControlaValidade] = useState(false);
  const [estoqueMinimo, setEstoqueMinimo] = useState('');
  const [estoqueCritico, setEstoqueCritico] = useState('');

  // Códigos de barras
  const [codigosBarras, setCodigosBarras] = useState<{ codigo: string; tipo: TipoCodigoBarras; principal: boolean }[]>([]);

  // Embalagens
  const [embalagens, setEmbalagens] = useState<{ nome: string; quantidadeUnidades: string; codigoBarras: string; precoSugerido: string }[]>([]);

  useEffect(() => {
    api.get('/categorias').then(({ data }) => setCategorias(data)).catch(() => {});

    if (isEdit) {
      setLoading(true);
      api.get<Produto>(`/produtos/${id}`)
        .then(({ data }) => {
          setNome(data.nome);
          setDescricao(data.descricao || '');
          setCategoriaId(data.categoriaId);
          setUnidadeBase(data.unidadeBase);
          setPrecoVenda(data.precoVenda.toString());
          setControlaValidade(data.controlaValidade);
          setEstoqueMinimo(data.estoqueMinimo?.toString() || '');
          setEstoqueCritico(data.estoqueCritico?.toString() || '');
          setCodigosBarras(data.codigosBarras.map(cb => ({
            codigo: cb.codigo, tipo: cb.tipo, principal: cb.principal
          })));
          setEmbalagens(data.embalagens.map(e => ({
            nome: e.nome,
            quantidadeUnidades: e.quantidadeUnidades.toString(),
            codigoBarras: e.codigoBarras || '',
            precoSugerido: e.precoSugerido?.toString() || '',
          })));
        })
        .catch(() => setErro('Erro ao carregar produto.'))
        .finally(() => setLoading(false));
    }
  }, [id, isEdit]);

  const addCodigoBarras = () => {
    setCodigosBarras([...codigosBarras, { codigo: '', tipo: TipoCodigoBarras.EAN13, principal: codigosBarras.length === 0 }]);
  };

  const removeCodigoBarras = (idx: number) => {
    setCodigosBarras(codigosBarras.filter((_, i) => i !== idx));
  };

  const addEmbalagem = () => {
    setEmbalagens([...embalagens, { nome: '', quantidadeUnidades: '', codigoBarras: '', precoSugerido: '' }]);
  };

  const removeEmbalagem = (idx: number) => {
    setEmbalagens(embalagens.filter((_, i) => i !== idx));
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setErro('');
    setSalvando(true);

    try {
      if (isEdit) {
        await api.put(`/produtos/${id}`, {
          nome, descricao: descricao || null, categoriaId,
          precoVenda: parseFloat(precoVenda), controlaValidade,
          estoqueMinimo: estoqueMinimo ? parseInt(estoqueMinimo) : null,
          estoqueCritico: estoqueCritico ? parseInt(estoqueCritico) : null,
        });
      } else {
        const payload: CriarProdutoRequest = {
          nome, descricao: descricao || undefined, categoriaId,
          unidadeBase, precoVenda: parseFloat(precoVenda), controlaValidade,
          estoqueMinimo: estoqueMinimo ? parseInt(estoqueMinimo) : undefined,
          estoqueCritico: estoqueCritico ? parseInt(estoqueCritico) : undefined,
          codigosBarras: codigosBarras.filter(cb => cb.codigo).map(cb => ({
            codigo: cb.codigo, tipo: cb.tipo, principal: cb.principal,
          })),
          embalagens: embalagens.filter(e => e.nome && e.quantidadeUnidades).map(e => ({
            nome: e.nome,
            quantidadeUnidades: parseInt(e.quantidadeUnidades),
            codigoBarras: e.codigoBarras || undefined,
            precoSugerido: e.precoSugerido ? parseFloat(e.precoSugerido) : undefined,
          })),
        };
        await api.post('/produtos', payload);
      }
      navigate('/produtos');
    } catch (err: any) {
      setErro(err.response?.data?.erro || err.response?.data?.mensagem || 'Erro ao salvar produto.');
    } finally {
      setSalvando(false);
    }
  };

  if (loading) return <div style={{ padding: 40, color: '#969085' }}>Carregando...</div>;

  return (
    <div className={styles.page}>
      <button className={styles.backBtn} onClick={() => navigate('/produtos')}>
        <ArrowLeft size={16} /> Voltar para produtos
      </button>

      <h1 className={styles.title}>{isEdit ? 'Editar Produto' : 'Novo Produto'}</h1>

      <form onSubmit={handleSubmit} className={styles.form}>
        {/* Dados básicos */}
        <div className={styles.section}>
          <h2 className={styles.sectionTitle}>Dados Básicos</h2>
          <div className={styles.grid2}>
            <Input label="Nome *" placeholder="Ex: Coca-Cola 350ml" value={nome}
              onChange={e => setNome(e.target.value)} required />
            <div className={styles.field}>
              <label className={styles.label}>Categoria *</label>
              <select className={styles.select} value={categoriaId}
                onChange={e => setCategoriaId(e.target.value)} required>
                <option value="">Selecione...</option>
                {categorias.map(c => <option key={c.id} value={c.id}>{c.nome}</option>)}
              </select>
            </div>
          </div>
          <Input label="Descrição" placeholder="Descrição opcional" value={descricao}
            onChange={e => setDescricao(e.target.value)} />
          <div className={styles.grid3}>
            <Input label="Preço de Venda *" placeholder="0.00" type="number" step="0.01"
              value={precoVenda} onChange={e => setPrecoVenda(e.target.value)} required />
            {!isEdit && (
              <div className={styles.field}>
                <label className={styles.label}>Unidade Base</label>
                <select className={styles.select} value={unidadeBase}
                  onChange={e => setUnidadeBase(Number(e.target.value))}>
                  <option value={1}>Unidade</option>
                  <option value={2}>Litro</option>
                  <option value={3}>Quilograma</option>
                </select>
              </div>
            )}
            <div className={styles.field}>
              <label className={styles.label}>Controla Validade</label>
              <label className={styles.checkbox}>
                <input type="checkbox" checked={controlaValidade}
                  onChange={e => setControlaValidade(e.target.checked)} />
                <span>Sim, exigir lote com validade</span>
              </label>
            </div>
          </div>
          <div className={styles.grid2}>
            <Input label="Estoque Mínimo" placeholder="Ex: 24" type="number"
              value={estoqueMinimo} onChange={e => setEstoqueMinimo(e.target.value)}
              helper="Alerta quando estoque cair abaixo" />
            <Input label="Estoque Crítico" placeholder="Ex: 6" type="number"
              value={estoqueCritico} onChange={e => setEstoqueCritico(e.target.value)}
              helper="Alerta urgente" />
          </div>
        </div>

        {/* Códigos de barras */}
        {!isEdit && (
          <div className={styles.section}>
            <div className={styles.sectionHeader}>
              <h2 className={styles.sectionTitle}>Códigos de Barras</h2>
              <Button type="button" variant="ghost" size="sm" icon={<Plus size={14} />}
                onClick={addCodigoBarras}>Adicionar</Button>
            </div>
            {codigosBarras.map((cb, i) => (
              <div key={i} className={styles.grid3Row}>
                <Input placeholder="Código EAN" value={cb.codigo}
                  onChange={e => { const n = [...codigosBarras]; n[i].codigo = e.target.value; setCodigosBarras(n); }} />
                <select className={styles.select} value={cb.tipo}
                  onChange={e => { const n = [...codigosBarras]; n[i].tipo = Number(e.target.value); setCodigosBarras(n); }}>
                  <option value={1}>EAN-13</option>
                  <option value={2}>EAN-8</option>
                  <option value={3}>DUN-14</option>
                  <option value={4}>Interno</option>
                </select>
                <div className={styles.rowActions}>
                  <label className={styles.checkboxSmall}>
                    <input type="radio" name="principal" checked={cb.principal}
                      onChange={() => { const n = codigosBarras.map((c, j) => ({ ...c, principal: j === i })); setCodigosBarras(n); }} />
                    Principal
                  </label>
                  <button type="button" className={styles.removeBtn} onClick={() => removeCodigoBarras(i)}>
                    <Trash2 size={14} />
                  </button>
                </div>
              </div>
            ))}
            {codigosBarras.length === 0 && <p className={styles.emptyHint}>Nenhum código de barras adicionado</p>}
          </div>
        )}

        {/* Embalagens */}
        {!isEdit && (
          <div className={styles.section}>
            <div className={styles.sectionHeader}>
              <h2 className={styles.sectionTitle}>Embalagens (Fardo, Caixa, Pack)</h2>
              <Button type="button" variant="ghost" size="sm" icon={<Plus size={14} />}
                onClick={addEmbalagem}>Adicionar</Button>
            </div>
            {embalagens.map((emb, i) => (
              <div key={i} className={styles.embRow}>
                <Input placeholder="Nome (ex: Fardo)" value={emb.nome}
                  onChange={e => { const n = [...embalagens]; n[i].nome = e.target.value; setEmbalagens(n); }} />
                <Input placeholder="Qtd. unidades" type="number" value={emb.quantidadeUnidades}
                  onChange={e => { const n = [...embalagens]; n[i].quantidadeUnidades = e.target.value; setEmbalagens(n); }} />
                <Input placeholder="Código barras" value={emb.codigoBarras}
                  onChange={e => { const n = [...embalagens]; n[i].codigoBarras = e.target.value; setEmbalagens(n); }} />
                <Input placeholder="Preço sugerido" type="number" step="0.01" value={emb.precoSugerido}
                  onChange={e => { const n = [...embalagens]; n[i].precoSugerido = e.target.value; setEmbalagens(n); }} />
                <button type="button" className={styles.removeBtn} onClick={() => removeEmbalagem(i)}>
                  <Trash2 size={14} />
                </button>
              </div>
            ))}
            {embalagens.length === 0 && <p className={styles.emptyHint}>Nenhuma embalagem. Ex: Fardo com 12 unidades</p>}
          </div>
        )}

        {/* Error & Submit */}
        {erro && <div className={styles.error}>{erro}</div>}
        <div className={styles.footer}>
          <Button type="button" variant="outline" onClick={() => navigate('/produtos')}>Cancelar</Button>
          <Button type="submit" variant="primary" icon={<Save size={16} />} loading={salvando}>
            {isEdit ? 'Salvar Alterações' : 'Cadastrar Produto'}
          </Button>
        </div>
      </form>
    </div>
  );
}
