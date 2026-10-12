import type { QueryClient } from '@tanstack/react-query'
import { isUnauthorized } from '@/api/fetcher'
import {
  getGetApiAuthManageInfoQueryKey,
  getGetApiAuthManageInfoQueryOptions,
} from '@/api/generated'

export const sessionQueryKey = getGetApiAuthManageInfoQueryKey()

/** Resolves the signed-in user, or null when the session cookie is missing or expired. */
export async function fetchSession(queryClient: QueryClient) {
  try {
    return await queryClient.ensureQueryData(
      getGetApiAuthManageInfoQueryOptions(),
    )
  } catch (error) {
    if (isUnauthorized(error)) {
      return null
    }
    throw error
  }
}
