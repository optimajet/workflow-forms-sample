import {AppProps, useAppState} from '../types.ts'
import WorkflowDesigner from '@optimajet/workflow-designer-react'

export function DesignerPage({schemeCode, apiUrl}: AppProps) {
  const {selectedUser} = useAppState()
  if (selectedUser === null) return <>Select a user first</>

  const designerConfig = {
    renderTo: 'wfdesigner',
    apiurl: `${apiUrl.replace(/\/$/, '')}/workflow-api/designer`,
    showSaveButton: true,
    name: 'wfe',
    language: 'en'
  }
  return <WorkflowDesigner
    schemeCode={schemeCode}
    designerConfig={designerConfig}
    readOnly={false}
  />
}
