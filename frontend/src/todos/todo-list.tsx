import { useTranslation } from 'react-i18next'
import { errorMessage } from '@/api/fetcher'
import { useGetTodos } from '@/api/generated'
import { TodoItem } from './todo-item'

export function TodoList() {
  const { t } = useTranslation()
  const { data: todos, isPending, error } = useGetTodos()

  if (isPending) {
    return <p className="text-muted-foreground text-sm">{t('todos.loading')}</p>
  }
  if (error) {
    return <p className="text-destructive text-sm">{errorMessage(error)}</p>
  }
  if (todos.length === 0) {
    return <p className="text-muted-foreground text-sm">{t('todos.empty')}</p>
  }

  return (
    <ul className="divide-y rounded-lg border">
      {todos.map((todo) => (
        <TodoItem key={todo.id} todo={todo} />
      ))}
    </ul>
  )
}
