import {createRoot} from 'react-dom/client'
import App from './App.tsx'
import 'rsuite/dist/rsuite.min.css'
import {AppProps} from './types.ts'

const appProps: AppProps = {
  apiUrl: 'https://localhost:5141/api',
  schemeCode: 'WeeklyReportProcess',
  //licenseKey: ''
}


createRoot(document.getElementById('root')!).render(
  <App {...appProps}/>
)
