// Tipos que reflejan los DTOs del backend (JSON en camelCase, enums como texto).

export type Rol = 'Admin' | 'Docente' | 'Apoderado';

export interface Usuario {
  id: number;
  email: string;
  nombreCompleto: string;
  rol: Rol;
  docenteId: number | null;
  apoderadoId: number | null;
}

export interface LoginResponse {
  token: string;
  expiraEn: string;
  usuario: Usuario;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

export interface PageParams {
  page?: number;
  pageSize?: number;
  search?: string;
  sortBy?: string;
  sortDir?: 'asc' | 'desc';
}

export interface Kpi {
  clave: string;
  titulo: string;
  valor: number;
  formato: 'numero' | 'moneda' | 'porcentaje';
  detalle: string | null;
}

export interface OcupacionSeccion {
  seccionId: number;
  seccion: string;
  capacidad: number;
  matriculados: number;
  ocupacion: number;
}

export interface Dashboard {
  rol: Rol;
  periodoId: number | null;
  anio: number | null;
  kpis: Kpi[];
  secciones: OcupacionSeccion[];
}

export interface Periodo {
  id: number;
  anio: number;
  nombre: string;
  fechaInicio: string;
  fechaFin: string;
  pensionMensual: number;
  activo: boolean;
}

export interface Seccion {
  id: number;
  gradoId: number;
  grado: string;
  letra: string;
  periodoId: number;
  anio: number;
  capacidad: number;
  matriculados: number;
  vacantes: number;
  ocupacion: number;
}

export interface Competencia {
  id: number;
  nombre: string;
  peso: number;
  orden: number;
}

export interface Curso {
  id: number;
  codigo: string;
  nombre: string;
  gradoId: number;
  grado: string;
  competencias: Competencia[];
}

export interface Asignacion {
  id: number;
  docenteId: number;
  docente: string;
  cursoId: number;
  curso: string;
  seccionId: number;
  seccion: string;
  periodoId: number;
}

export interface Alumno {
  id: number;
  codigo: string;
  dni: string;
  nombres: string;
  apellidos: string;
  fechaNacimiento: string;
  apoderadoId: number;
  apoderado: string;
  activo: boolean;
  seccionActual: string | null;
}

export interface GuardarAlumno {
  dni: string;
  nombres: string;
  apellidos: string;
  fechaNacimiento: string;
  apoderadoId: number;
}

export interface Apoderado {
  id: number;
  dni: string;
  nombres: string;
  apellidos: string;
  telefono: string | null;
  email: string | null;
  hijos: number;
}

export type EstadoMatricula = 'Activa' | 'Retirada';

export interface Matricula {
  id: number;
  alumnoId: number;
  alumno: string;
  codigoAlumno: string;
  seccionId: number;
  seccion: string;
  periodoId: number;
  anio: number;
  fechaMatricula: string;
  estado: EstadoMatricula;
  fechaRetiro: string | null;
}

export interface MatriculaCreada {
  matricula: Matricula;
  cuotasGeneradas: number;
  totalCronograma: number;
}

export interface NotaCelda {
  competenciaId: number;
  valor: number | null;
}

export interface FilaNotas {
  matriculaId: number;
  alumno: string;
  codigoAlumno: string;
  notas: NotaCelda[];
  promedioExacto: number | null;
  promedio: number | null;
  literal: string | null;
  aprobado: boolean | null;
}

export interface RegistroNotas {
  asignacionId: number;
  curso: string;
  seccion: string;
  docente: string;
  bimestre: number;
  competencias: Competencia[];
  filas: FilaNotas[];
}

export type EstadoAsistencia = 'Presente' | 'Tarde' | 'Falta' | 'FaltaJustificada';

export interface FilaAsistencia {
  matriculaId: number;
  alumno: string;
  codigoAlumno: string;
  estado: EstadoAsistencia | null;
  observacion: string | null;
}

export interface RegistroAsistencia {
  seccionId: number;
  seccion: string;
  fecha: string;
  filas: FilaAsistencia[];
}

export interface ResumenAsistencia {
  matriculaId: number;
  presentes: number;
  tardanzas: number;
  faltas: number;
  faltasJustificadas: number;
  total: number;
  porcentaje: number;
  enRiesgo: boolean;
}

export type EstadoCuota = 'Pendiente' | 'Vencida' | 'Pagada';
export type MedioPago = 'Efectivo' | 'Transferencia' | 'Tarjeta' | 'Yape';

export interface Cuota {
  id: number;
  matriculaId: number;
  alumno: string;
  seccion: string;
  numero: number;
  concepto: string;
  monto: number;
  fechaVencimiento: string;
  estado: EstadoCuota;
  diasAtraso: number;
  mora: number;
  totalAPagar: number;
  fechaPago: string | null;
  medioPago: MedioPago | null;
  numeroOperacion: string | null;
}

export interface PagoRegistrado {
  cuota: Cuota;
  montoCobrado: number;
}

export interface CompetenciaLibreta {
  id: number;
  nombre: string;
  peso: number;
  notas: (number | null)[];
}

export interface CursoLibreta {
  cursoId: number;
  curso: string;
  docente: string | null;
  competencias: CompetenciaLibreta[];
  promediosBimestrales: (number | null)[];
  promedioFinal: number | null;
  literal: string | null;
  aprobado: boolean | null;
}

export interface Libreta {
  matriculaId: number;
  codigoAlumno: string;
  alumno: string;
  dni: string;
  apoderado: string;
  grado: string;
  seccion: string;
  nivel: string;
  anio: number;
  cursos: CursoLibreta[];
  asistencia: ResumenAsistencia;
  promedioGeneral: number | null;
  cursosAprobados: number;
  cursosDesaprobados: number;
  situacion: string;
  fechaEmision: string;
}
