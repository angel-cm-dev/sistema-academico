import { api } from './client';
import type {
  Alumno,
  Apoderado,
  Asignacion,
  Cuota,
  Curso,
  Dashboard,
  EstadoAsistencia,
  EstadoCuota,
  EstadoMatricula,
  GuardarAlumno,
  Libreta,
  LoginResponse,
  Matricula,
  MatriculaCreada,
  MedioPago,
  PagedResult,
  PageParams,
  PagoRegistrado,
  Periodo,
  RegistroAsistencia,
  RegistroNotas,
  Seccion,
  Usuario,
} from './types';

/** Funciones tipadas por recurso: los componentes nunca arman URLs a mano. */
export const authApi = {
  login: (email: string, password: string) =>
    api<LoginResponse>('/api/auth/login', { method: 'POST', body: { email, password } }),
  me: () => api<Usuario>('/api/auth/me'),
};

export const dashboardApi = {
  obtener: () => api<Dashboard>('/api/dashboard'),
};

export const estructuraApi = {
  periodos: () => api<Periodo[]>('/api/periodos'),
  secciones: (periodoId?: number) => api<Seccion[]>('/api/secciones', { query: { periodoId } }),
  cursos: () => api<Curso[]>('/api/cursos'),
  actualizarCompetencias: (cursoId: number, competencias: { id: number | null; nombre: string; peso: number }[]) =>
    api<Curso>(`/api/cursos/${cursoId}/competencias`, { method: 'PUT', body: { competencias } }),
  asignaciones: (periodoId?: number) => api<Asignacion[]>('/api/asignaciones', { query: { periodoId } }),
};

export const alumnosApi = {
  listar: (p: PageParams & { activo?: boolean; seccionId?: number }) =>
    api<PagedResult<Alumno>>('/api/alumnos', { query: { ...p } }),
  crear: (data: GuardarAlumno) => api<Alumno>('/api/alumnos', { method: 'POST', body: data }),
  actualizar: (id: number, data: GuardarAlumno) => api<Alumno>(`/api/alumnos/${id}`, { method: 'PUT', body: data }),
  desactivar: (id: number) => api<void>(`/api/alumnos/${id}`, { method: 'DELETE' }),
};

export const apoderadosApi = {
  listar: (p: PageParams) => api<PagedResult<Apoderado>>('/api/apoderados', { query: { ...p } }),
};

export const matriculasApi = {
  listar: (p: PageParams & { periodoId?: number; seccionId?: number; estado?: EstadoMatricula }) =>
    api<PagedResult<Matricula>>('/api/matriculas', { query: { ...p } }),
  crear: (alumnoId: number, seccionId: number) =>
    api<MatriculaCreada>('/api/matriculas', { method: 'POST', body: { alumnoId, seccionId } }),
  retirar: (id: number) => api<Matricula>(`/api/matriculas/${id}/retiro`, { method: 'POST' }),
};

export const notasApi = {
  registro: (asignacionId: number, bimestre: number) =>
    api<RegistroNotas>('/api/notas', { query: { asignacionId, bimestre } }),
  guardar: (
    asignacionId: number,
    bimestre: number,
    notas: { matriculaId: number; competenciaId: number; valor: number | null }[],
  ) => api<RegistroNotas>('/api/notas', { method: 'PUT', body: { asignacionId, bimestre, notas } }),
};

export const asistenciaApi = {
  registro: (seccionId: number, fecha: string) =>
    api<RegistroAsistencia>('/api/asistencias', { query: { seccionId, fecha } }),
  guardar: (
    seccionId: number,
    fecha: string,
    registros: { matriculaId: number; estado: EstadoAsistencia; observacion: string | null }[],
  ) => api<RegistroAsistencia>('/api/asistencias', { method: 'PUT', body: { seccionId, fecha, registros } }),
};

export const pensionesApi = {
  listar: (p: PageParams & { matriculaId?: number; periodoId?: number; estado?: EstadoCuota }) =>
    api<PagedResult<Cuota>>('/api/pensiones', { query: { ...p } }),
  pagar: (id: number, medioPago: MedioPago, numeroOperacion: string | null, fechaPago: string | null) =>
    api<PagoRegistrado>(`/api/pensiones/${id}/pago`, { method: 'POST', body: { medioPago, numeroOperacion, fechaPago } }),
};

export const libretasApi = {
  obtener: (matriculaId: number) => api<Libreta>(`/api/libretas/${matriculaId}`),
};
