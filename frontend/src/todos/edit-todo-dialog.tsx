import { PencilIcon } from 'lucide-react'
import { useState } from 'react'
import { getGetTodosQueryKey, useUpdateTodo } from '@/api/generated'
import type { TodoRequest, TodoResponse } from '@/api/generated/model'
import { Button } from '@/components/ui/button'
import {
  Dialog,
  DialogContent,
  DialogHeader,
  DialogTitle,
  DialogTrigger,
} from '@/components/ui/dialog'
import { TodoForm } from './todo-form'

export function EditTodoDialog({ todo }: { todo: TodoResponse }) {
  const [open, setOpen] = useState(false)
  const { mutateAsync: updateTodo } = useUpdateTodo({
    mutation: {
      meta: { handlesErrors: true, invalidates: [getGetTodosQueryKey()] },
    },
  })

  async function save(data: TodoRequest) {
    await updateTodo({ id: todo.id, data })
    setOpen(false)
  }

  return (
    <Dialog open={open} onOpenChange={setOpen}>
      <DialogTrigger
        render={
          <Button
            variant="ghost"
            size="icon-sm"
            aria-label={`Edit ${todo.title}`}
          />
        }
      >
        <PencilIcon />
      </DialogTrigger>
      <DialogContent>
        <DialogHeader>
          <DialogTitle>Edit todo</DialogTitle>
        </DialogHeader>
        <TodoForm todo={todo} submitLabel="Save" onSubmit={save} />
      </DialogContent>
    </Dialog>
  )
}
