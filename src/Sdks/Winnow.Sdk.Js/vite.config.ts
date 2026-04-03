import { defineConfig } from 'vite'
import { resolve } from 'path'

export default defineConfig({
    build: {
        lib: {
            entry: {
                winnow: resolve(__dirname, 'src/main.ts'),
                core: resolve(__dirname, 'src/core.ts'),
                ui: resolve(__dirname, 'src/ui.ts'),
                'react/index': resolve(__dirname, 'src/react/index.tsx')
            },
            name: 'Winnow',
            formats: ['es', 'cjs']
        },
        rollupOptions: {
            external: ['react', 'react-dom'],
            output: {
                globals: {
                    react: 'React',
                    'react-dom': 'ReactDOM'
                }
            }
        }
    }
})
