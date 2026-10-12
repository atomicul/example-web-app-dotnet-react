import { useQueryClient } from '@tanstack/react-query'
import { getGetTodosQueryKey } from '@/api/generated'

/** Refetches the todo list after a change, so it always mirrors the server. */
export function useInvalidateTodos() {
  const queryClient = useQueryClient()
  return () =>
    queryClient.invalidateQueries({ queryKey: getGetTodosQueryKey() })
}
