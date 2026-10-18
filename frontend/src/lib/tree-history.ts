import type { RouterHistory } from '@tanstack/react-router'

/** The hrefs of the app's history entries behind an entry, oldest first, kept in the entry's state. */
type TreeState = { trail?: string[] }

function trailOf(state: object) {
  return (state as TreeState).trail ?? []
}

function pathnameOf(href: string) {
  return href.replace(/[?#].*$/, '').replace(/(.)\/$/, '$1')
}

/** Whether a page's path is nested under another's, as `/account/email` is under `/account` and `/`. */
function isBelow(pathname: string, ancestor: string) {
  const prefix = ancestor.endsWith('/') ? ancestor : `${ancestor}/`
  return pathname !== ancestor && pathname.startsWith(prefix)
}

/** Whether going to `href` returns to an entry left at `entry`: the same href, or a plain link to its page. */
function returnsTo(entry: string, href: string) {
  return (
    entry === href || (href === pathnameOf(href) && pathnameOf(entry) === href)
  )
}

/**
 * Makes the history follow the tree of pages that their paths nest, instead of every page visited, so going back
 * goes up. Going to a page nested under the current one adds an entry; going to a page behind goes back to it,
 * dropping the entries after it; going anywhere else replaces the current entry.
 */
export function withTreeNavigation(history: RouterHistory): RouterHistory {
  const { push, replace, go, flush } = history
  const trail = () => trailOf(history.location.state)

  history.push = (href, state, options) => {
    const current = history.location
    const behind = trail()
    const index = behind.findLastIndex((entry) => returnsTo(entry, href))
    if (index >= 0) {
      flush()
      go(index - behind.length, options)
    } else if (isBelow(pathnameOf(href), current.pathname)) {
      push(href, { ...state, trail: [...behind, current.href] }, options)
    } else {
      replace(href, { ...state, trail: behind }, options)
    }
  }

  history.replace = (href, state, options) =>
    replace(href, { ...state, trail: trail() }, options)

  return history
}
