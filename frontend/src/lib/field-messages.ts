import type { z } from 'zod'

/**
 * Zod error option that reports every issue of a field in `messages` with the field's message, as a form does to
 * explain the rules of a pattern, since zod's own wording for one is unreadable. Other fields keep zod's message.
 */
export function fieldMessages(
  messages: Partial<Record<string, string>>,
): z.core.$ZodErrorMap {
  return (issue) => messages[String(issue.path?.[0])]
}
