import { lazy, Suspense } from 'react';
import { Link, Route, Routes } from 'react-router-dom';
import { RequireAuth } from './auth/RequireAuth';
import { AppLayout } from './components/layout/AppLayout';
import { PageLoader } from './components/ui/Feedback';
import LoginPage from './pages/LoginPage';

// Code splitting por página: el login carga rápido y el resto se descarga bajo demanda.
const DashboardPage = lazy(() => import('./pages/DashboardPage'));
const AlumnosPage = lazy(() => import('./pages/AlumnosPage'));
const MatriculasPage = lazy(() => import('./pages/MatriculasPage'));
const SeccionesPage = lazy(() => import('./pages/SeccionesPage'));
const NotasPage = lazy(() => import('./pages/NotasPage'));
const AsistenciaPage = lazy(() => import('./pages/AsistenciaPage'));
const PensionesPage = lazy(() => import('./pages/PensionesPage'));
const LibretasPage = lazy(() => import('./pages/LibretasPage'));

function Mensaje({ codigo, texto }: { codigo: string; texto: string }) {
  return (
    <div className="flex min-h-[50vh] flex-col items-center justify-center gap-2 text-center">
      <p className="text-5xl font-bold text-brand-600">{codigo}</p>
      <p className="text-slate-600 dark:text-slate-300">{texto}</p>
      <Link to="/" className="text-sm font-medium text-brand-600 hover:underline">
        Volver al inicio
      </Link>
    </div>
  );
}

export default function App() {
  return (
    <Suspense fallback={<PageLoader />}>
      <Routes>
        <Route path="/login" element={<LoginPage />} />
        <Route
          element={
            <RequireAuth>
              <AppLayout />
            </RequireAuth>
          }
        >
          <Route index element={<DashboardPage />} />
          <Route path="alumnos" element={<AlumnosPage />} />
          <Route path="matriculas" element={<RequireAuth roles={['Admin']}><MatriculasPage /></RequireAuth>} />
          <Route path="secciones" element={<RequireAuth roles={['Admin']}><SeccionesPage /></RequireAuth>} />
          <Route path="notas" element={<RequireAuth roles={['Admin', 'Docente']}><NotasPage /></RequireAuth>} />
          <Route path="asistencia" element={<RequireAuth roles={['Admin', 'Docente']}><AsistenciaPage /></RequireAuth>} />
          <Route path="pensiones" element={<RequireAuth roles={['Admin', 'Apoderado']}><PensionesPage /></RequireAuth>} />
          <Route path="libretas" element={<LibretasPage />} />
          <Route path="libretas/:matriculaId" element={<LibretasPage />} />
          <Route path="403" element={<Mensaje codigo="403" texto="No tiene permiso para ver esta página." />} />
          <Route path="*" element={<Mensaje codigo="404" texto="La página que busca no existe." />} />
        </Route>
      </Routes>
    </Suspense>
  );
}
