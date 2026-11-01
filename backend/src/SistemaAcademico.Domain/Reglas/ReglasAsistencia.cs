namespace SistemaAcademico.Domain.Reglas;

public sealed record ResumenAsistencia(int Presentes, int Tardanzas, int Faltas, int FaltasJustificadas)
{
    public int Total => Presentes + Tardanzas + Faltas + FaltasJustificadas;

    /// <summary>
    /// Porcentaje de asistencia efectiva. La tardanza cuenta como asistencia (el alumno estuvo
    /// en clase) y la falta justificada se excluye del denominador: no debe perjudicar al alumno
    /// que presentó un certificado médico. Sin registros se considera 100 %.
    /// </summary>
    public decimal Porcentaje
    {
        get
        {
            var computables = Presentes + Tardanzas + Faltas;
            return computables == 0 ? 100m
                : Math.Round((Presentes + Tardanzas) * 100m / computables, 1, MidpointRounding.AwayFromZero);
        }
    }

    /// <summary>
    /// Umbral de alerta temprana: con menos de 70 % de asistencia el tutor debe contactar
    /// a la familia (valor institucional, ajustable).
    /// </summary>
    public bool EnRiesgo => Total > 0 && Porcentaje < 70m;

    public static ResumenAsistencia Desde(IEnumerable<EstadoAsistencia> estados)
    {
        var lista = estados.ToList();
        return new ResumenAsistencia(
            lista.Count(e => e == EstadoAsistencia.Presente),
            lista.Count(e => e == EstadoAsistencia.Tarde),
            lista.Count(e => e == EstadoAsistencia.Falta),
            lista.Count(e => e == EstadoAsistencia.FaltaJustificada));
    }
}
