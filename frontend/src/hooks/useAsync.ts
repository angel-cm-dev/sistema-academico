import { useCallback, useEffect, useRef, useState } from 'react';
import { ApiError } from '@/api/client';

interface AsyncState<T> {
  data: T | undefined;
  loading: boolean;
  error: ApiError | null;
  reload: () => void;
  setData: (data: T) => void;
}

/**
 * Ejecuta una carga asíncrona y expone loading/error/data.
 * Ignora respuestas "viejas": si las dependencias cambian antes de que termine
 * una petición, su resultado se descarta (evita condiciones de carrera en filtros).
 */
export function useAsync<T>(fn: () => Promise<T>, deps: unknown[]): AsyncState<T> {
  const [data, setData] = useState<T>();
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<ApiError | null>(null);
  const [tick, setTick] = useState(0);
  const seq = useRef(0);

  useEffect(() => {
    const actual = ++seq.current;
    setLoading(true);
    setError(null);
    fn()
      .then((r) => {
        if (actual === seq.current) setData(r);
      })
      .catch((e: unknown) => {
        if (actual !== seq.current) return;
        setError(e instanceof ApiError ? e : new ApiError(500, 'Error', String(e)));
      })
      .finally(() => {
        if (actual === seq.current) setLoading(false);
      });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [...deps, tick]);

  const reload = useCallback(() => setTick((t) => t + 1), []);
  return { data, loading, error, reload, setData };
}
