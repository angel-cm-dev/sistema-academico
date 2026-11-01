import { useState } from 'react';
import { NavLink, Outlet } from 'react-router-dom';
import {
  BookOpenCheck,
  CalendarCheck,
  ClipboardList,
  GraduationCap,
  LayoutDashboard,
  LogOut,
  Menu,
  Moon,
  NotebookText,
  School,
  Sun,
  Users,
  Wallet,
  X,
} from 'lucide-react';
import type { Rol } from '@/api/types';
import { useAuth } from '@/auth/AuthContext';
import { useTheme } from '@/hooks/useTheme';
import { cn } from '@/lib/cn';

interface NavItem {
  to: string;
  label: string;
  icon: typeof LayoutDashboard;
  roles: Rol[];
}

// El menú se adapta al rol. Es solo comodidad visual: el backend vuelve a validar todo.
const NAV: NavItem[] = [
  { to: '/', label: 'Dashboard', icon: LayoutDashboard, roles: ['Admin', 'Docente', 'Apoderado'] },
  { to: '/alumnos', label: 'Alumnos', icon: Users, roles: ['Admin', 'Docente', 'Apoderado'] },
  { to: '/matriculas', label: 'Matrículas', icon: ClipboardList, roles: ['Admin'] },
  { to: '/secciones', label: 'Secciones y cursos', icon: School, roles: ['Admin'] },
  { to: '/notas', label: 'Registro de notas', icon: BookOpenCheck, roles: ['Admin', 'Docente'] },
  { to: '/asistencia', label: 'Asistencia', icon: CalendarCheck, roles: ['Admin', 'Docente'] },
  { to: '/pensiones', label: 'Pensiones', icon: Wallet, roles: ['Admin', 'Apoderado'] },
  { to: '/libretas', label: 'Libretas', icon: NotebookText, roles: ['Admin', 'Docente', 'Apoderado'] },
];

export function AppLayout() {
  const { usuario, logout } = useAuth();
  const { tema, alternar } = useTheme();
  const [abierto, setAbierto] = useState(false);
  const items = NAV.filter((n) => usuario && n.roles.includes(usuario.rol));

  const sidebar = (
    <nav className="flex h-full flex-col gap-1 p-4" aria-label="Menú principal">
      <div className="mb-6 flex items-center gap-2 px-2">
        <span className="flex h-9 w-9 items-center justify-center rounded-lg bg-brand-600 text-white">
          <GraduationCap className="h-5 w-5" />
        </span>
        <div>
          <p className="text-sm font-semibold leading-tight">Sistema Académico</p>
          <p className="text-xs text-slate-500">I.E. Demo · Lima</p>
        </div>
      </div>
      {items.map((item) => (
        <NavLink
          key={item.to}
          to={item.to}
          end={item.to === '/'}
          onClick={() => setAbierto(false)}
          className={({ isActive }) =>
            cn(
              'flex items-center gap-3 rounded-lg px-3 py-2 text-sm font-medium transition',
              isActive
                ? 'bg-brand-50 text-brand-700 dark:bg-brand-900/40 dark:text-brand-200'
                : 'text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800',
            )
          }
        >
          <item.icon className="h-4 w-4" aria-hidden />
          {item.label}
        </NavLink>
      ))}
    </nav>
  );

  return (
    <div className="flex min-h-screen">
      {/* Sidebar fijo en escritorio */}
      <aside className="hidden w-64 shrink-0 border-r border-slate-200 bg-white dark:border-slate-800 dark:bg-slate-900 lg:block print:hidden">
        {sidebar}
      </aside>

      {/* Drawer en móvil */}
      {abierto && (
        <div className="fixed inset-0 z-40 lg:hidden print:hidden">
          <div className="absolute inset-0 bg-slate-900/50" onClick={() => setAbierto(false)} />
          <aside className="relative h-full w-64 bg-white dark:bg-slate-900">
            <button
              className="absolute right-3 top-3 rounded p-1 text-slate-500"
              onClick={() => setAbierto(false)}
              aria-label="Cerrar menú"
            >
              <X className="h-5 w-5" />
            </button>
            {sidebar}
          </aside>
        </div>
      )}

      <div className="flex min-w-0 flex-1 flex-col">
        <header className="sticky top-0 z-30 flex h-16 items-center justify-between gap-3 border-b border-slate-200 bg-white/80 px-4 backdrop-blur dark:border-slate-800 dark:bg-slate-900/80 lg:px-8 print:hidden">
          <button className="rounded p-2 text-slate-600 lg:hidden" onClick={() => setAbierto(true)} aria-label="Abrir menú">
            <Menu className="h-5 w-5" />
          </button>
          <div className="flex-1" />
          <button
            onClick={alternar}
            className="rounded-lg p-2 text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
            aria-label={tema === 'oscuro' ? 'Activar modo claro' : 'Activar modo oscuro'}
            title="Cambiar tema"
          >
            {tema === 'oscuro' ? <Sun className="h-5 w-5" /> : <Moon className="h-5 w-5" />}
          </button>
          <div className="hidden text-right sm:block">
            <p className="text-sm font-medium">{usuario?.nombreCompleto}</p>
            <p className="text-xs text-slate-500">{usuario?.rol}</p>
          </div>
          <button
            onClick={logout}
            className="inline-flex items-center gap-2 rounded-lg px-3 py-2 text-sm text-slate-600 hover:bg-slate-100 dark:text-slate-300 dark:hover:bg-slate-800"
          >
            <LogOut className="h-4 w-4" /> <span className="hidden sm:inline">Salir</span>
          </button>
        </header>
        <main className="mx-auto w-full max-w-7xl flex-1 px-4 py-6 lg:px-8 print:max-w-none print:p-0">
          <Outlet />
        </main>
      </div>
    </div>
  );
}
