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

function PlaceholderPage({ titulo }: { titulo: string }) {
  return (
    <div>
      <h1 style={{ fontSize: 26, fontWeight: 800, color: '#1A1917', letterSpacing: '-0.02em' }}>
        {titulo}
      </h1>
      <p style={{ color: '#747069', marginTop: 8 }}>Módulo em desenvolvimento — próxima fase.</p>
    </div>
  );
}

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
          <Route path="/compras" element={<PlaceholderPage titulo="Compras" />} />
          <Route path="/financeiro" element={<PlaceholderPage titulo="Financeiro" />} />
          <Route path="/clientes" element={<PlaceholderPage titulo="Clientes" />} />
          <Route path="/reservas" element={<PlaceholderPage titulo="Reservas" />} />
          <Route path="/transferencias" element={<PlaceholderPage titulo="Transferências" />} />
          <Route path="/notificacoes" element={<PlaceholderPage titulo="Notificações" />} />
          <Route path="/configuracoes" element={<PlaceholderPage titulo="Configurações" />} />
        </Route>
        <Route path="/" element={<Navigate to="/dashboard" replace />} />
        <Route path="*" element={<Navigate to="/dashboard" replace />} />
      </Routes>
    </BrowserRouter>
  );
}
