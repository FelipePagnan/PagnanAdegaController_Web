import { BrowserRouter, Routes, Route, Navigate } from 'react-router-dom';
import { useEffect } from 'react';
import { useAuthStore } from '@/store/authStore';
import { MainLayout } from '@/components/layout/MainLayout';
import { LoginPage } from '@/pages/auth/LoginPage';
import { DashboardPage } from '@/pages/dashboard/DashboardPage';
import { ProdutosPage } from '@/pages/produtos/ProdutosPage';
import { ProdutoFormPage } from '@/pages/produtos/ProdutoFormPage';
import { EstoquePage } from '@/pages/estoque/EstoquePage';
import { PDVPage } from '@/pages/vendas/PDVPage';
import { VendasListPage } from '@/pages/vendas/VendasListPage';
import { ComprasPage } from '@/pages/compras/ComprasPage';
import { FornecedoresPage } from '@/pages/fornecedores/FornecedoresPage';
import { FinanceiroPage } from '@/pages/financeiro/FinanceiroPage';
import { ClientesPage } from '@/pages/clientes/ClientesPage';
import { ReservasPage } from '@/pages/reservas/ReservasPage';
import { NotificacoesPage } from '@/pages/notificacoes/NotificacoesPage';
import { ConfiguracoesPage } from '@/pages/configuracoes/ConfiguracoesPage';
import { InventarioPage } from '@/pages/inventario/InventarioPage';
import { RelatoriosPage } from '@/pages/relatorios/RelatoriosPage';
import { TransferenciasPage } from '@/pages/transferencias/TransferenciasPage';

function ProtectedRoute({ children }: { children: React.ReactNode }) {
  const token = useAuthStore((s) => s.token);
  const storedToken = localStorage.getItem('erp_token');
  if (!token && !storedToken) return <Navigate to="/login" replace />;
  return <>{children}</>;
}

export default function App() {
  const carregarSessao = useAuthStore((s) => s.carregarSessao);
  useEffect(() => { carregarSessao(); }, [carregarSessao]);

  return (
    <BrowserRouter>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route element={<ProtectedRoute><MainLayout /></ProtectedRoute>}>
          <Route path="/dashboard" element={<DashboardPage />} />
          <Route path="/produtos" element={<ProdutosPage />} />
          <Route path="/produtos/novo" element={<ProdutoFormPage />} />
          <Route path="/produtos/:id" element={<ProdutoFormPage />} />
          <Route path="/estoque" element={<EstoquePage />} />
          <Route path="/vendas" element={<PDVPage />} />
          <Route path="/vendas/lista" element={<VendasListPage />} />
          <Route path="/compras" element={<ComprasPage />} />
          <Route path="/fornecedores" element={<FornecedoresPage />} />
          <Route path="/financeiro" element={<FinanceiroPage />} />
          <Route path="/clientes" element={<ClientesPage />} />
          <Route path="/reservas" element={<ReservasPage />} />
          <Route path="/notificacoes" element={<NotificacoesPage />} />
          <Route path="/inventario" element={<InventarioPage />} />
          <Route path="/relatorios" element={<RelatoriosPage />} />
          <Route path="/configuracoes" element={<ConfiguracoesPage />} />
        </Route>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
