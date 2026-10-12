import { useQueryClient } from '@tanstack/react-query'
import { useRouter } from '@tanstack/react-router'
import {
  usePostApiAuthLogin,
  usePostApiAuthLogout,
  usePostApiAuthRegister,
} from '@/api/generated'
import type { LoginRequest } from '@/api/generated/model'

/**
 * Signs the user in and out with Identity's cookie. Afterwards every cached query is dropped
 * and the route guards run again, which sends the user to the right page.
 */
export function useAuth() {
  const queryClient = useQueryClient()
  const router = useRouter()
  const showsErrorsInForm = { mutation: { meta: { handlesErrors: true } } }
  const { mutateAsync: postLogin } = usePostApiAuthLogin(showsErrorsInForm)
  const { mutateAsync: postLogout } = usePostApiAuthLogout()
  const { mutateAsync: postRegister } =
    usePostApiAuthRegister(showsErrorsInForm)

  async function resetSession() {
    queryClient.removeQueries()
    await router.invalidate()
  }

  async function login(credentials: LoginRequest) {
    await postLogin({ data: credentials, params: { useCookies: true } })
    await resetSession()
  }

  /** Creates the account only: Identity emails a confirmation link that must be opened before logging in. */
  async function register(credentials: LoginRequest) {
    await postRegister({ data: credentials })
  }

  async function logout() {
    await postLogout()
    await resetSession()
  }

  return { login, register, logout }
}
