/** @type {import('tailwindcss').Config} */
module.exports = {
  content: [
    './src/**/*.{html,ts}',
  ],
  theme: {
    extend: {
      colors: {
        primary: '#0ea5e9',
        secondary: '#10b981',
      }
    },
  },
  plugins: [
    require('@tailwindcss/forms')
  ],
}
