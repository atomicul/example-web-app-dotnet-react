import type { HttpValidationProblemDetails } from './generated/model'

/** A non-success response, carrying the ProblemDetails body when the API sent one. */
export class ApiError extends Error {
  readonly status: number
  readonly problem: HttpValidationProblemDetails | undefined

  constructor(status: number, problem?: HttpValidationProblemDetails) {
    super(
      problem?.detail ??
        problem?.title ??
        `Request failed with status ${status}`,
    )
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
  }

  /** Validation messages keyed by field, empty when the response had none. */
  get errors(): Record<string, string[]> {
    return this.problem?.errors ?? {}
  }
}

/** A message fit for the user, listing every validation error when there are any. */
export function errorMessage(error: unknown): string {
  if (error instanceof ApiError) {
    const messages = Object.values(error.errors).flat()
    return messages.length > 0 ? messages.join(' ') : error.message
  }
  return error instanceof Error ? error.message : 'Something went wrong.'
}

export function isUnauthorized(error: unknown): boolean {
  return error instanceof ApiError && error.status === 401
}

export type ErrorType<_Error> = ApiError

async function readJson(response: Response): Promise<unknown> {
  const isJson = response.headers.get('content-type')?.includes('json')
  return isJson ? response.json() : undefined
}

/** Orval mutator: same-origin fetch that resolves the JSON body or throws an {@link ApiError}. */
export async function fetcher<T>(url: string, init: RequestInit): Promise<T> {
  const response = await fetch(url, init)
  const body = await readJson(response)
  if (!response.ok) {
    throw new ApiError(
      response.status,
      body as HttpValidationProblemDetails | undefined,
    )
  }
  return body as T
}
