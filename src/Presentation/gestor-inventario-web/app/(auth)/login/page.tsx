'use client';

import { FormEvent, useMemo, useState } from 'react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { Button } from '../../../components/ui/button';
import { Input } from '../../../components/ui/input';
import { useAuthStore } from '../../../src/store/auth-store';

export default function LoginPage() {
  const router = useRouter();
  const {
    login,
    completeMfa,
    isLoading,
    error,
    token,
    mfaSessionId,
    mfaExpiresAt,
    pendingUsername,
    requestPasswordReset,
    resetPassword
  } = useAuthStore((state) => ({
    login: state.login,
    completeMfa: state.completeMfa,
    isLoading: state.isLoading,
    error: state.error,
    token: state.token,
    mfaSessionId: state.mfaSessionId,
    mfaExpiresAt: state.mfaExpiresAt,
    pendingUsername: state.pendingUsername,
    requestPasswordReset: state.requestPasswordReset,
    resetPassword: state.resetPassword
  }));

  const [usernameOrEmail, setUsernameOrEmail] = useState('');
  const [password, setPassword] = useState('');
  const [verificationCode, setVerificationCode] = useState('');
  const [showReset, setShowReset] = useState(false);
  const [resetEmail, setResetEmail] = useState('');
  const [resetToken, setResetToken] = useState('');
  const [newPassword, setNewPassword] = useState('');
  const [resetStep, setResetStep] = useState<'request' | 'confirm'>('request');
  const [resetMessage, setResetMessage] = useState<string | null>(null);
  const [resetError, setResetError] = useState<string | null>(null);
  const [deliveredToken, setDeliveredToken] = useState<string | null>(null);

  const isMfaStep = Boolean(mfaSessionId);
  const mfaExpirationLabel = useMemo(() => {
    if (!mfaExpiresAt) {
      return null;
    }

    const expiresDate = new Date(mfaExpiresAt);
    if (Number.isNaN(expiresDate.getTime())) {
      return null;
    }

    return expiresDate.toLocaleTimeString('es-ES', { hour: '2-digit', minute: '2-digit' });
  }, [mfaExpiresAt]);

  async function handleSubmit(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    try {
      if (isMfaStep) {
        await completeMfa(verificationCode);
        setVerificationCode('');
      } else {
        await login(usernameOrEmail, password);
      }
    } catch (err) {
      console.error('Login failed', err);
    }
  }

  async function handleResetRequest(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setResetError(null);
    setResetMessage(null);
    try {
      const response = await requestPasswordReset(resetEmail);
      setDeliveredToken(response.token);
      setResetToken(response.token);
      setResetStep('confirm');
      setResetMessage('Token generado. Úsalo a continuación para definir una nueva contraseña.');
    } catch (err) {
      console.error(err);
      setResetError('No se pudo generar el token de restablecimiento.');
    }
  }

  async function handleResetConfirm(event: FormEvent<HTMLFormElement>) {
    event.preventDefault();
    setResetError(null);
    setResetMessage(null);
    try {
      await resetPassword(resetEmail, resetToken, newPassword);
      setResetMessage('Contraseña actualizada correctamente. Ya puedes iniciar sesión.');
      setShowReset(false);
      setResetStep('request');
      setResetEmail('');
      setResetToken('');
      setNewPassword('');
      setDeliveredToken(null);
    } catch (err) {
      console.error(err);
      setResetError('No se pudo restablecer la contraseña. Verifica el token y la nueva contraseña.');
    }
  }

  if (token) {
    router.replace('/dashboard');
    return null;
  }

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-primary-50 to-slate-100 px-4 py-12">
      <div className="w-full max-w-md rounded-3xl border border-slate-200 bg-white/90 p-10 shadow-xl backdrop-blur">
        <div className="mb-8 text-center">
          <div className="mx-auto mb-4 flex h-14 w-14 items-center justify-center rounded-2xl bg-primary-100 text-primary-600">
            <span className="text-xl font-semibold">GI</span>
          </div>
          <h1 className="text-2xl font-semibold text-slate-900">Accede al gestor de inventario</h1>
          <p className="mt-2 text-sm text-slate-500">
            Controla productos, almacenes, pedidos y planificación estratégica en un único panel.
          </p>
        </div>

        <form className="flex flex-col gap-4" onSubmit={handleSubmit}>
          {isMfaStep ? (
            <>
              <p className="rounded-lg bg-primary-50 px-3 py-2 text-sm text-primary-700">
                Se requiere un código de autenticación para completar el acceso de{' '}
                <span className="font-semibold">{pendingUsername}</span>.
                {mfaExpirationLabel && (
                  <span className="block text-xs text-primary-500">Caduca a las {mfaExpirationLabel}</span>
                )}
              </p>
              <Input
                label="Código MFA"
                name="verificationCode"
                autoComplete="one-time-code"
                value={verificationCode}
                onChange={(event) => setVerificationCode(event.target.value)}
                required
              />
            </>
          ) : (
            <>
              <Input
                label="Usuario o correo"
                name="username"
                autoComplete="username"
                value={usernameOrEmail}
                onChange={(event) => setUsernameOrEmail(event.target.value)}
                required
              />
              <Input
                label="Contraseña"
                name="password"
                type="password"
                autoComplete="current-password"
                value={password}
                onChange={(event) => setPassword(event.target.value)}
                required
              />
              <button
                type="button"
                onClick={() => {
                  setShowReset((previous) => !previous);
                  setResetError(null);
                  setResetMessage(null);
                }}
                className="text-left text-xs font-medium text-primary-600 hover:underline"
              >
                ¿Olvidaste tu contraseña?
              </button>
            </>
          )}

          {error && <p className="rounded-lg bg-red-50 px-3 py-2 text-sm text-red-600">{error}</p>}

          <Button type="submit" disabled={isLoading} className="mt-2 w-full">
            {isLoading ? 'Validando…' : isMfaStep ? 'Validar código MFA' : 'Iniciar sesión'}
          </Button>
        </form>

        {!isMfaStep && showReset && (
          <div className="mt-6 rounded-2xl border border-slate-200 bg-slate-50 p-4">
            <h2 className="text-sm font-semibold text-slate-800">Recuperar acceso</h2>
            <p className="mt-1 text-xs text-slate-500">Genera un token temporal y establece una nueva contraseña.</p>

            {resetMessage && <p className="mt-2 rounded-lg bg-emerald-50 px-3 py-2 text-xs text-emerald-700">{resetMessage}</p>}
            {resetError && <p className="mt-2 rounded-lg bg-red-50 px-3 py-2 text-xs text-red-600">{resetError}</p>}

            {resetStep === 'request' ? (
              <form className="mt-3 flex flex-col gap-3" onSubmit={handleResetRequest}>
                <Input
                  label="Correo o usuario"
                  name="resetEmail"
                  value={resetEmail}
                  onChange={(event) => setResetEmail(event.target.value)}
                  autoComplete="username"
                  required
                />
                <Button type="submit" disabled={isLoading} size="sm">
                  {isLoading ? 'Generando token…' : 'Enviar token'}
                </Button>
              </form>
            ) : (
              <form className="mt-3 flex flex-col gap-3" onSubmit={handleResetConfirm}>
                <Input
                  label="Token recibido"
                  name="resetToken"
                  value={resetToken}
                  onChange={(event) => setResetToken(event.target.value)}
                  hint={deliveredToken ? `Token generado: ${deliveredToken}` : undefined}
                  required
                />
                <Input
                  label="Nueva contraseña"
                  name="newPassword"
                  type="password"
                  autoComplete="new-password"
                  value={newPassword}
                  onChange={(event) => setNewPassword(event.target.value)}
                  required
                />
                <div className="flex gap-2">
                  <Button type="submit" disabled={isLoading} size="sm" className="flex-1">
                    {isLoading ? 'Actualizando…' : 'Restablecer'}
                  </Button>
                  <Button
                    type="button"
                    variant="ghost"
                    size="sm"
                    className="flex-1"
                    onClick={() => {
                      setResetStep('request');
                      setResetToken('');
                      setNewPassword('');
                      setDeliveredToken(null);
                    }}
                  >
                    Volver atrás
                  </Button>
                </div>
              </form>
            )}
          </div>
        )}

        <p className="mt-6 text-center text-xs text-slate-500">
          ¿Necesitas una cuenta? Solicita acceso a un administrador desde la consola de seguridad.
        </p>
        <p className="mt-2 text-center text-xs text-slate-400">
          Revisa la documentación funcional en{' '}
          <Link href="https://github.com/your-org/gestor-inventario" className="text-primary-600 underline">
            docs/documentacion.md
          </Link>
        </p>
      </div>
    </div>
  );
}
