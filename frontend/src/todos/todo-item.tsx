import { Trash2Icon } from 'lucide-react'
import { useTranslation } from 'react-i18next'
import {
  getGetTodosQueryKey,
  useDeleteTodo,
  useUpdateTodo,
} from '@/api/generated'
import type { TodoResponse } from '@/api/generated/model'
import { Button } from '@/components/ui/button'
import { Checkbox } from '@/components/ui/checkbox'
import { cn } from '@/lib/utils'
import { EditTodoDialog } from './edit-todo-dialog'

/**
 * Shows a toggled todo as toggled at once, while its request and the refetch after it are pending; a failed toggle
 * shows the todo as it was again. Requests about the same todo run one after another, so they reach the API in the
 * order they were made.
 */
export function TodoItem({ todo }: { todo: TodoResponse }) {
  const { t } = useTranslation()
  const mutation = {
    meta: { invalidates: [getGetTodosQueryKey()] },
    scope: { id: `todo-${todo.id}` },
  }
  const update = useUpdateTodo({ mutation })
  const remove = useDeleteTodo({ mutation })
  const isComplete = update.isPending
    ? update.variables.data.isComplete
    : todo.isComplete

  function toggle(checked: boolean) {
    update.mutate({ id: todo.id, data: { ...todo, isComplete: checked } })
  }

  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <Checkbox
        checked={isComplete}
        onCheckedChange={toggle}
        aria-label={t(isComplete ? 'todos.markOpen' : 'todos.markDone', {
          title: todo.title,
        })}
      />
      <div className="min-w-0 flex-1">
        <p
          className={cn(
            'truncate',
            isComplete && 'text-muted-foreground line-through',
          )}
        >
          {todo.title}
        </p>
        {todo.dueDate && (
          <p className="text-muted-foreground text-xs">
            {t('todos.due', { date: new Date(`${todo.dueDate}T00:00`) })}
          </p>
        )}
      </div>
      <EditTodoDialog todo={todo} />
      <Button
        variant="ghost"
        size="icon-sm"
        aria-label={t('todos.delete', { title: todo.title })}
        disabled={remove.isPending}
        onClick={() => remove.mutate({ id: todo.id })}
      >
        <Trash2Icon />
      </Button>
    </li>
  )
}
