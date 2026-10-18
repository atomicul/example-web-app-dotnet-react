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

/** The field a JSON Pointer such as `#/dueDate` names, or undefined when it names none. */
function fieldOf(pointer: string | undefined) {
  return pointer?.replace(/^#\//, '')
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
  const validationErrors = error instanceof ApiError ? error.errors : []
  const rootMessages =
    validationErrors.length === 0 ? [errorMessage(error)] : []
  const fieldMessages = new Map<string, string[]>()

  for (const { pointer, message } of validationErrors) {
    const field = fieldOf(pointer)
    if (field && fields.includes(field)) {
      fieldMessages.set(field, [...(fieldMessages.get(field) ?? []), message])
    } else {
      rootMessages.push(message)
    }
  }

  for (const [field, messages] of fieldMessages) {
    form.setError(field as FieldPath<T>, { message: messages.join(' ') })
  }

  if (rootMessages.length > 0) {
    form.setError('root', { message: rootMessages.join(' ') })
  }
}
