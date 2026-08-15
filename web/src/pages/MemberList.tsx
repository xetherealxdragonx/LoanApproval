import { Link } from 'react-router-dom'
import { getApplicants } from '../api'
import { formatDate, formatMoney } from '../format'
import { useAsync } from '../useAsync'

export function MemberList() {
  const { data, error, loading } = useAsync(getApplicants, [])

  if (loading) return <p className="status">Loading members…</p>
  if (error) return <p className="status status-error">Could not load members: {error.message}</p>
  if (!data?.length) return <p className="status">No members found. Is the API seeded?</p>

  return (
    <>
      <header className="page-header">
        <h1>Members</h1>
        <p className="subtitle">
          {data.length} member{data.length === 1 ? '' : 's'} · select one to review their
          applications
        </p>
      </header>

      <table className="grid">
        <thead>
          <tr>
            <th>Member</th>
            <th>Name</th>
            <th className="numeric">Monthly income</th>
            <th className="numeric">Open loans</th>
            <th>Standing</th>
            <th className="numeric">Applications</th>
          </tr>
        </thead>
        <tbody>
          {data.map((member) => (
            <tr key={member.id}>
              <td>
                <Link className="member-link" to={`/members/${member.memberNumber}`}>
                  {member.memberNumber}
                </Link>
                <span className="muted-since">since {formatDate(member.memberSince)}</span>
              </td>
              <td>{member.fullName}</td>
              <td className="numeric">{formatMoney(member.monthlyIncome)}</td>
              <td className="numeric">{member.openLoanCount}</td>
              <td>
                {member.hasRecentDelinquency ? (
                  <span className="badge badge-review">Delinquency flag</span>
                ) : (
                  <span className="badge badge-approved">Clear</span>
                )}
              </td>
              <td className="numeric">{member.applicationCount}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </>
  )
}
