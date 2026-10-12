import { defineConfig } from 'orval'

export default defineConfig({
  hooks: {
    input: './openapi.json',
    output: {
      mode: 'tags-split',
      target: 'src/api/generated',
      schemas: 'src/api/generated/model',
      client: 'react-query',
      httpClient: 'fetch',
      override: {
        mutator: { path: 'src/api/fetcher.ts', name: 'fetcher' },
        fetch: { includeHttpResponseReturnType: false },
        query: { shouldSplitQueryKey: true },
      },
    },
  },
  zod: {
    input: './openapi.json',
    output: {
      mode: 'tags-split',
      target: 'src/api/generated',
      client: 'zod',
      fileExtension: '.zod.ts',
    },
  },
})
