import { useEffect, useState } from 'react';

/** Retrasa un valor (p. ej. el texto de búsqueda) para no llamar a la API en cada tecla. */
export function useDebounce<T>(value: T, delay = 350): T {
  const [debounced, setDebounced] = useState(value);
  useEffect(() => {
    const id = setTimeout(() => setDebounced(value), delay);
    return () => clearTimeout(id);
  }, [value, delay]);
  return debounced;
}
