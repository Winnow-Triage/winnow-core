import { defineConfig } from 'vite'
import { resolve } from 'path'

export default defineConfig({
    build: {
        lib: {
            entry: resolve(__dirname, 'src/main.ts'),
            name: 'Winnow',
            fileName: (format) => `winnow.${format}.js`,
            formats: ['iife']
        },
        outDir: 'dist',
        emptyOutDir: false, // Don't clear dist, as the main build runs first
        minify: 'terser',
        terserOptions: {
            format: {
                comments: false,
            },
            compress: {
                drop_console: true,
                drop_debugger: true
            }
        }
    }
})
