import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import type { z } from 'zod'
import type { LoginRequest } from '@/api/generated/model'
import { Button } from '@/components/ui/button'
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
} from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { fieldMessages } from '@/lib/field-messages'
import { setServerErrors } from '@/lib/server-errors'

type CredentialsFormProps = {
  schema: z.ZodType<LoginRequest, LoginRequest>
  /** What to show for a field the schema refuses, in place of zod's message. */
  messages?: Partial<Record<keyof LoginRequest, string>>
  submitLabel: string
  /** `new-password` lets password managers offer to generate one when signing up. */
  passwordAutoComplete: 'current-password' | 'new-password'
  onSubmit: (credentials: LoginRequest) => Promise<void>
}

/** Email and password form shared by login and registration; failures show under the fields. */
export function CredentialsForm({
  schema,
  messages = {},
  submitLabel,
  passwordAutoComplete,
  onSubmit,
}: CredentialsFormProps) {
  const { t } = useTranslation()
  const form = useForm<LoginRequest>({
    resolver: zodResolver(schema, { error: fieldMessages(messages) }),
    defaultValues: { email: '', password: '' },
  })
  const { errors, isSubmitting } = form.formState

  async function submit(credentials: LoginRequest) {
    try {
      await onSubmit(credentials)
    } catch (error) {
      setServerErrors(form, error)
    }
  }

  return (
    <form onSubmit={form.handleSubmit(submit)} noValidate>
      <FieldGroup>
        <Field data-invalid={!!errors.email}>
          <FieldLabel htmlFor="email">{t('credentials.email')}</FieldLabel>
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
          <FieldLabel htmlFor="password">
            {t('credentials.password')}
          </FieldLabel>
          <Input
            id="password"
            type="password"
            autoComplete={passwordAutoComplete}
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
