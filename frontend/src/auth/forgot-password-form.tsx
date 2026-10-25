import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import type { z } from 'zod'
import { PostApiAuthRegisterBody } from '@/api/generated/index.zod'
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

/** Checks the email as registration does, since only a registered email gets a link. */
const ForgotPasswordBody = PostApiAuthRegisterBody.pick({ email: true })

type ForgotPassword = z.infer<typeof ForgotPasswordBody>

type ForgotPasswordFormProps = {
  onSubmit: (email: string) => Promise<void>
}

/** Asks for the email to send a password reset link to; failures show under the field. */
export function ForgotPasswordForm({ onSubmit }: ForgotPasswordFormProps) {
  const { t } = useTranslation()
  const form = useForm({
    resolver: zodResolver(ForgotPasswordBody, {
      error: fieldMessages({ email: t('register.emailRules') }),
    }),
    defaultValues: { email: '' },
  })
  const { errors, isSubmitting } = form.formState

  async function submit({ email }: ForgotPassword) {
    try {
      await onSubmit(email)
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
        <FieldError errors={[errors.root]} />
        <Button type="submit" disabled={isSubmitting}>
          {t('forgotPassword.submit')}
        </Button>
      </FieldGroup>
    </form>
  )
}
