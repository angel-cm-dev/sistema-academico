import { render, screen, waitFor } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import { MemoryRouter, Route, Routes } from 'react-router-dom';
import { afterEach, describe, expect, it, vi } from 'vitest';
import { AuthProvider } from '@/auth/AuthContext';
import LoginPage from './LoginPage';

function renderLogin() {
  return render(
    <MemoryRouter initialEntries={['/login']} future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
      <AuthProvider>
        <Routes>
          <Route path="/login" element={<LoginPage />} />
          <Route path="/" element={<p>Inicio</p>} />
        </Routes>
      </AuthProvider>
    </MemoryRouter>,
  );
}

describe('LoginPage', () => {
  afterEach(() => vi.restoreAllMocks());

  it('valida los campos antes de llamar a la API', async () => {
    const fetchMock = vi.spyOn(globalThis, 'fetch');
    renderLogin();

    await userEvent.type(screen.getByLabelText('Correo electrónico'), 'no-es-correo');
    await userEvent.click(screen.getByRole('button', { name: 'Ingresar' }));

    expect(await screen.findByText('Correo no válido.')).toBeInTheDocument();
    expect(screen.getByText('Ingrese su contraseña.')).toBeInTheDocument();
    expect(fetchMock).not.toHaveBeenCalled();
  });

  it('muestra el error del backend con credenciales incorrectas', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(JSON.stringify({ title: 'No autenticado', detail: 'Correo o contraseña incorrectos.' }), { status: 401 }),
    );
    renderLogin();

    await userEvent.type(screen.getByLabelText('Correo electrónico'), 'admin@demo.pe');
    await userEvent.type(screen.getByLabelText('Contraseña'), 'mala');
    await userEvent.click(screen.getByRole('button', { name: 'Ingresar' }));

    expect(await screen.findByRole('alert')).toHaveTextContent('Correo o contraseña incorrectos.');
  });

  it('inicia sesión, guarda el token y redirige', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      new Response(
        JSON.stringify({
          token: 'jwt-demo',
          expiraEn: '2026-12-31T00:00:00Z',
          usuario: { id: 1, email: 'admin@demo.pe', nombreCompleto: 'Admin', rol: 'Admin', docenteId: null, apoderadoId: null },
        }),
        { status: 200 },
      ),
    );
    renderLogin();

    await userEvent.click(screen.getByRole('button', { name: 'Administración' }));
    await userEvent.click(screen.getByRole('button', { name: 'Ingresar' }));

    await waitFor(() => expect(screen.getByText('Inicio')).toBeInTheDocument());
    expect(localStorage.getItem('sa.token')).toBe('jwt-demo');
  });
});
