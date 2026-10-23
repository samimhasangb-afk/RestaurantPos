import { Navigate, Route, Routes } from 'react-router-dom'
import { homeFor, useAuth } from './auth'
import { Shell } from './ui'
import { AdminPage } from './pages/Admin'
import { ApprovalsPage } from './pages/Approvals'
import { CustomersPage } from './pages/Customers'
import { DashboardPage } from './pages/Dashboard'
import { DeliveryPage } from './pages/Delivery'
import { ExpensesPage } from './pages/Expenses'
import { InventoryPage } from './pages/Inventory'
import { KitchenPage } from './pages/Kitchen'
import { LoginPage } from './pages/Login'
import { MenuPage } from './pages/Menu'
import { OrdersPage } from './pages/Orders'
import { PosPage } from './pages/Pos'
import { ProcurementPage } from './pages/Procurement'
import { ReceiptsPage } from './pages/Receipts'
import { ReportsPage } from './pages/Reports'
import { TablesPage } from './pages/Tables'
import { VerifyPage } from './pages/Verify'

function Gate({ children }: { children: React.ReactNode }) {
  const { user, ready } = useAuth()
  if (!ready) return <div className="content">Opening Saffron…</div>
  if (!user) return <Navigate to="/login" replace />
  return <Shell>{children}</Shell>
}

export default function App() {
  const { user, ready } = useAuth()
  return (
    <Routes>
      <Route path="/login" element={ready && user ? <Navigate to={homeFor(user)} replace /> : <LoginPage />} />
      <Route path="/r/:token" element={<VerifyPage />} />
      <Route path="/" element={<Gate><DashboardPage /></Gate>} />
      <Route path="/pos" element={<Gate><PosPage /></Gate>} />
      <Route path="/orders" element={<Gate><OrdersPage /></Gate>} />
      <Route path="/kitchen" element={<Gate><KitchenPage /></Gate>} />
      <Route path="/tables" element={<Gate><TablesPage /></Gate>} />
      <Route path="/receipts" element={<Gate><ReceiptsPage /></Gate>} />
      <Route path="/delivery" element={<Gate><DeliveryPage /></Gate>} />
      <Route path="/menu" element={<Gate><MenuPage /></Gate>} />
      <Route path="/inventory" element={<Gate><InventoryPage /></Gate>} />
      <Route path="/procurement" element={<Gate><ProcurementPage /></Gate>} />
      <Route path="/customers" element={<Gate><CustomersPage /></Gate>} />
      <Route path="/expenses" element={<Gate><ExpensesPage /></Gate>} />
      <Route path="/reports" element={<Gate><ReportsPage /></Gate>} />
      <Route path="/approvals" element={<Gate><ApprovalsPage /></Gate>} />
      <Route path="/admin" element={<Gate><AdminPage /></Gate>} />
      <Route path="*" element={<Navigate to="/" replace />} />
    </Routes>
  )
}
