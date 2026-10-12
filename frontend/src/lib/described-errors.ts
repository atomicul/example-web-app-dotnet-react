import { z } from 'zod'

/**
 * Zod error option that reports a failing field with its description, where the API describes the field's rules,
 * since zod's own wording for a pattern is unreadable. Fields without a description keep zod's message.
 */
export function describedErrors(schema: z.ZodType): z.core.$ZodErrorMap {
  return (issue) => {
    const field =
      schema instanceof z.ZodObject
        ? schema.shape[String(issue.path?.[0])]
        : undefined
    return field && z.globalRegistry.get(field)?.description
  }
}
