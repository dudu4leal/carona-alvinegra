import axios from 'axios';
import type { DefinirResponsavelVanRequest, VanAlocada } from '../types';

const TOKEN_KEY = 'carona.token';

export const api = axios.create({
  baseURL: import.meta.env.VITE_API_URL ?? 'http://localhost:5000',
});

export function getToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setToken(token: string): void {
  localStorage.setItem(TOKEN_KEY, token);
}

export function clearToken(): void {
  localStorage.removeItem(TOKEN_KEY);
}

api.interceptors.request.use((config) => {
  const token = getToken();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});

api.interceptors.response.use(
  (response) => response,
  (error: unknown) => {
    if (axios.isAxiosError(error)) {
      const isLoginRequest = error.config?.url?.includes('/api/auth/login') ?? false;

      if (error.response?.status === 401 && !isLoginRequest) {
        clearToken();

        if (window.location.pathname !== '/login') {
          window.location.assign('/login');
        }
      }
    }

    return Promise.reject(error);
  },
);

export function apiErrorMessage(error: unknown): string {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data as { message?: string } | undefined;

    if (data?.message) {
      return data.message;
    }

    if (error.response) {
      return `Erro ${error.response.status} ao chamar a API.`;
    }

    return 'Não foi possível conectar à API.';
  }

  return 'Erro inesperado.';
}

export function definirResponsavelVan(
  jogoId: string,
  vanId: string,
  body: DefinirResponsavelVanRequest,
): Promise<VanAlocada> {
  return api
    .put<VanAlocada>(`/api/jogos/${jogoId}/alocacao/vans/${vanId}`, body)
    .then((response) => response.data);
}
