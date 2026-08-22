import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';
import type { ApiError } from '@/types';

const api = axios.create({
  baseURL: '/api',
  headers: { 'Content-Type': 'application/json' },
});

// Request: injeta JWT
api.interceptors.request.use((config: InternalAxiosRequestConfig) => {
  const token = localStorage.getItem('erp_token');
  if (token && config.headers) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

// Response: trata 401 e erros
api.interceptors.response.use(
  (response) => response,
  async (error: AxiosError<ApiError>) => {
    if (error.response?.status === 401) {
      // Token expirado — tentar refresh
      const refreshToken = localStorage.getItem('erp_refresh_token');
      if (refreshToken && error.config) {
        try {
          const { data } = await axios.post('/api/auth/refresh', JSON.stringify(refreshToken), {
            headers: { 'Content-Type': 'application/json' },
          });
          localStorage.setItem('erp_token', data.token);
          localStorage.setItem('erp_refresh_token', data.refreshToken);
          error.config.headers.Authorization = `Bearer ${data.token}`;
          return api(error.config);
        } catch {
          // Refresh falhou — limpar e redirecionar
          localStorage.removeItem('erp_token');
          localStorage.removeItem('erp_refresh_token');
          window.location.href = '/login';
        }
      } else {
        window.location.href = '/login';
      }
    }
    return Promise.reject(error);
  }
);

export default api;
