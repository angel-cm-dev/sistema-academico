import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';
import { Navigate, useLocation, useNavigate } from 'react-router-dom';
import { GraduationCap } from 'lucide-react';
import { ApiError } from '@/api/client';
import { useAuth } from '@/auth/AuthContext';
import { Button } from '@/components/ui/Button';
import { Alert } from '@/components/ui/Feedback';
import { Field, Input } from '@/components/ui/Form';

const schema = z.object({
  email: z.string().trim().min(1, 'Ingrese su correo.').email('Correo no válido.'),
  password: z.string().min(1, 'Ingrese su contraseña.'),
});
type FormValues = z.infer<typeof schema>;

const DEMOS = [
  { rol: 'Administración', email: 'admin@demo.pe' },
  { rol: 'Docente', email: 'docente@demo.pe' },
  { rol: 'Apoderado', email: 'apoderado@demo.pe' },
];

export default function LoginPage() {
  const { usuario, login } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();
  const [error, setError] = useState<string | null>(null);
  const {
    register,
    handleSubmit,
    setValue,
    formState: { errors, isSubmitting },
  } = useForm<FormValues>({ resolver: zodResolver(schema), defaultValues: { email: '', password: '' } });

  if (usuario) return <Navigate to="/" replace />;

  const onSubmit = async (values: FormValues) => {
    setError(null);
    try {
      await login(values.email, values.password);
      const destino = (location.state as { from?: string } | null)?.from ?? '/';
      navigate(destino, { replace: true });
    } catch (e) {
      setError(e instanceof ApiError ? e.message : 'No se pudo iniciar sesión.');
    }
  };

  return (
    <div className="flex min-h-screen items-center justify-center bg-gradient-to-br from-brand-50 via-white to-slate-100 px-4 dark:from-slate-950 dark:via-slate-900 dark:to-brand-950">
      <div className="w-full max-w-md">
        <div className="mb-8 flex flex-col items-center gap-3 text-center">
          <span className="flex h-12 w-12 items-center justify-center rounded-xl bg-brand-600 text-white shadow-lg">
            <GraduationCap className="h-7 w-7" />
          </span>
          <h1 className="text-2xl font-semibold">Sistema Académico</h1>
          <p className="text-sm text-slate-500">Matrícula, notas, asistencia y pensiones en un solo lugar.</p>
        </div>

        <form
          onSubmit={handleSubmit(onSubmit)}
          noValidate
          className="space-y-4 rounded-2xl border border-slate-200 bg-white p-6 shadow-sm dark:border-slate-800 dark:bg-slate-900"
        >
          {error && <Alert>{error}</Alert>}
          <Field label="Correo electrónico" error={errors.email?.message}>
            {(id, desc) => (
              <Input id={id} aria-describedby={desc} type="email" autoComplete="username" invalid={!!errors.email} {...register('email')} />
            )}
          </Field>
          <Field label="Contraseña" error={errors.password?.message}>
            {(id, desc) => (
              <Input
                id={id}
                aria-describedby={desc}
                type="password"
                autoComplete="current-password"
                invalid={!!errors.password}
                {...register('password')}
              />
            )}
          </Field>
          <Button type="submit" className="w-full" loading={isSubmitting}>
            Ingresar
          </Button>
        </form>

        <div className="mt-6 rounded-xl border border-dashed border-slate-300 p-4 text-sm dark:border-slate-700">
          <p className="mb-2 font-medium text-slate-700 dark:text-slate-300">Cuentas demo (contraseña: Demo123!)</p>
          <div className="flex flex-wrap gap-2">
            {DEMOS.map((d) => (
              <button
                key={d.email}
                type="button"
                className="rounded-full bg-slate-100 px-3 py-1 text-xs text-slate-700 hover:bg-brand-100 dark:bg-slate-800 dark:text-slate-300"
                onClick={() => {
                  setValue('email', d.email);
                  setValue('password', 'Demo123!');
                }}
              >
                {d.rol}
              </button>
            ))}
          </div>
        </div>
      </div>
    </div>
  );
}
