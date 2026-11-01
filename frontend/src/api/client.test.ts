import { afterEach, describe, expect, it, vi } from 'vitest';
import { api, ApiError, buildQuery, setUnauthorizedHandler, tokenStorage } from './client';

function jsonResponse(status: number, body: unknown) {
  return new Response(JSON.stringify(body), { status, headers: { 'Content-Type': 'application/json' } });
}

/** Ejecuta una llamada que debe fallar y devuelve el ApiError lanzado. */
async function capturarError(promesa: Promise<unknown>): Promise<ApiError> {
  try {
    await promesa;
  } catch (e) {
    return e as ApiError;
  }
  throw new Error('Se esperaba que la llamada fallara');
}

describe('buildQuery', () => {
  it('omite valores vacíos', () => {
    expect(buildQuery({ page: 2, search: '', estado: undefined, activo: false })).toBe('?page=2&activo=false');
    expect(buildQuery({})).toBe('');
  });
});

describe('api', () => {
  afterEach(() => {
    vi.restoreAllMocks();
    setUnauthorizedHandler(null);
  });

  it('envía el token Bearer guardado', async () => {
    tokenStorage.set('abc');
    const fetchMock = vi.spyOn(globalThis, 'fetch').mockResolvedValue(jsonResponse(200, { ok: true }));

    await api('/api/dashboard');

    const headers = fetchMock.mock.calls[0][1]?.headers as Record<string, string>;
    expect(headers.Authorization).toBe('Bearer abc');
  });

  it('convierte ProblemDetails en ApiError con errores por campo', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(
      jsonResponse(400, { title: 'Datos inválidos', detail: 'Revise los campos', errors: { dni: ['El DNI debe tener 8 dígitos.'] } }),
    );

    const error = await capturarError(api('/api/alumnos', { method: 'POST', body: {} }));

    expect(error).toBeInstanceOf(ApiError);
    expect(error.status).toBe(400);
    expect(error.message).toBe('Revise los campos');
    expect(error.fieldError('dni')).toBe('El DNI debe tener 8 dígitos.');
  });

  it('usa un mensaje amigable cuando la respuesta no trae cuerpo', async () => {
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 403 }));
    const error = await capturarError(api('/api/usuarios'));
    expect(error.message).toMatch(/permiso/);
  });

  it('notifica 401 para cerrar la sesión', async () => {
    tokenStorage.set('vencido');
    const handler = vi.fn();
    setUnauthorizedHandler(handler);
    vi.spyOn(globalThis, 'fetch').mockResolvedValue(new Response(null, { status: 401 }));

    await expect(api('/api/dashboard')).rejects.toBeInstanceOf(ApiError);
    expect(handler).toHaveBeenCalledOnce();
  });

  it('traduce fallos de red a ApiError con status 0', async () => {
    vi.spyOn(globalThis, 'fetch').mockRejectedValue(new TypeError('Failed to fetch'));
    const error = await capturarError(api('/api/dashboard'));
    expect(error.status).toBe(0);
  });
});
