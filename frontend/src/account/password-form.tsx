import { zodResolver } from '@hookform/resolvers/zod'
import { useForm } from 'react-hook-form'
import { useTranslation } from 'react-i18next'
import { toast } from 'sonner'
import { z } from 'zod'
import {
  getGetPendingEmailQueryKey,
  usePostApiAuthManageInfo,
} from '@/api/generated'
import { PostApiAuthManageInfoBody } from '@/api/generated/index.zod'
import { Button } from '@/components/ui/button'
import {
  Card,
  CardContent,
  CardDescription,
  CardHeader,
  CardTitle,
} from '@/components/ui/card'
import {
  Field,
  FieldError,
  FieldGroup,
  FieldLabel,
} from '@/components/ui/field'
import { Input } from '@/components/ui/input'
import { fieldMessages } from '@/lib/field-messages'
import { setServerErrors } from '@/lib/server-errors'

const PasswordChangeForm = PostApiAuthManageInfoBody.pick({
  newPassword: true,
})
  .required()
  .extend({ oldPassword: z.string().min(1), confirmPassword: z.string() })
  .refine((values) => values.newPassword === values.confirmPassword, {
    path: ['confirmPassword'],
  })

type PasswordChange = z.infer<typeof PasswordChangeForm>

const passwordFields = [
  {
    name: 'oldPassword',
    label: 'account.currentPassword',
    autoComplete: 'current-password',
  },
  {
    name: 'newPassword',
    label: 'account.newPassword',
    autoComplete: 'new-password',
  },
  {
    name: 'confirmPassword',
    label: 'account.confirmPassword',
    autoComplete: 'new-password',
  },
] as const

/**
 * Changes the user's password, given the current one. The session that changes it stays signed in, and every other
 * one ends; a pending email change is dropped too, since its link stops working.
 */
export function PasswordForm() {
  const { t } = useTranslation()
  const form = useForm({
    resolver: zodResolver(PasswordChangeForm, {
      error: fieldMessages({
        oldPassword: t('account.currentPasswordRequired'),
        newPassword: t('register.passwordRules'),
        confirmPassword: t('account.passwordsDiffer'),
      }),
    }),
    defaultValues: { oldPassword: '', newPassword: '', confirmPassword: '' },
  })
  const { errors, isSubmitting } = form.formState
  const { mutateAsync: changePassword } = usePostApiAuthManageInfo({
    mutation: {
      meta: {
        handlesErrors: true,
        invalidates: [getGetPendingEmailQueryKey()],
      },
    },
  })

  async function submit({ oldPassword, newPassword }: PasswordChange) {
    try {
      await changePassword({ data: { oldPassword, newPassword } })
      toast.success(t('account.passwordChanged'))
      form.reset()
    } catch (error) {
      setServerErrors(form, error)
    }
  }

  return (
    <Card>
      <CardHeader>
        <CardTitle>{t('account.password')}</CardTitle>
        <CardDescription>{t('account.passwordDescription')}</CardDescription>
      </CardHeader>
      <CardContent>
        <form onSubmit={form.handleSubmit(submit)} noValidate>
          <FieldGroup>
            {passwordFields.map(({ name, label, autoComplete }) => (
              <Field key={name} data-invalid={!!errors[name]}>
                <FieldLabel htmlFor={name}>{t(label)}</FieldLabel>
                <Input
                  id={name}
                  type="password"
                  autoComplete={autoComplete}
                  aria-invalid={!!errors[name]}
                  {...form.register(name)}
                />
                <FieldError errors={[errors[name]]} />
              </Field>
            ))}
            <FieldError errors={[errors.root]} />
            <Button
              type="submit"
              disabled={isSubmitting}
              className="self-start"
            >
              {t('account.changePassword')}
            </Button>
          </FieldGroup>
        </form>
      </CardContent>
    </Card>
  )
}
