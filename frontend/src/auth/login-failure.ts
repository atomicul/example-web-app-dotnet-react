import { ApiError } from '@/api/fetcher'
import { TwoFactorMethod } from '@/api/generated/model'

/**
 * The reason Identity refused a sign-in with, such as `NotAllowed` or `RequiresTwoFactor`, from the detail of its
 * 401; undefined when the request failed in another way.
 */
export function loginFailureReason(error: unknown) {
  return error instanceof ApiError && error.status === 401
    ? (error.problem?.detail ?? '')
    : undefined
}

/**
 * How the user finishes the login Identity refused with `RequiresTwoFactor`, as the API names it in the problem's
 * `twoFactorMethod`.
 */
export function twoFactorMethodOf(error: unknown): TwoFactorMethod {
  const problem =
    error instanceof ApiError
      ? (error.problem as { twoFactorMethod?: TwoFactorMethod } | undefined)
      : undefined
  return problem?.twoFactorMethod ?? TwoFactorMethod.Email
}
