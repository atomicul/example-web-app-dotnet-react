import { createFileRoute } from '@tanstack/react-router'
import { useTranslation } from 'react-i18next'
import {
  getGetTodosQueryKey,
  getGetTodosQueryOptions,
  useCreateTodo,
} from '@/api/generated'
import type { TodoRequest } from '@/api/generated/model'
import { TodoForm } from '@/todos/todo-form'
import { TodoList } from '@/todos/todo-list'

export const Route = createFileRoute('/_authenticated/')({
  loader: ({ context }) =>
    context.queryClient.query({
      ...getGetTodosQueryOptions(),
      staleTime: 'static',
    }),
  component: TodosPage,
})

function TodosPage() {
  const { t } = useTranslation()
  const { mutateAsync: createTodo } = useCreateTodo({
    mutation: {
      meta: { handlesErrors: true, invalidates: [getGetTodosQueryKey()] },
    },
  })

  async function add(data: TodoRequest) {
    await createTodo({ data })
  }

  return (
    <div className="flex flex-col gap-6">
      <TodoForm submitLabel={t('todos.add')} onSubmit={add} />
      <TodoList />
    </div>
  )
}
