using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace SistemaAcademico.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class Inicial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "apoderados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Dni = table.Column<string>(type: "character(8)", fixedLength: true, maxLength: 8, nullable: false),
                    Nombres = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Telefono = table.Column<string>(type: "character varying(9)", maxLength: 9, nullable: true),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_apoderados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "docentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Dni = table.Column<string>(type: "character(8)", fixedLength: true, maxLength: 8, nullable: false),
                    Nombres = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Especialidad = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_docentes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "grados",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    Nivel = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_grados", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "periodos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Anio = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(60)", maxLength: 60, nullable: false),
                    FechaInicio = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaFin = table.Column<DateOnly>(type: "date", nullable: false),
                    PensionMensual = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_periodos", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "alumnos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Dni = table.Column<string>(type: "character(8)", fixedLength: true, maxLength: 8, nullable: false),
                    Nombres = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Apellidos = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    FechaNacimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    ApoderadoId = table.Column<int>(type: "integer", nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alumnos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alumnos_apoderados_ApoderadoId",
                        column: x => x.ApoderadoId,
                        principalTable: "apoderados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "usuarios",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Email = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    PasswordHash = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: false),
                    NombreCompleto = table.Column<string>(type: "character varying(150)", maxLength: 150, nullable: false),
                    Rol = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Activo = table.Column<bool>(type: "boolean", nullable: false),
                    CreadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    DocenteId = table.Column<int>(type: "integer", nullable: true),
                    ApoderadoId = table.Column<int>(type: "integer", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_usuarios", x => x.Id);
                    table.ForeignKey(
                        name: "FK_usuarios_apoderados_ApoderadoId",
                        column: x => x.ApoderadoId,
                        principalTable: "apoderados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_usuarios_docentes_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "docentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "cursos",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    Codigo = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Nombre = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    GradoId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cursos", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cursos_grados_GradoId",
                        column: x => x.GradoId,
                        principalTable: "grados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "secciones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    GradoId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoId = table.Column<int>(type: "integer", nullable: false),
                    Letra = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    Capacidad = table.Column<int>(type: "integer", nullable: false),
                    Matriculados = table.Column<int>(type: "integer", nullable: false),
                    Version = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_secciones", x => x.Id);
                    table.CheckConstraint("ck_secciones_cupo", "\"Matriculados\" <= \"Capacidad\"");
                    table.ForeignKey(
                        name: "FK_secciones_grados_GradoId",
                        column: x => x.GradoId,
                        principalTable: "grados",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_secciones_periodos_PeriodoId",
                        column: x => x.PeriodoId,
                        principalTable: "periodos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "competencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CursoId = table.Column<int>(type: "integer", nullable: false),
                    Nombre = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Peso = table.Column<decimal>(type: "numeric(5,2)", precision: 5, scale: 2, nullable: false),
                    Orden = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_competencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_competencias_cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "asignaciones_docentes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    DocenteId = table.Column<int>(type: "integer", nullable: false),
                    CursoId = table.Column<int>(type: "integer", nullable: false),
                    SeccionId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asignaciones_docentes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_asignaciones_docentes_cursos_CursoId",
                        column: x => x.CursoId,
                        principalTable: "cursos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asignaciones_docentes_docentes_DocenteId",
                        column: x => x.DocenteId,
                        principalTable: "docentes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_asignaciones_docentes_secciones_SeccionId",
                        column: x => x.SeccionId,
                        principalTable: "secciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "matriculas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    AlumnoId = table.Column<int>(type: "integer", nullable: false),
                    SeccionId = table.Column<int>(type: "integer", nullable: false),
                    PeriodoId = table.Column<int>(type: "integer", nullable: false),
                    FechaMatricula = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FechaRetiro = table.Column<DateOnly>(type: "date", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_matriculas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_matriculas_alumnos_AlumnoId",
                        column: x => x.AlumnoId,
                        principalTable: "alumnos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matriculas_periodos_PeriodoId",
                        column: x => x.PeriodoId,
                        principalTable: "periodos",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_matriculas_secciones_SeccionId",
                        column: x => x.SeccionId,
                        principalTable: "secciones",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "asistencias",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatriculaId = table.Column<int>(type: "integer", nullable: false),
                    Fecha = table.Column<DateOnly>(type: "date", nullable: false),
                    Estado = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    Observacion = table.Column<string>(type: "character varying(250)", maxLength: 250, nullable: true),
                    RegistradoPorUsuarioId = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_asistencias", x => x.Id);
                    table.ForeignKey(
                        name: "FK_asistencias_matriculas_MatriculaId",
                        column: x => x.MatriculaId,
                        principalTable: "matriculas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "cuotas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatriculaId = table.Column<int>(type: "integer", nullable: false),
                    Numero = table.Column<int>(type: "integer", nullable: false),
                    Concepto = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    Monto = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: false),
                    FechaVencimiento = table.Column<DateOnly>(type: "date", nullable: false),
                    FechaPago = table.Column<DateOnly>(type: "date", nullable: true),
                    MoraPagada = table.Column<decimal>(type: "numeric(10,2)", precision: 10, scale: 2, nullable: true),
                    MedioPago = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: true),
                    NumeroOperacion = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_cuotas", x => x.Id);
                    table.ForeignKey(
                        name: "FK_cuotas_matriculas_MatriculaId",
                        column: x => x.MatriculaId,
                        principalTable: "matriculas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "notas",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    MatriculaId = table.Column<int>(type: "integer", nullable: false),
                    CompetenciaId = table.Column<int>(type: "integer", nullable: false),
                    Bimestre = table.Column<int>(type: "integer", nullable: false),
                    Valor = table.Column<decimal>(type: "numeric(4,2)", precision: 4, scale: 2, nullable: false),
                    RegistradoPorUsuarioId = table.Column<int>(type: "integer", nullable: false),
                    ActualizadoEn = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_notas", x => x.Id);
                    table.CheckConstraint("ck_notas_bimestre", "\"Bimestre\" BETWEEN 1 AND 4");
                    table.CheckConstraint("ck_notas_vigesimal", "\"Valor\" >= 0 AND \"Valor\" <= 20");
                    table.ForeignKey(
                        name: "FK_notas_competencias_CompetenciaId",
                        column: x => x.CompetenciaId,
                        principalTable: "competencias",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_notas_matriculas_MatriculaId",
                        column: x => x.MatriculaId,
                        principalTable: "matriculas",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_alumnos_Apellidos",
                table: "alumnos",
                column: "Apellidos");

            migrationBuilder.CreateIndex(
                name: "IX_alumnos_ApoderadoId",
                table: "alumnos",
                column: "ApoderadoId");

            migrationBuilder.CreateIndex(
                name: "IX_alumnos_Codigo",
                table: "alumnos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_alumnos_Dni",
                table: "alumnos",
                column: "Dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_apoderados_Dni",
                table: "apoderados",
                column: "Dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asignaciones_docentes_CursoId_SeccionId",
                table: "asignaciones_docentes",
                columns: new[] { "CursoId", "SeccionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_asignaciones_docentes_DocenteId",
                table: "asignaciones_docentes",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_asignaciones_docentes_SeccionId",
                table: "asignaciones_docentes",
                column: "SeccionId");

            migrationBuilder.CreateIndex(
                name: "IX_asistencias_Fecha",
                table: "asistencias",
                column: "Fecha");

            migrationBuilder.CreateIndex(
                name: "IX_asistencias_MatriculaId_Fecha",
                table: "asistencias",
                columns: new[] { "MatriculaId", "Fecha" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_competencias_CursoId",
                table: "competencias",
                column: "CursoId");

            migrationBuilder.CreateIndex(
                name: "IX_cuotas_FechaVencimiento",
                table: "cuotas",
                column: "FechaVencimiento");

            migrationBuilder.CreateIndex(
                name: "IX_cuotas_MatriculaId_Numero",
                table: "cuotas",
                columns: new[] { "MatriculaId", "Numero" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cursos_Codigo",
                table: "cursos",
                column: "Codigo",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_cursos_GradoId",
                table: "cursos",
                column: "GradoId");

            migrationBuilder.CreateIndex(
                name: "IX_docentes_Dni",
                table: "docentes",
                column: "Dni",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matriculas_AlumnoId_PeriodoId",
                table: "matriculas",
                columns: new[] { "AlumnoId", "PeriodoId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_matriculas_PeriodoId",
                table: "matriculas",
                column: "PeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_matriculas_SeccionId",
                table: "matriculas",
                column: "SeccionId");

            migrationBuilder.CreateIndex(
                name: "IX_notas_CompetenciaId",
                table: "notas",
                column: "CompetenciaId");

            migrationBuilder.CreateIndex(
                name: "IX_notas_MatriculaId_CompetenciaId_Bimestre",
                table: "notas",
                columns: new[] { "MatriculaId", "CompetenciaId", "Bimestre" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_periodos_Anio",
                table: "periodos",
                column: "Anio",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_secciones_GradoId_PeriodoId_Letra",
                table: "secciones",
                columns: new[] { "GradoId", "PeriodoId", "Letra" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_secciones_PeriodoId",
                table: "secciones",
                column: "PeriodoId");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_ApoderadoId",
                table: "usuarios",
                column: "ApoderadoId");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_DocenteId",
                table: "usuarios",
                column: "DocenteId");

            migrationBuilder.CreateIndex(
                name: "IX_usuarios_Email",
                table: "usuarios",
                column: "Email",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "asignaciones_docentes");

            migrationBuilder.DropTable(
                name: "asistencias");

            migrationBuilder.DropTable(
                name: "cuotas");

            migrationBuilder.DropTable(
                name: "notas");

            migrationBuilder.DropTable(
                name: "usuarios");

            migrationBuilder.DropTable(
                name: "competencias");

            migrationBuilder.DropTable(
                name: "matriculas");

            migrationBuilder.DropTable(
                name: "docentes");

            migrationBuilder.DropTable(
                name: "cursos");

            migrationBuilder.DropTable(
                name: "alumnos");

            migrationBuilder.DropTable(
                name: "secciones");

            migrationBuilder.DropTable(
                name: "apoderados");

            migrationBuilder.DropTable(
                name: "grados");

            migrationBuilder.DropTable(
                name: "periodos");
        }
    }
}
