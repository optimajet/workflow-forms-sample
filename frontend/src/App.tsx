import React, {useMemo} from 'react'
import {BrowserRouter, Route, Routes, useLocation, useNavigate} from 'react-router-dom'
import {Container, Content, Header, Nav, Navbar} from 'rsuite'
import {Toaster} from 'sonner'
import {DesignerPage} from './components/DesignerPage.tsx'
import {FormsManagerPage} from './components/FormsManagerPage.tsx'
import {UserSelector} from './components/UserSelector.tsx'
import {WeeklyReportPage} from './components/WeeklyReportPage.tsx'
import {WeeklyReportsPage} from './components/WeeklyReportsPage.tsx'
import {AppProps} from './types.ts'


const navigationItems = [
  {name: 'WeeklyReports', component: WeeklyReportsPage, path: '/'},
  {name: 'FormsManager', component: FormsManagerPage, path: '/formsManager'},
  {name: 'Designer', component: DesignerPage, path: '/designer'}
]

const allItems = [
  ...navigationItems,
  {name: 'WeeklyReport', component: WeeklyReportPage, path: '/weeklyReport/:id'},
]


function AppLayout(props: AppProps) {
  const navigate = useNavigate()
  const location = useLocation()

  const navs = useMemo(() => {
    return navigationItems.map(item => {
      const active = location.pathname === item.path
      const handleSelect = () => navigate(item.path)
      return <Nav.Item key={item.name} active={active} onSelect={handleSelect}>{item.name}</Nav.Item>
    })
  }, [navigate, location.pathname])

  const routes = useMemo(() => {
    return allItems.map(item => {
        return <Route key={item.name} path={item.path} element={React.createElement(item.component, props)}/>
      }
    )
  }, [props])

  return <Container style={{display: 'flex', flexDirection: 'column', height: '100vh'}}>
    <Header style={{
      height: '60px',
      background: 'var(--rs-navbar-default-bg)',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'space-between',
      padding: '0 20px'
    }}>
      <Navbar>
        <Nav>
          {navs}
        </Nav>
      </Navbar>
      <UserSelector {...props}/>
    </Header>
    <Content style={{flex: 1, overflow: 'hidden'}}>
      <Routes>
        {routes}
      </Routes>
    </Content>
  </Container>
}

function App({...props}: AppProps) {
  return <BrowserRouter>
    <Toaster richColors position="bottom-right"/>
    <AppLayout {...props}/>
  </BrowserRouter>
}

export default App
