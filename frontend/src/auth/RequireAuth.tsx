import type { ReactNode } from 'react';
import { Navigate, useLocation } from 'react-router-dom';
import type { Rol } from '@/api/types';
import { PageLoader } from '@/components/ui/Feedback';
import { useAuth } from './AuthContext';

/** Protege rutas: sin sesión → /login; sin rol permitido → /403. */
export function RequireAuth({ roles, children }: { roles?: Rol[]; children: ReactNode }) {
  const { usuario, cargando } = useAuth();
  const location = useLocation();

  if (cargando) return <PageLoader />;
  if (!usuario) return <Navigate to="/login" replace state={{ from: location.pathname }} />;
  if (roles && !roles.includes(usuario.rol)) return <Navigate to="/403" replace />;
  return <>{children}</>;
}
