import { createFileRoute } from '@tanstack/react-router'
import { getGetTodosQueryOptions, useCreateTodo } from '@/api/generated'
import type { TodoRequest } from '@/api/generated/model'
import { TodoForm } from '@/todos/todo-form'
import { TodoList } from '@/todos/todo-list'
import { useInvalidateTodos } from '@/todos/use-invalidate-todos'

export const Route = createFileRoute('/_authenticated/')({
  loader: ({ context }) =>
    context.queryClient.ensureQueryData(getGetTodosQueryOptions()),
  component: TodosPage,
})

function TodosPage() {
  const invalidateTodos = useInvalidateTodos()
  const { mutateAsync: createTodo } = useCreateTodo({
    mutation: { meta: { handlesErrors: true }, onSuccess: invalidateTodos },
  })

  async function add(data: TodoRequest) {
    await createTodo({ data })
  }

  return (
    <div className="flex flex-col gap-6">
      <TodoForm submitLabel="Add" onSubmit={add} />
      <TodoList />
    </div>
  )
}
