import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import type { z } from 'zod'
import { errorMessage } from '@/api/fetcher'
import type { LoginRequest } from '@/api/generated/model'
import { Button } from '@/components/ui/button'
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
} from '@/components/ui/field'
import { Input } from '@/components/ui/input'

type CredentialsFormProps = {
  schema: z.ZodType<LoginRequest, LoginRequest>
  submitLabel: string
  onSubmit: (credentials: LoginRequest) => Promise<void>
}

/** Email and password form shared by login and registration; failures show under the fields. */
export function CredentialsForm({
  schema,
  submitLabel,
  onSubmit,
}: CredentialsFormProps) {
  const form = useForm<LoginRequest>({
    resolver: zodResolver(schema),
    defaultValues: { email: '', password: '' },
  })
  const { errors, isSubmitting } = form.formState

  async function submit(credentials: LoginRequest) {
    try {
      await onSubmit(credentials)
    } catch (error) {
      form.setError('root', { message: errorMessage(error) })
    }
  }

  return (
    <form onSubmit={form.handleSubmit(submit)} noValidate>
      <FieldGroup>
        <Field data-invalid={!!errors.email}>
          <FieldLabel htmlFor="email">Email</FieldLabel>
          <Input
            id="email"
            type="email"
            autoComplete="email"
            aria-invalid={!!errors.email}
            {...form.register('email')}
          />
          <FieldError errors={[errors.email]} />
        </Field>
        <Field data-invalid={!!errors.password}>
          <FieldLabel htmlFor="password">Password</FieldLabel>
          <Input
            id="password"
            type="password"
            autoComplete="current-password"
            aria-invalid={!!errors.password}
            {...form.register('password')}
          />
          <FieldError errors={[errors.password]} />
        </Field>
        <FieldError errors={[errors.root]} />
        <Button type="submit" disabled={isSubmitting}>
          {submitLabel}
        </Button>
      </FieldGroup>
    </form>
  )
}
