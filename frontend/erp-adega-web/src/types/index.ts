// ═══════════════════════════════════════════════════════════
// Types espelhando os DTOs do backend .NET
// ═══════════════════════════════════════════════════════════

// === Enums ===

export enum UnidadeBase {
  Unidade = 1,
  Litro = 2,
  Quilograma = 3,
}

export enum TipoCodigoBarras {
  EAN13 = 1,
  EAN8 = 2,
  DUN14 = 3,
  Interno = 4,
}

export enum TipoMovimentacao {
  Entrada = 1,
  Venda = 2,
  Devolucao = 3,
  Perda = 4,
  Dano = 5,
  Ajuste = 6,
  Transferencia = 7,
  Reserva = 8,
  LiberacaoReserva = 9,
}

export enum NivelAlertaEstoque {
  Normal = 1,
  Baixo = 2,
  Critico = 3,
  Vencendo = 4,
  Vencido = 5,
}

export enum StatusVenda {
  Aberta = 1,
  Finalizada = 2,
  Cancelada = 3,
}

export enum FormaPagamento {
  Dinheiro = 1,
  PIX = 2,
  CartaoCredito = 3,
  CartaoDebito = 4,
}

// === Auth ===

export interface LoginRequest {
  email: string;
  senha: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  expiracao: string;
  usuario: UsuarioLogado;
}

export interface UsuarioLogado {
  id: string;
  nome: string;
  email: string;
  perfil: string;
  empresaId: string;
  empresaNome: string;
  permissoes: string[];
  filiaisPermitidas: string[];
}

// === Produto ===

export interface Produto {
  id: string;
  nome: string;
  descricao?: string;
  categoriaId: string;
  categoriaNome: string;
  unidadeBase: UnidadeBase;
  controlaValidade: boolean;
  estoqueMinimo?: number;
  estoqueCritico?: number;
  precoVenda: number;
  precoCusto?: number;
  ativo: boolean;
  criadoEm: string;
  atualizadoEm?: string;
  codigosBarras: CodigoBarras[];
  embalagens: Embalagem[];
}

export interface ProdutoResumo {
  id: string;
  nome: string;
  categoriaNome: string;
  precoVenda: number;
  ativo: boolean;
}

export interface CodigoBarras {
  id: string;
  codigo: string;
  tipo: TipoCodigoBarras;
  principal: boolean;
}

export interface Embalagem {
  id: string;
  nome: string;
  quantidadeUnidades: number;
  codigoBarras?: string;
  precoSugerido?: number;
}

export interface CriarProdutoRequest {
  nome: string;
  descricao?: string;
  categoriaId: string;
  unidadeBase: UnidadeBase;
  precoVenda: number;
  controlaValidade?: boolean;
  estoqueMinimo?: number;
  estoqueCritico?: number;
  codigosBarras?: { codigo: string; tipo: TipoCodigoBarras; principal: boolean }[];
  embalagens?: { nome: string; quantidadeUnidades: number; codigoBarras?: string; precoSugerido?: number }[];
}

// === Estoque ===

export interface EstoqueProduto {
  id: string;
  produtoId: string;
  produtoNome: string;
  filialId: string;
  estoqueFisico: number;
  estoqueReservado: number;
  estoqueDisponivel: number;
  localizacaoFisica?: string;
  nivelAlerta: NivelAlertaEstoque;
  fardoQuantidade?: number;
  fardos?: number;
  unidadesRestantes?: number;
  atualizadoEm: string;
}

export interface MovimentacaoEstoque {
  id: string;
  produtoId: string;
  produtoNome: string;
  tipo: TipoMovimentacao;
  quantidade: number;
  saldoAnterior: number;
  saldoPosterior: number;
  loteCodigo?: string;
  motivo?: string;
  documentoOrigem?: string;
  usuarioNome: string;
  criadoEm: string;
}

export interface Lote {
  id: string;
  produtoId: string;
  codigo: string;
  dataFabricacao?: string;
  dataValidade?: string;
  fornecedorNome?: string;
  notaFiscal?: string;
  custoUnitario: number;
  quantidadeRecebida: number;
  quantidadeAtual: number;
  vencido: boolean;
  vencendo: boolean;
  criadoEm: string;
}

// === Categoria ===

export interface Categoria {
  id: string;
  nome: string;
  descricao?: string;
  ativa: boolean;
}

// === Paginação ===

export interface PagedResult<T> {
  items: T[];
  total: number;
  pagina: number;
  tamanhoPagina: number;
  totalPaginas: number;
}

// === Venda ===

export enum StatusVenda {
  Aberta = 1,
  Finalizada = 2,
  Cancelada = 3,
}

export interface VendaResumo {
  id: string;
  numero: number;
  status: StatusVenda;
  total: number;
  totalItens: number;
  usuarioNome: string;
  formaPagamentoPrincipal: string;
  criadoEm: string;
}

export interface Venda {
  id: string;
  numero: number;
  filialId: string;
  clienteId?: string;
  clienteNome?: string;
  status: StatusVenda;
  subTotal: number;
  desconto: number;
  total: number;
  totalPago: number;
  troco: number;
  totalItens: number;
  usuarioNome: string;
  motivoCancelamento?: string;
  criadoEm: string;
  finalizadoEm?: string;
  itens: ItemVenda[];
  pagamentos: PagamentoVenda[];
}

export interface ItemVenda {
  id: string;
  produtoId: string;
  produtoNome: string;
  quantidade: number;
  precoUnitario: number;
  total: number;
  embalagemNome?: string;
  unidadesPorEmbalagem?: number;
}

export interface PagamentoVenda {
  id: string;
  forma: FormaPagamento;
  valor: number;
  taxaPercentual: number;
  taxaValor: number;
  valorLiquido: number;
}

// === Erro ===

export interface ApiError {
  codigo: string;
  mensagem: string;
}
