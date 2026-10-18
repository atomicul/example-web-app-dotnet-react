import { Link, createFileRoute } from '@tanstack/react-router'
import { useState } from 'react'
import { useTranslation } from 'react-i18next'
import { PostApiAuthRegisterBody } from '@/api/generated/index.zod'
import type { LoginRequest } from '@/api/generated/model'
import { AuthCard } from '@/auth/auth-card'
import { CredentialsForm } from '@/auth/credentials-form'
import { publicSearchSchema, redirectWhenSignedIn } from '@/auth/public-route'
import { useAuth } from '@/auth/use-auth'
import { buttonVariants } from '@/components/ui/button'

export const Route = createFileRoute('/register')({
  validateSearch: publicSearchSchema,
  beforeLoad: redirectWhenSignedIn,
  component: RegisterPage,
})

function RegisterPage() {
  const { t } = useTranslation()
  const search = Route.useSearch()
  const { register } = useAuth()
  const [registeredEmail, setRegisteredEmail] = useState<string>()

  async function submit(credentials: LoginRequest) {
    await register(credentials)
    setRegisteredEmail(credentials.email)
  }

  if (registeredEmail) {
    return (
      <AuthCard
        title={t('register.checkEmail')}
        description={t('register.linkSent', { email: registeredEmail })}
      >
        <Link
          to="/login"
          search={search}
          className={buttonVariants({ className: 'w-full' })}
        >
          {t('register.goToLogIn')}
        </Link>
      </AuthCard>
    )
  }

  return (
    <AuthCard
      title={t('register.title')}
      description={t('register.description')}
      footer={
        <p>
          {t('register.haveAccount')}{' '}
          <Link to="/login" search={search} className="underline">
            {t('register.logIn')}
          </Link>
        </p>
      }
    >
      <CredentialsForm
        schema={PostApiAuthRegisterBody}
        messages={{
          email: t('register.emailRules'),
          password: t('register.passwordRules'),
        }}
        submitLabel={t('register.submit')}
        passwordAutoComplete="new-password"
        onSubmit={submit}
      />
    </AuthCard>
  )
}
