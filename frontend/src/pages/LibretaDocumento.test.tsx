import { render, screen } from '@testing-library/react';
import { MemoryRouter } from 'react-router-dom';
import { describe, expect, it } from 'vitest';
import type { Libreta } from '@/api/types';
import { LibretaDocumento } from './LibretasPage';

const libreta: Libreta = {
  matriculaId: 1,
  codigoAlumno: 'ALU-000001',
  alumno: 'Mendoza Flores, Diego Alonso',
  dni: '70000001',
  apoderado: 'Mendoza Vargas, Carlos Alberto',
  grado: '1° Secundaria',
  seccion: 'A',
  nivel: 'Secundaria',
  anio: 2026,
  cursos: [
    {
      cursoId: 1,
      curso: 'Matemática',
      docente: 'Quispe Huamán, Rosa Elena',
      competencias: [{ id: 1, nombre: 'Resuelve problemas de cantidad', peso: 2, notas: [14, 9, null, null] }],
      promediosBimestrales: [14, 9, null, null],
      promedioFinal: 12,
      literal: 'B',
      aprobado: true,
    },
  ],
  asistencia: { matriculaId: 1, presentes: 8, tardanzas: 1, faltas: 1, faltasJustificadas: 0, total: 10, porcentaje: 90, enRiesgo: false },
  promedioGeneral: 12,
  cursosAprobados: 1,
  cursosDesaprobados: 0,
  situacion: 'En curso',
  fechaEmision: '2026-10-15',
};

describe('LibretaDocumento', () => {
  it('muestra datos del alumno, promedios y situación', () => {
    render(
      <MemoryRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
        <LibretaDocumento libreta={libreta} />
      </MemoryRouter>,
    );

    expect(screen.getByRole('heading', { name: 'Libreta de notas 2026' })).toBeInTheDocument();
    expect(screen.getByText('Mendoza Flores, Diego Alonso')).toBeInTheDocument();
    expect(screen.getByText('Matemática')).toBeInTheDocument();
    expect(screen.getByText('(×2)')).toBeInTheDocument();
    expect(screen.getByText('90.0 %')).toBeInTheDocument();
    expect(screen.getByText('En curso')).toBeInTheDocument();
    expect(screen.getByText('Emitido el 15/10/2026')).toBeInTheDocument();
  });

  it('resalta en rojo el bimestre desaprobado', () => {
    render(
      <MemoryRouter future={{ v7_startTransition: true, v7_relativeSplatPath: true }}>
        <LibretaDocumento libreta={libreta} />
      </MemoryRouter>,
    );
    const celdas = screen.getAllByText('9');
    expect(celdas.some((c) => c.className.includes('text-red'))).toBe(true);
  });
});
