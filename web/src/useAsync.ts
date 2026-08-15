import { useEffect, useState } from 'react'

interface AsyncState<T> {
  data: T | null
  error: Error | null
  loading: boolean
}

/**
 * Minimal data-fetching hook: runs `load` whenever `deps` change and tracks
 * loading/error state. Deliberately not a full query library - this app has two
 * read-only screens, so React Query would be more machinery than the problem needs.
 */
export function useAsync<T>(load: () => Promise<T>, deps: unknown[]): AsyncState<T> {
  const [state, setState] = useState<AsyncState<T>>({ data: null, error: null, loading: true })

  useEffect(() => {
    // Guards against a resolved promise from a previous member overwriting the
    // current one if the user navigates before the first request settles.
    let cancelled = false
    setState({ data: null, error: null, loading: true })

    load()
      .then((data) => {
        if (!cancelled) setState({ data, error: null, loading: false })
      })
      .catch((error: Error) => {
        if (!cancelled) setState({ data: null, error, loading: false })
      })

    return () => {
      cancelled = true
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, deps)

  return state
}
