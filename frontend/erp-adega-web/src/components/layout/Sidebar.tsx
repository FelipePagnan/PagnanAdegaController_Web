import { useState } from 'react';
import { NavLink, useLocation } from 'react-router-dom';
import {
  Wine, ShoppingCart, Package, BarChart3, ClipboardList,
  DollarSign, Users, Calendar, Truck, Bell, Settings,
  ChevronLeft, LogOut, Search, Receipt
} from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import styles from './Sidebar.module.css';

const menuItems = [
  { path: '/dashboard', label: 'Dashboard', icon: BarChart3 },
  { path: '/vendas', label: 'Vendas / PDV', icon: ShoppingCart },
  { path: '/vendas/lista', label: 'Histórico Vendas', icon: Receipt },
  { path: '/estoque', label: 'Estoque', icon: Package },
  { path: '/produtos', label: 'Produtos', icon: Wine },
  { path: '/compras', label: 'Compras', icon: ClipboardList },
  { path: '/financeiro', label: 'Financeiro', icon: DollarSign },
  { path: '/clientes', label: 'Clientes', icon: Users },
  { path: '/reservas', label: 'Reservas', icon: Calendar },
  { path: '/transferencias', label: 'Transferências', icon: Truck },
  { path: '/notificacoes', label: 'Notificações', icon: Bell, badge: 3 },
  { path: '/configuracoes', label: 'Configurações', icon: Settings },
];

export function Sidebar() {
  const [collapsed, setCollapsed] = useState(false);
  const { usuario, logout } = useAuthStore();
  const location = useLocation();

  return (
    <aside className={`${styles.sidebar} ${collapsed ? styles.collapsed : ''}`}>
      {/* Header */}
      <div className={styles.header}>
        <div className={styles.logo}>
          <div className={styles.logoIcon}>
            <Wine size={18} />
          </div>
          {!collapsed && (
            <div className={styles.logoText}>
              <span className={styles.logoTitle}>ERP Adega</span>
              <span className={styles.logoSub}>Filial Centro</span>
            </div>
          )}
        </div>
        <button className={styles.collapseBtn} onClick={() => setCollapsed(!collapsed)}>
          <ChevronLeft size={16} style={{ transform: collapsed ? 'rotate(180deg)' : 'none' }} />
        </button>
      </div>

      {/* Search */}
      {!collapsed && (
        <div className={styles.search}>
          <Search size={14} />
          <span>Buscar...</span>
        </div>
      )}

      {/* Navigation */}
      <nav className={styles.nav}>
        {menuItems.map((item) => {
          const Icon = item.icon;
          const isActive = location.pathname === item.path;

          return (
            <NavLink
              key={item.path}
              to={item.path}
              className={`${styles.navItem} ${isActive ? styles.active : ''}`}
              title={collapsed ? item.label : undefined}
            >
              <Icon size={18} />
              {!collapsed && (
                <>
                  <span className={styles.navLabel}>{item.label}</span>
                  {item.badge && (
                    <span className={styles.badge}>{item.badge}</span>
                  )}
                </>
              )}
            </NavLink>
          );
        })}
      </nav>

      {/* Footer */}
      <div className={styles.footer}>
        {!collapsed && usuario && (
          <div className={styles.user}>
            <div className={styles.avatar}>
              {usuario.nome.charAt(0).toUpperCase()}
            </div>
            <div className={styles.userInfo}>
              <span className={styles.userName}>{usuario.nome}</span>
              <span className={styles.userRole}>{usuario.perfil}</span>
            </div>
          </div>
        )}
        <button className={styles.logoutBtn} onClick={logout} title="Sair">
          <LogOut size={16} />
          {!collapsed && <span>Sair</span>}
        </button>
      </div>
    </aside>
  );
}
