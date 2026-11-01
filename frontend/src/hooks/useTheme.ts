import { useCallback, useState } from 'react';

type Tema = 'claro' | 'oscuro';

function temaInicial(): Tema {
  return document.documentElement.classList.contains('dark') ? 'oscuro' : 'claro';
}

/** Alterna la clase "dark" en <html> (Tailwind darkMode: 'class') y la recuerda. */
export function useTheme() {
  const [tema, setTema] = useState<Tema>(temaInicial);

  const alternar = useCallback(() => {
    setTema((actual) => {
      const nuevo: Tema = actual === 'oscuro' ? 'claro' : 'oscuro';
      document.documentElement.classList.toggle('dark', nuevo === 'oscuro');
      try {
        localStorage.setItem('tema', nuevo);
      } catch {
        // almacenamiento no disponible: el tema solo dura la sesión
      }
      return nuevo;
    });
  }, []);

  return { tema, alternar };
}
