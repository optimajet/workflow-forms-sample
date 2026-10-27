import {createRoot} from 'react-dom/client'
import App from './App.tsx'
import 'rsuite/dist/rsuite.min.css'
import '@optimajet/workflow-forms-manager/index.css'
import '@optimajet/workflow-forms-viewer/index.css'
import {appConfig} from './config.ts'

createRoot(document.getElementById('root')!).render(
  <App {...appConfig}/>
)
