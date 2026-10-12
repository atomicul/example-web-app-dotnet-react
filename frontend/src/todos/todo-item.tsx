import { Trash2Icon } from 'lucide-react'
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

function formatDueDate(dueDate: string) {
  return new Date(`${dueDate}T00:00`).toLocaleDateString(undefined, {
    dateStyle: 'medium',
  })
}

export function TodoItem({ todo }: { todo: TodoResponse }) {
  const mutation = { meta: { invalidates: [getGetTodosQueryKey()] } }
  const update = useUpdateTodo({ mutation })
  const remove = useDeleteTodo({ mutation })

  function toggle(isComplete: boolean) {
    update.mutate({ id: todo.id, data: { ...todo, isComplete } })
  }

  return (
    <li className="flex items-center gap-3 px-4 py-3">
      <Checkbox
        checked={todo.isComplete}
        disabled={update.isPending}
        onCheckedChange={toggle}
        aria-label={`Mark ${todo.title} as ${todo.isComplete ? 'open' : 'done'}`}
      />
      <div className="min-w-0 flex-1">
        <p
          className={cn(
            'truncate',
            todo.isComplete && 'text-muted-foreground line-through',
          )}
        >
          {todo.title}
        </p>
        {todo.dueDate && (
          <p className="text-muted-foreground text-xs">
            Due {formatDueDate(todo.dueDate)}
          </p>
        )}
      </div>
      <EditTodoDialog todo={todo} />
      <Button
        variant="ghost"
        size="icon-sm"
        aria-label={`Delete ${todo.title}`}
        disabled={remove.isPending}
        onClick={() => remove.mutate({ id: todo.id })}
      >
        <Trash2Icon />
      </Button>
    </li>
  )
}
