import { fileURLToPath, URL } from 'node:url'

import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'
import vueDevTools from 'vite-plugin-vue-devtools'

export default defineConfig(({ command, mode }) => {
	if (command === 'serve') {
		return {
			plugins: [
				vue(),
				vueDevTools(),
			],
		}
	}
	else {
		return {
			plugins: [
				vue()
			],
		}
	}
})
