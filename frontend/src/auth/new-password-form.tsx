import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { PostApiAuthResetPasswordBody } from '@/api/generated/index.zod'
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

const NewPasswordBody = PostApiAuthResetPasswordBody.pick({ newPassword: true })
  .extend({ confirmPassword: z.string() })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ['confirmPassword'],
  })

type NewPassword = z.infer<typeof NewPasswordBody>

const passwordFields = [
  { name: 'newPassword', label: 'account.newPassword' },
  { name: 'confirmPassword', label: 'account.confirmPassword' },
] as const

type NewPasswordFormProps = {
  onSubmit: (newPassword: string) => Promise<void>
}

/** Asks for a new password twice, checked against the API's rules; failures show under the fields. */
export function NewPasswordForm({ onSubmit }: NewPasswordFormProps) {
  const { t } = useTranslation()
  const form = useForm({
    resolver: zodResolver(NewPasswordBody, {
      error: fieldMessages({
        newPassword: t('register.passwordRules'),
        confirmPassword: t('account.passwordsDiffer'),
      }),
    }),
    defaultValues: { newPassword: '', confirmPassword: '' },
  })
  const { errors, isSubmitting } = form.formState

  async function submit({ newPassword }: NewPassword) {
    try {
      await onSubmit(newPassword)
    } catch (error) {
      setServerErrors(form, error)
    }
  }

  return (
    <form onSubmit={form.handleSubmit(submit)} noValidate>
      <FieldGroup>
        {passwordFields.map(({ name, label }) => (
          <Field key={name} data-invalid={!!errors[name]}>
            <FieldLabel htmlFor={name}>{t(label)}</FieldLabel>
            <Input
              id={name}
              type="password"
              autoComplete="new-password"
              aria-invalid={!!errors[name]}
              {...form.register(name)}
            />
            <FieldError errors={[errors[name]]} />
          </Field>
        ))}
        <FieldError errors={[errors.root]} />
        <Button type="submit" disabled={isSubmitting}>
          {t('resetPassword.submit')}
        </Button>
      </FieldGroup>
    </form>
  )
}
