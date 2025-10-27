'use client';

import { create } from 'zustand';
import { persist } from 'zustand/middleware';
import { apiClient } from '../lib/api-client';
import type { AuthResponseDto, PasswordResetRequestDto, UserSummaryDto } from '../types/api';

interface AuthState {
  token: string | null;
  user: UserSummaryDto | null;
  isLoading: boolean;
  error: string | null;
  mfaSessionId: string | null;
  mfaExpiresAt: string | null;
  pendingUsername: string | null;
  login: (usernameOrEmail: string, password: string) => Promise<void>;
  completeMfa: (verificationCode: string) => Promise<void>;
  logout: () => void;
  initialize: () => Promise<void>;
  setUser: (user: UserSummaryDto | null) => void;
  requestPasswordReset: (usernameOrEmail: string) => Promise<PasswordResetRequestDto>;
  resetPassword: (usernameOrEmail: string, token: string, newPassword: string) => Promise<void>;
}

export const useAuthStore = create<AuthState>()(
  persist(
    (set, get) => ({
      token: null,
      user: null,
      isLoading: false,
      error: null,
      mfaSessionId: null,
      mfaExpiresAt: null,
      pendingUsername: null,
      async login(usernameOrEmail, password) {
        set({ isLoading: true, error: null });
        try {
          const response = await apiClient.post<AuthResponseDto>('/auth/login', {
            usernameOrEmail,
            password
          });

          const { token, user, requiresTwoFactor, twoFactorSessionId, sessionExpiresAt } = response.data;

          if (requiresTwoFactor) {
            set({
              isLoading: false,
              token: null,
              user: null,
              mfaSessionId: twoFactorSessionId ?? null,
              mfaExpiresAt: sessionExpiresAt ?? null,
              pendingUsername: usernameOrEmail,
              error: null
            });
            return;
          }

          if (!token || !user) {
            set({
              isLoading: false,
              error: 'Respuesta de autenticación incompleta. Contacta con soporte.'
            });
            throw new Error('Invalid authentication response');
          }

          apiClient.defaults.headers.common.Authorization = `Bearer ${token}`;
          set({
            token,
            user,
            isLoading: false,
            mfaSessionId: null,
            mfaExpiresAt: null,
            pendingUsername: null
          });
        } catch (error: unknown) {
          set({ isLoading: false, error: 'No se pudo iniciar sesión. Revisa tus credenciales.' });
          throw error;
        }
      },
      async completeMfa(verificationCode) {
        const { pendingUsername, mfaSessionId } = get();
        if (!pendingUsername || !mfaSessionId) {
          throw new Error('No existe un desafío MFA activo.');
        }

        set({ isLoading: true, error: null });

        try {
          const response = await apiClient.post<AuthResponseDto>('/auth/login/mfa', {
            usernameOrEmail: pendingUsername,
            sessionId: mfaSessionId,
            verificationCode
          });

          const { token, user } = response.data;

          if (!token || !user) {
            set({ isLoading: false, error: 'El código MFA fue válido pero no se recibió un token.' });
            throw new Error('Missing token after MFA completion');
          }

          apiClient.defaults.headers.common.Authorization = `Bearer ${token}`;
          set({
            token,
            user,
            isLoading: false,
            mfaSessionId: null,
            mfaExpiresAt: null,
            pendingUsername: null,
            error: null
          });
        } catch (error: unknown) {
          set({ isLoading: false, error: 'El código MFA no es válido o ha expirado.' });
          throw error;
        }
      },
      logout() {
        apiClient.defaults.headers.common.Authorization = undefined;
        set({ token: null, user: null, mfaSessionId: null, mfaExpiresAt: null, pendingUsername: null });
      },
      async initialize() {
        const token = get().token;
        if (token) {
          apiClient.defaults.headers.common.Authorization = `Bearer ${token}`;
          try {
            const response = await apiClient.get<UserSummaryDto>('/auth/me');
            set({ user: response.data });
          } catch (error) {
            console.error('No se pudo validar la sesión', error);
            set({ token: null, user: null });
          }
        }
      },
      setUser(user) {
        set({ user });
      },
      async requestPasswordReset(usernameOrEmail) {
        set({ isLoading: true, error: null });
        try {
          const response = await apiClient.post<PasswordResetRequestDto>('/auth/password/forgot', {
            usernameOrEmail
          });
          set({ isLoading: false });
          return response.data;
        } catch (error: unknown) {
          set({ isLoading: false, error: 'No se pudo iniciar el restablecimiento de contraseña.' });
          throw error;
        }
      },
      async resetPassword(usernameOrEmail, token, newPassword) {
        set({ isLoading: true, error: null });
        try {
          await apiClient.post('/auth/password/reset', {
            usernameOrEmail,
            token,
            newPassword
          });
          set({ isLoading: false });
        } catch (error: unknown) {
          set({ isLoading: false, error: 'No se pudo restablecer la contraseña.' });
          throw error;
        }
      }
    }),
    {
      name: 'gestor-inventario-auth',
      partialize: (state) => ({ token: state.token, user: state.user })
    }
  )
);
