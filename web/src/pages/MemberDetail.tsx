import { Link, useParams } from 'react-router-dom'
import { ApiError, getApplicant } from '../api'
import { describeOutcome, formatDate, formatDateTime, formatMoney } from '../format'
import { useAsync } from '../useAsync'

export function MemberDetail() {
  const { memberNumber = '' } = useParams<{ memberNumber: string }>()
  const { data, error, loading } = useAsync(() => getApplicant(memberNumber), [memberNumber])

  if (loading) return <p className="status">Loading {memberNumber}…</p>

  if (error) {
    const notFound = error instanceof ApiError && error.status === 404
    return (
      <>
        <Link className="back-link" to="/">
          ← All members
        </Link>
        <p className="status status-error">
          {notFound ? `No member found with number ${memberNumber}.` : error.message}
        </p>
      </>
    )
  }

  if (!data) return null

  const approved = data.applications.filter((a) => a.outcome === 'Approved').length

  return (
    <>
      <Link className="back-link" to="/">
        ← All members
      </Link>

      <header className="page-header">
        <h1>{data.fullName}</h1>
        <p className="subtitle">
          {data.memberNumber} · member since {formatDate(data.memberSince)}
        </p>
      </header>

      <dl className="facts">
        <div>
          <dt>Monthly income</dt>
          <dd>{formatMoney(data.monthlyIncome)}</dd>
        </div>
        <div>
          <dt>Open loans</dt>
          <dd>{data.openLoanCount}</dd>
        </div>
        <div>
          <dt>Standing</dt>
          <dd>{data.hasRecentDelinquency ? 'Recent delinquency' : 'Clear'}</dd>
        </div>
        <div>
          <dt>Applications</dt>
          <dd>
            {data.applications.length}
            {data.applications.length > 0 && (
              <span className="muted"> · {approved} approved</span>
            )}
          </dd>
        </div>
      </dl>

      <h2>Submitted applications</h2>

      {data.applications.length === 0 ? (
        <p className="status">
          This member has not submitted any applications yet. Submit one via{' '}
          <code>POST /api/loanapplications</code> and it will appear here.
        </p>
      ) : (
        <ul className="applications">
          {data.applications.map((application) => {
            const outcome = describeOutcome(application.outcome)

            return (
              <li key={application.loanApplicationId} className="application">
                <div className="application-head">
                  <div>
                    <span className="amount">{formatMoney(application.requestedAmount)}</span>
                    <span className="muted">
                      {' '}
                      · application #{application.loanApplicationId}
                    </span>
                  </div>
                  <span className={`badge badge-${outcome.tone}`}>{outcome.label}</span>
                </div>

                {application.reasoning && <p className="reasoning">{application.reasoning}</p>}

                <div className="application-meta">
                  <span>Submitted {formatDateTime(application.submittedAtUtc)}</span>
                  {application.evaluationDurationMs !== null && (
                    <span>Evaluated in {application.evaluationDurationMs} ms</span>
                  )}
                  {application.fundedAtUtc ? (
                    <span className="funded">Funded {formatDateTime(application.fundedAtUtc)}</span>
                  ) : (
                    <span className="muted">Not funded</span>
                  )}
                </div>
              </li>
            )
          })}
        </ul>
      )}
    </>
  )
}
