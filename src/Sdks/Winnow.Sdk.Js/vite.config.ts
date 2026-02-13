import { defineConfig } from 'vite'
import { resolve } from 'path'

export default defineConfig({
    build: {
        lib: {
            // Could also be a dictionary or array of multiple entry points
            entry: resolve(__dirname, 'src/main.ts'),
            name: 'Winnow',
            // the proper extensions will be added
            fileName: 'winnow.iife',
            formats: ['iife']
        },
    },
    define: {
        'process.env': {} // Fallback for some libs if needed, usually not with vanilla
    }
})
