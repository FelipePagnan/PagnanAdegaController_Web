// ═══════════════════════════════════════════════════════════════
// ERP ADEGA — Design Tokens
// Fonte única de verdade para cores, tipografia, sombras e raios.
// Compartilhado entre Web, Desktop e Mobile.
// ═══════════════════════════════════════════════════════════════

export const colors = {
  // Primária — Vinho
  primary: {
    50: '#F5EFF0',
    100: '#E8D5D8',
    200: '#D4ACB2',
    300: '#B8747D',
    400: '#9B4753',
    500: '#722F37',
    600: '#5E262E',
    700: '#4A1D24',
    800: '#36141A',
    900: '#220B10',
  },

  // Secundária — Ouro
  gold: {
    50: '#FBF8F0',
    100: '#F5EED8',
    200: '#EBDDB1',
    300: '#DCC97E',
    400: '#C8A951',
    500: '#B08E30',
    600: '#8E7226',
    700: '#6B561D',
    800: '#483A13',
    900: '#251E0A',
  },

  // Ação — Verde Garrafa
  teal: {
    50: '#EFF7F7',
    100: '#D0EAEA',
    200: '#A1D5D6',
    300: '#5FB8BA',
    400: '#2E9496',
    500: '#1B6B6D',
    600: '#165758',
    700: '#114243',
    800: '#0C2D2E',
    900: '#071919',
  },

  // Neutros
  neutral: {
    50: '#F7F5F2',
    100: '#ECEAE6',
    200: '#D9D5CF',
    300: '#B8B3AA',
    400: '#969085',
    500: '#747069',
    600: '#5C5852',
    700: '#44413C',
    800: '#2C2A27',
    900: '#1A1917',
  },

  // Superfícies
  surface: '#FDFCFA',
  surfaceAlt: '#F7F5F2',
  white: '#FFFFFF',
  dark: '#1A1917',

  // Estados do sistema
  success: '#2D8A4E',
  successBg: '#E8F5EE',
  warning: '#C4841D',
  warningBg: '#FFF4E0',
  critical: '#C03744',
  criticalBg: '#FCEDEF',
  info: '#2B6CB0',
  infoBg: '#EBF4FF',
  reserved: '#7B61C2',
  reservedBg: '#F0ECFA',
  inactive: '#969085',
  inactiveBg: '#ECEAE6',
  expiring: '#D97706',
  expiringBg: '#FFF7ED',
} as const;

export const fonts = {
  body: "'Inter', system-ui, -apple-system, sans-serif",
  mono: "'JetBrains Mono', 'Fira Code', 'Cascadia Code', monospace",
} as const;

export const fontSizes = {
  xs: '0.75rem',    // 12px
  sm: '0.8125rem',  // 13px
  base: '0.875rem', // 14px
  md: '1rem',       // 16px
  lg: '1.125rem',   // 18px
  xl: '1.375rem',   // 22px
  '2xl': '1.625rem', // 26px
  '3xl': '2rem',     // 32px
} as const;

export const fontWeights = {
  normal: 400,
  medium: 500,
  semibold: 600,
  bold: 700,
  extrabold: 800,
} as const;

export const radius = {
  sm: '6px',
  md: '8px',
  lg: '12px',
  xl: '16px',
  full: '9999px',
} as const;

export const shadows = {
  sm: '0 1px 2px rgba(26, 25, 23, 0.06)',
  md: '0 2px 8px rgba(26, 25, 23, 0.08)',
  lg: '0 4px 16px rgba(26, 25, 23, 0.1)',
  xl: '0 8px 32px rgba(26, 25, 23, 0.12)',
} as const;

export const spacing = {
  0: '0',
  1: '4px',
  2: '8px',
  3: '12px',
  4: '16px',
  5: '20px',
  6: '24px',
  8: '32px',
  10: '40px',
  12: '48px',
  16: '64px',
} as const;

// Breakpoints para responsividade
export const breakpoints = {
  sm: '640px',
  md: '768px',
  lg: '1024px',
  xl: '1280px',
  '2xl': '1536px',
} as const;

// Z-index scale
export const zIndex = {
  dropdown: 10,
  sticky: 20,
  overlay: 30,
  modal: 40,
  toast: 50,
} as const;

// Transições padrão
export const transitions = {
  fast: '0.1s ease',
  normal: '0.15s ease',
  slow: '0.3s ease',
} as const;
