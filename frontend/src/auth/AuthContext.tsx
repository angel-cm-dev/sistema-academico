import { createContext, useCallback, useContext, useEffect, useMemo, useState, type ReactNode } from 'react';
import { setUnauthorizedHandler, tokenStorage } from '@/api/client';
import { authApi } from '@/api/endpoints';
import type { Rol, Usuario } from '@/api/types';

interface AuthContextValue {
  usuario: Usuario | null;
  cargando: boolean;
  login: (email: string, password: string) => Promise<void>;
  logout: () => void;
  tieneRol: (...roles: Rol[]) => boolean;
}

const AuthContext = createContext<AuthContextValue | null>(null);

/**
 * Mantiene la sesión. Al iniciar, si hay token guardado se valida contra /api/auth/me:
 * nunca confiamos en datos del usuario guardados en el navegador (podrían manipularse).
 * Igual, la UI solo oculta opciones; quien autoriza de verdad es el backend.
 */
export function AuthProvider({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<Usuario | null>(null);
  const [cargando, setCargando] = useState(true);

  const logout = useCallback(() => {
    tokenStorage.clear();
    setUsuario(null);
  }, []);

  useEffect(() => {
    setUnauthorizedHandler(logout);
    if (!tokenStorage.get()) {
      setCargando(false);
      return;
    }
    authApi
      .me()
      .then(setUsuario)
      .catch(() => tokenStorage.clear())
      .finally(() => setCargando(false));
    return () => setUnauthorizedHandler(null);
  }, [logout]);

  const login = useCallback(async (email: string, password: string) => {
    const resp = await authApi.login(email, password);
    tokenStorage.set(resp.token);
    setUsuario(resp.usuario);
  }, []);

  const tieneRol = useCallback((...roles: Rol[]) => !!usuario && roles.includes(usuario.rol), [usuario]);

  const value = useMemo(() => ({ usuario, cargando, login, logout, tieneRol }), [usuario, cargando, login, logout, tieneRol]);
  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
}

// eslint-disable-next-line react-refresh/only-export-components
export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext);
  if (!ctx) throw new Error('useAuth debe usarse dentro de <AuthProvider>');
  return ctx;
}
