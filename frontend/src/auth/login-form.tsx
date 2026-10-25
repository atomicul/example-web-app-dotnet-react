import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { PostApiAuthLoginBody } from '@/api/generated/index.zod'
import type { LoginRequest } from '@/api/generated/model'
import { Button } from '@/components/ui/button'
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
} from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { setServerErrors } from '@/lib/server-errors'

type LoginFormProps = {
  onSubmit: (credentials: LoginRequest) => Promise<void>
}

/** Email and password form of the login page; failures show under the fields. */
export function LoginForm({ onSubmit }: LoginFormProps) {
  const { t } = useTranslation()
  const form = useForm<LoginRequest>({
    resolver: zodResolver(PostApiAuthLoginBody),
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
            autoComplete="current-password"
            aria-invalid={!!errors.password}
            {...form.register('password')}
          />
          <FieldError errors={[errors.password]} />
        </Field>
        <FieldError errors={[errors.root]} />
        <Button type="submit" disabled={isSubmitting}>
          {t('login.submit')}
        </Button>
      </FieldGroup>
    </form>
  )
}
