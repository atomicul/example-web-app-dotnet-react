import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { z } from 'zod'
import { PostApiAuthRegisterBody } from '@/api/generated/index.zod'
import type { RegisterRequest } from '@/api/generated/model'
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

const RegistrationForm = PostApiAuthRegisterBody.extend({
  confirmPassword: z.string(),
}).refine((values) => values.password === values.confirmPassword, {
  path: ['confirmPassword'],
})

type Registration = z.infer<typeof RegistrationForm>

const registrationFields = [
  {
    name: 'email',
    label: 'credentials.email',
    type: 'email',
    autoComplete: 'email',
  },
  {
    name: 'password',
    label: 'credentials.password',
    type: 'password',
    autoComplete: 'new-password',
  },
  {
    name: 'confirmPassword',
    label: 'register.confirmPassword',
    type: 'password',
    autoComplete: 'new-password',
  },
] as const

type RegisterFormProps = {
  onSubmit: (registration: RegisterRequest) => Promise<void>
}

/** Asks for an email and a password, the password twice, checked against the API's rules; failures show under the fields. */
export function RegisterForm({ onSubmit }: RegisterFormProps) {
  const { t } = useTranslation()
  const form = useForm({
    resolver: zodResolver(RegistrationForm, {
      error: fieldMessages({
        email: t('register.emailRules'),
        password: t('register.passwordRules'),
        confirmPassword: t('account.passwordsDiffer'),
      }),
    }),
    defaultValues: { email: '', password: '', confirmPassword: '' },
  })
  const { errors, isSubmitting } = form.formState

  async function submit({ email, password }: Registration) {
    try {
      await onSubmit({ email, password })
    } catch (error) {
      setServerErrors(form, error)
    }
  }

  return (
    <form onSubmit={form.handleSubmit(submit)} noValidate>
      <FieldGroup>
        {registrationFields.map(({ name, label, type, autoComplete }) => (
          <Field key={name} data-invalid={!!errors[name]}>
            <FieldLabel htmlFor={name}>{t(label)}</FieldLabel>
            <Input
              id={name}
              type={type}
              autoComplete={autoComplete}
              aria-invalid={!!errors[name]}
              {...form.register(name)}
            />
            <FieldError errors={[errors[name]]} />
          </Field>
        ))}
        <FieldError errors={[errors.root]} />
        <Button type="submit" disabled={isSubmitting}>
          {t('register.submit')}
        </Button>
      </FieldGroup>
    </form>
  )
}
