import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { CreateTodoBody } from '@/api/generated/index.zod'
import type { TodoRequest, TodoResponse } from '@/api/generated/model'
import { Button } from '@/components/ui/button'
import { Field, FieldError, FieldLabel } from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { setServerErrors } from '@/lib/server-errors'

type TodoFormProps = {
  todo?: TodoResponse
  submitLabel: string
  onSubmit: (todo: TodoRequest) => Promise<void>
}

/** Creates or edits a todo; the server's validation errors show next to their fields. */
export function TodoForm({ todo, submitLabel, onSubmit }: TodoFormProps) {
  const { t } = useTranslation()
  const form = useForm({
    resolver: zodResolver(CreateTodoBody),
    defaultValues: {
      title: todo?.title ?? '',
      isComplete: todo?.isComplete ?? false,
      dueDate: todo?.dueDate ?? null,
    },
  })
  const { errors, isSubmitting } = form.formState

  async function submit(values: TodoRequest) {
    try {
      await onSubmit(values)
      form.reset()
    } catch (error) {
      setServerErrors(form, error)
    }
  }

  return (
    <form
      onSubmit={form.handleSubmit(submit)}
      noValidate
      className="@container flex flex-col gap-2"
    >
      <div className="flex flex-col gap-3 @md:flex-row @md:items-start">
        <Field data-invalid={!!errors.title} className="flex-1">
          <FieldLabel
            htmlFor={`title-${todo?.id ?? 'new'}`}
            className="sr-only"
          >
            {t('todos.title')}
          </FieldLabel>
          <Input
            id={`title-${todo?.id ?? 'new'}`}
            placeholder={t('todos.titlePlaceholder')}
            aria-invalid={!!errors.title}
            {...form.register('title')}
          />
          <FieldError errors={[errors.title]} />
        </Field>
        <Field data-invalid={!!errors.dueDate} className="@md:w-40">
          <FieldLabel
            htmlFor={`due-date-${todo?.id ?? 'new'}`}
            className="sr-only"
          >
            {t('todos.dueDate')}
          </FieldLabel>
          <Input
            id={`due-date-${todo?.id ?? 'new'}`}
            type="date"
            aria-invalid={!!errors.dueDate}
            {...form.register('dueDate', {
              setValueAs: (value) => value || null,
            })}
          />
          <FieldError errors={[errors.dueDate]} />
        </Field>
        <Button type="submit" disabled={isSubmitting}>
          {submitLabel}
        </Button>
      </div>
      <FieldError errors={[errors.root]} />
    </form>
  )
}
