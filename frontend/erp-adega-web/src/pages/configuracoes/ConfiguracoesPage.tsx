import { useState, useEffect } from 'react';
import { Settings, Building2, User, Shield, Database } from 'lucide-react';
import { Badge } from '@/components/ui';
import { useAuthStore } from '@/store/authStore';
import styles from './ConfiguracoesPage.module.css';

export function ConfiguracoesPage() {
  const { usuario } = useAuthStore();

  return (
    <div className={styles.page}>
      <h1 className={styles.title}>Configurações</h1>
      <p className={styles.subtitle}>Informações do sistema, empresa e usuário</p>

      <div className={styles.cards}>
        <div className={styles.card}>
          <div className={styles.cardIcon}><User size={20} /></div>
          <h2 className={styles.cardTitle}>Usuário Logado</h2>
          <div className={styles.info}><span className={styles.label}>Nome</span><span className={styles.value}>{usuario?.nome || '—'}</span></div>
          <div className={styles.info}><span className={styles.label}>E-mail</span><span className={styles.value}>{usuario?.email || '—'}</span></div>
          <div className={styles.info}><span className={styles.label}>Perfil</span><Badge label={usuario?.perfil || '—'} variant="primary" /></div>
        </div>

        <div className={styles.card}>
          <div className={styles.cardIcon}><Building2 size={20} /></div>
          <h2 className={styles.cardTitle}>Empresa</h2>
          <div className={styles.info}><span className={styles.label}>Razão Social</span><span className={styles.value}>Adega Central LTDA</span></div>
          <div className={styles.info}><span className={styles.label}>CNPJ</span><span className={styles.valueMono}>12.345.678/0001-90</span></div>
          <div className={styles.info}><span className={styles.label}>Filial Ativa</span><span className={styles.value}>Filial Centro (FC01)</span></div>
        </div>

        <div className={styles.card}>
          <div className={styles.cardIcon}><Shield size={20} /></div>
          <h2 className={styles.cardTitle}>Permissões</h2>
          <div className={styles.permList}>
            {usuario?.permissoes?.slice(0, 12).map((p, i) => (
              <Badge key={i} label={p} variant="default" dot={false} />
            ))}
            {(usuario?.permissoes?.length || 0) > 12 && (
              <span className={styles.morePerms}>+{(usuario?.permissoes?.length || 0) - 12} mais</span>
            )}
          </div>
        </div>

        <div className={styles.card}>
          <div className={styles.cardIcon}><Database size={20} /></div>
          <h2 className={styles.cardTitle}>Sistema</h2>
          <div className={styles.info}><span className={styles.label}>Versão</span><span className={styles.value}>ERP Adega v1.0</span></div>
          <div className={styles.info}><span className={styles.label}>Backend</span><span className={styles.value}>.NET 8 + SQLite</span></div>
          <div className={styles.info}><span className={styles.label}>Frontend</span><span className={styles.value}>React 18 + TypeScript</span></div>
          <div className={styles.info}><span className={styles.label}>Módulos Ativos</span><Badge label="13 módulos" variant="success" /></div>
        </div>
      </div>
    </div>
  );
}
