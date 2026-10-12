import { createFileRoute } from '@tanstack/react-router'

export const Route = createFileRoute('/_authenticated/')({
  component: () => <p>Your todos will show up here.</p>,
})
