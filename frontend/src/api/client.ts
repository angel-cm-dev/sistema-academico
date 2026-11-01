/**
 * Cliente HTTP mínimo sobre fetch:
 * - agrega el token JWT en cada petición,
 * - convierte las respuestas ProblemDetails (RFC 9457) del backend en ApiError,
 * - ante un 401 notifica a la app para cerrar sesión (token vencido o inválido).
 */

const TOKEN_KEY = 'sa.token';
const BASE_URL = import.meta.env.VITE_API_URL ?? '';

export class ApiError extends Error {
  readonly status: number;
  readonly title: string;
  readonly errors: Record<string, string[]>;

  constructor(status: number, title: string, detail?: string, errors: Record<string, string[]> = {}) {
    super(detail || title);
    this.name = 'ApiError';
    this.status = status;
    this.title = title;
    this.errors = errors;
  }

  /** Primer mensaje de validación de un campo (para mostrarlo bajo el input). */
  fieldError(field: string): string | undefined {
    return this.errors[field]?.[0];
  }
}

export const tokenStorage = {
  get: (): string | null => {
    try {
      return localStorage.getItem(TOKEN_KEY);
    } catch {
      return null;
    }
  },
  set: (token: string) => localStorage.setItem(TOKEN_KEY, token),
  clear: () => localStorage.removeItem(TOKEN_KEY),
};

let unauthorizedHandler: (() => void) | null = null;
export function setUnauthorizedHandler(handler: (() => void) | null) {
  unauthorizedHandler = handler;
}

type QueryValue = string | number | boolean | null | undefined;

/** Construye "?a=1&b=x" omitiendo valores vacíos, para no enviar filtros sin usar. */
export function buildQuery(params?: Record<string, QueryValue>): string {
  if (!params) return '';
  const qs = new URLSearchParams();
  for (const [key, value] of Object.entries(params)) {
    if (value === undefined || value === null || value === '') continue;
    qs.append(key, String(value));
  }
  const s = qs.toString();
  return s ? `?${s}` : '';
}

const MENSAJES_POR_ESTADO: Record<number, string> = {
  0: 'No se pudo conectar con el servidor. Verifique su conexión.',
  401: 'Su sesión expiró. Vuelva a iniciar sesión.',
  403: 'No tiene permiso para realizar esta acción.',
  404: 'El recurso solicitado no existe.',
  429: 'Demasiados intentos. Espere un momento e intente de nuevo.',
  500: 'Ocurrió un error inesperado en el servidor.',
};

export async function parseError(resp: Response): Promise<ApiError> {
  let body: { title?: string; detail?: string; errors?: Record<string, string[]> } = {};
  try {
    body = await resp.json();
  } catch {
    // respuesta sin cuerpo JSON (p. ej. 403 del middleware): usamos el mensaje por estado
  }
  const fallback = MENSAJES_POR_ESTADO[resp.status] ?? `Error ${resp.status}`;
  return new ApiError(resp.status, body.title ?? fallback, body.detail ?? fallback, body.errors ?? {});
}

interface RequestOptions {
  method?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  body?: unknown;
  query?: Record<string, QueryValue>;
  signal?: AbortSignal;
}

export async function api<T>(path: string, options: RequestOptions = {}): Promise<T> {
  const headers: Record<string, string> = { Accept: 'application/json' };
  const token = tokenStorage.get();
  if (token) headers.Authorization = `Bearer ${token}`;
  if (options.body !== undefined) headers['Content-Type'] = 'application/json';

  let resp: Response;
  try {
    resp = await fetch(`${BASE_URL}${path}${buildQuery(options.query)}`, {
      method: options.method ?? 'GET',
      headers,
      body: options.body !== undefined ? JSON.stringify(options.body) : undefined,
      signal: options.signal,
    });
  } catch (e) {
    if (e instanceof DOMException && e.name === 'AbortError') throw e;
    throw new ApiError(0, 'Sin conexión', MENSAJES_POR_ESTADO[0]);
  }

  if (resp.status === 401 && token) unauthorizedHandler?.();
  if (!resp.ok) throw await parseError(resp);
  if (resp.status === 204) return undefined as T;
  return (await resp.json()) as T;
}
