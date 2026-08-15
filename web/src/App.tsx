import { BrowserRouter, Link, Route, Routes } from 'react-router-dom'
import { Logo } from './components/Logo'
import { MemberDetail } from './pages/MemberDetail'
import { MemberList } from './pages/MemberList'

export default function App() {
  return (
    <BrowserRouter>
      <div className="shell">
        <nav className="app-nav">
          {/* The logo doubles as the route home, which is the convention users
              expect from a masthead. */}
          <Link to="/" className="logo-link" aria-label="Alloya home">
            <Logo />
          </Link>
          <span className="brand-sub">Loan approval · member browser</span>
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
