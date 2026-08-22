import { create } from 'zustand';
import api from '@/services/api';
import type { LoginRequest, LoginResponse, UsuarioLogado } from '@/types';

interface AuthState {
  usuario: UsuarioLogado | null;
  token: string | null;
  carregando: boolean;
  erro: string | null;

  login: (req: LoginRequest) => Promise<boolean>;
  logout: () => void;
  carregarSessao: () => void;
  temPermissao: (permissao: string) => boolean;
  temAcessoFilial: (filialId: string) => boolean;
}

export const useAuthStore = create<AuthState>((set, get) => ({
  usuario: null,
  token: null,
  carregando: false,
  erro: null,

  login: async (req: LoginRequest) => {
    set({ carregando: true, erro: null });
    try {
      const { data } = await api.post<LoginResponse>('/auth/login', req);
      localStorage.setItem('erp_token', data.token);
      localStorage.setItem('erp_refresh_token', data.refreshToken);
      set({
        usuario: data.usuario,
        token: data.token,
        carregando: false,
      });
      return true;
    } catch (err: any) {
      const msg = err.response?.data?.mensagem ?? 'Erro ao fazer login.';
      set({ erro: msg, carregando: false });
      return false;
    }
  },

  logout: () => {
    localStorage.removeItem('erp_token');
    localStorage.removeItem('erp_refresh_token');
    set({ usuario: null, token: null });
    window.location.href = '/login';
  },

  carregarSessao: () => {
    const token = localStorage.getItem('erp_token');
    if (token) {
      set({ token });
      // Carregar dados do usuário
      api.get('/auth/me')
        .then(({ data }) => set({ usuario: data }))
        .catch(() => {
          localStorage.removeItem('erp_token');
          set({ token: null });
        });
    }
  },

  temPermissao: (permissao: string) => {
    const { usuario } = get();
    if (!usuario) return false;
    return usuario.permissoes.includes(permissao.toLowerCase());
  },

  temAcessoFilial: (filialId: string) => {
    const { usuario } = get();
    if (!usuario) return false;
    return usuario.filiaisPermitidas.includes(filialId);
  },
}));
