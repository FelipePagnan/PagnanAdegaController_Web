namespace ERP.Adega.Domain.Enums;

public enum UnidadeBase
{
    Unidade = 1,
    Litro = 2,
    Quilograma = 3
}

public enum TipoCodigoBarras
{
    EAN13 = 1,
    EAN8 = 2,
    DUN14 = 3,
    Interno = 4
}

public enum TipoMovimentacao
{
    Entrada = 1,
    Venda = 2,
    Devolucao = 3,
    Perda = 4,
    Dano = 5,
    Ajuste = 6,
    Transferencia = 7,
    Reserva = 8,
    LiberacaoReserva = 9
}

public enum NivelAlertaEstoque
{
    Normal = 1,
    Baixo = 2,
    Critico = 3,
    Vencendo = 4,
    Vencido = 5
}

public enum StatusVenda
{
    Aberta = 1,
    Finalizada = 2,
    Cancelada = 3
}

public enum StatusReserva
{
    Ativa = 1,
    Expirada = 2,
    Retirada = 3,
    Cancelada = 4
}

public enum StatusPedidoCompra
{
    Rascunho = 1,
    AguardandoAprovacao = 2,
    Aprovado = 3,
    Rejeitado = 4,
    Recebido = 5,
    RecebidoParcial = 6,
    Cancelado = 7
}

public enum StatusConta
{
    Aberta = 1,
    Paga = 2,
    Vencida = 3,
    Cancelada = 4
}

public enum StatusCaixa
{
    Aberto = 1,
    Fechado = 2
}

public enum StatusTransferencia
{
    Solicitada = 1,
    Aprovada = 2,
    Separada = 3,
    Enviada = 4,
    Recebida = 5,
    Cancelada = 6
}

public enum FormaPagamento
{
    Dinheiro = 1,
    PIX = 2,
    CartaoCredito = 3,
    CartaoDebito = 4
}

public enum AcaoAuditoria
{
    Criar = 1,
    Alterar = 2,
    Excluir = 3,
    Aprovar = 4,
    Rejeitar = 5,
    Cancelar = 6,
    Autorizar = 7
}
