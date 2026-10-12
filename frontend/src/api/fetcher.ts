import type { HttpValidationProblemDetails } from './generated/model'

/** A non-success response, carrying the ProblemDetails body when the API sent one. */
export class ApiError extends Error {
  readonly status: number
  readonly problem: HttpValidationProblemDetails | undefined
  /** Seconds the API asks to wait before trying again, sent with a 429. */
  readonly retryAfter: number | undefined

  constructor(
    status: number,
    problem?: HttpValidationProblemDetails,
    retryAfter?: number,
  ) {
    super(
      problem?.detail ??
        problem?.title ??
        `Request failed with status ${status}`,
    )
    this.name = 'ApiError'
    this.status = status
    this.problem = problem
    this.retryAfter = retryAfter
  }

  /** Validation messages keyed by field, empty when the response had none. */
  get errors(): Record<string, string[]> {
    return this.problem?.errors ?? {}
  }
}

/** The request never reached the API, as when the user is offline or the server is down. */
export class NetworkError extends Error {
  constructor(options?: ErrorOptions) {
    super(
      "Couldn't reach the server. Check your connection and try again.",
      options,
    )
    this.name = 'NetworkError'
  }
}

/** A message fit for the user, listing every validation error when there are any. */
export function errorMessage(error: unknown): string {
  if (isTooManyRequests(error)) {
    const wait = error.retryAfter ? `${error.retryAfter} seconds` : 'a moment'
    return `Whoa, slow down! Take a breather and try again in ${wait}.`
  }
  if (error instanceof ApiError) {
    const messages = Object.values(error.errors).flat()
    return messages.length > 0 ? messages.join(' ') : error.message
  }
  return error instanceof Error ? error.message : 'Something went wrong.'
}

export function isUnauthorized(error: unknown): boolean {
  return error instanceof ApiError && error.status === 401
}

export function isTooManyRequests(error: unknown): error is ApiError {
  return error instanceof ApiError && error.status === 429
}

export type ErrorType<_Error> = ApiError | NetworkError

async function readJson(response: Response): Promise<unknown> {
  const isJson = response.headers.get('content-type')?.includes('json')
  return isJson ? response.json() : undefined
}

/** Reads a Retry-After header given in seconds, the form the API sends. */
function readRetryAfter(response: Response): number | undefined {
  const seconds = Number(response.headers.get('retry-after') ?? undefined)
  return Number.isInteger(seconds) ? seconds : undefined
}

/** Fetches, turning the TypeError fetch rejects with when the request cannot be sent into a {@link NetworkError}. */
async function send(url: string, init: RequestInit): Promise<Response> {
  try {
    return await fetch(url, init)
  } catch (error) {
    throw error instanceof TypeError
      ? new NetworkError({ cause: error })
      : error
  }
}

/** Orval mutator: same-origin fetch that resolves the JSON body or throws an {@link ApiError} or {@link NetworkError}. */
export async function fetcher<T>(url: string, init: RequestInit): Promise<T> {
  const response = await send(url, init)
  const body = await readJson(response)
  if (!response.ok) {
    throw new ApiError(
      response.status,
      body as HttpValidationProblemDetails | undefined,
      readRetryAfter(response),
    )
  }
  return body as T
}
