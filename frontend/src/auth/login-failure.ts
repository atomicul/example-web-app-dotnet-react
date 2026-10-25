import { ApiError } from '@/api/fetcher'

/**
 * The reason Identity refused a sign-in with, such as `NotAllowed` or `RequiresTwoFactor`, from the detail of its
 * 401; undefined when the request failed in another way.
 */
export function loginFailureReason(error: unknown) {
  return error instanceof ApiError && error.status === 401
    ? (error.problem?.detail ?? '')
    : undefined
}
