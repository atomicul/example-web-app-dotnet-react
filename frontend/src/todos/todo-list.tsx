import { errorMessage } from '@/api/fetcher'
import { useGetTodos } from '@/api/generated'
import { TodoItem } from './todo-item'

export function TodoList() {
  const { data: todos, isPending, error } = useGetTodos()

  if (isPending) {
    return <p className="text-muted-foreground text-sm">Loading todos…</p>
  }
  if (error) {
    return <p className="text-destructive text-sm">{errorMessage(error)}</p>
  }
  if (todos.length === 0) {
    return (
      <p className="text-muted-foreground text-sm">
        Nothing to do yet. Add your first todo above.
      </p>
    )
  }

  return (
    <ul className="divide-y rounded-lg border">
      {todos.map((todo) => (
        <TodoItem key={todo.id} todo={todo} />
      ))}
    </ul>
  )
}
