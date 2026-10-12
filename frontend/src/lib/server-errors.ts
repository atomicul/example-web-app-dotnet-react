import type { FieldPath, FieldValues, UseFormSetError } from 'react-hook-form'
import { ApiError, NetworkError, errorMessage } from '@/api/fetcher'

type ErrorTarget<T extends FieldValues> = {
  getValues: () => T
  setError: UseFormSetError<T>
}

/** A failed submission the caller explains in its own words, as a login form does for wrong credentials. */
export class FormError extends Error {
  constructor(message: string) {
    super(message)
    this.name = 'FormError'
  }
}

/** Maps a server validation key such as `DueDate` onto the camelCase name the form uses. */
function toFieldName(key: string): string {
  return key.charAt(0).toLowerCase() + key.slice(1)
}

/**
 * Shows a failed submission on the form: validation errors for the form's fields go under them,
 * other failures of the request and {@link FormError}s go to the form's root error. Any other error is a bug and
 * is rethrown.
 */
export function setServerErrors<T extends FieldValues>(
  form: ErrorTarget<T>,
  error: unknown,
) {
  if (!(
    error instanceof ApiError ||
    error instanceof NetworkError ||
    error instanceof FormError
  )) {
    throw error
  }
  const fields = Object.keys(form.getValues())
  const fieldErrors =
    error instanceof ApiError ? Object.entries(error.errors) : []
  const rootMessages = fieldErrors.length === 0 ? [errorMessage(error)] : []

  for (const [key, messages] of fieldErrors) {
    const field = toFieldName(key)
    if (fields.includes(field)) {
      form.setError(field as FieldPath<T>, { message: messages.join(' ') })
    } else {
      rootMessages.push(...messages)
    }
  }

  if (rootMessages.length > 0) {
    form.setError('root', { message: rootMessages.join(' ') })
  }
}
