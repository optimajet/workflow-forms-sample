import {AppProps} from '../types.ts'
// @ts-expect-error types for designer not defined
import WorkflowDesigner from '@optimajet/workflow-designer-react'

export function DesignerPage({schemeCode, apiUrl}: AppProps) {
  const designerConfig = {
    renderTo: 'wfdesigner',
    apiurl: `${apiUrl}/designer`,
    showSaveButton: true,
    name: 'wfe',
    language: 'en',
  }
  return <WorkflowDesigner
    schemeCode={schemeCode}
    designerConfig={designerConfig}
    readOnly={false}
  />
}
