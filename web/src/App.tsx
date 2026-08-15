import { BrowserRouter, Route, Routes } from 'react-router-dom'
import { MemberDetail } from './pages/MemberDetail'
import { MemberList } from './pages/MemberList'

export default function App() {
  return (
    <BrowserRouter>
      <div className="shell">
        <nav className="app-nav">
          <span className="brand">Loan Approval</span>
          <span className="brand-sub">Member browser</span>
        </nav>
        <main>
          <Routes>
            <Route path="/" element={<MemberList />} />
            {/* Member number is the route key rather than the surrogate Id,
                because that is what the API's lookup endpoint takes. */}
            <Route path="/members/:memberNumber" element={<MemberDetail />} />
            <Route path="*" element={<p className="status">Page not found.</p>} />
          </Routes>
        </main>
      </div>
    </BrowserRouter>
  )
}
