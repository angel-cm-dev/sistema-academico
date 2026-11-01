/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** URL base de la API. Vacía = mismo origen (proxy de Vite o nginx). */
  readonly VITE_API_URL?: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
