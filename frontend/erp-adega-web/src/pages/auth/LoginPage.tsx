import { useState, FormEvent } from 'react';
import { useNavigate } from 'react-router-dom';
import { Wine, Mail, Lock } from 'lucide-react';
import { useAuthStore } from '@/store/authStore';
import { Button } from '@/components/ui/Button';
import { Input } from '@/components/ui/Input';
import styles from './LoginPage.module.css';

export function LoginPage() {
  const [email, setEmail] = useState('');
  const [senha, setSenha] = useState('');
  const { login, carregando, erro } = useAuthStore();
  const navigate = useNavigate();

  const handleSubmit = async (e: FormEvent) => {
    e.preventDefault();
    const sucesso = await login({ email, senha });
    if (sucesso) navigate('/dashboard');
  };

  return (
    <div className={styles.page}>
      <div className={styles.card}>
        {/* Logo */}
        <div className={styles.logoArea}>
          <div className={styles.logoIcon}>
            <Wine size={28} />
          </div>
          <h1 className={styles.title}>ERP Adega</h1>
          <p className={styles.subtitle}>Sistema de gestão para adegas e comércio de bebidas</p>
        </div>

        {/* Form */}
        <form onSubmit={handleSubmit} className={styles.form}>
          <Input
            label="E-mail"
            type="email"
            placeholder="seu@email.com"
            icon={<Mail size={16} />}
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
          />

          <Input
            label="Senha"
            type="password"
            placeholder="••••••••"
            icon={<Lock size={16} />}
            value={senha}
            onChange={(e) => setSenha(e.target.value)}
            required
          />

          {erro && (
            <div className={styles.error}>{erro}</div>
          )}

          <Button type="submit" variant="primary" size="lg" fullWidth loading={carregando}>
            Entrar
          </Button>
        </form>

        <p className={styles.footer}>
          ERP Adega v1.0 — Gestão profissional
        </p>
      </div>
    </div>
  );
}
