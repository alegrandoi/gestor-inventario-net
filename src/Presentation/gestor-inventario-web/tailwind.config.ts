import type { Config } from 'tailwindcss';

const config: Config = {
  content: [
    './app/**/*.{js,ts,jsx,tsx}',
    './components/**/*.{js,ts,jsx,tsx}',
    './src/**/*.{js,ts,jsx,tsx}'
  ],
  theme: {
    extend: {
      colors: {
        primary: {
          50: '#e7f2ff',
          100: '#cfe5ff',
          200: '#9fcaff',
          300: '#6faeff',
          400: '#3f93ff',
          500: '#0f78ff',
          600: '#0c60cc',
          700: '#094899',
          800: '#063066',
          900: '#031833'
        }
      }
    }
  },
  plugins: []
};

export default config;
