import {useCallback, useState} from 'react'
import {useNavigate} from 'react-router-dom'
import {Button} from 'rsuite'
import {AppProps, useAppState, useShowError} from '../types.ts'

export function CreateReportButton({apiUrl}: AppProps) {
  const {selectedUser} = useAppState()
  const navigate = useNavigate()
  const showError = useShowError()
  const [loading, setLoading] = useState(false)

  const handleClick = useCallback(async () => {
    setLoading(true)
    const data = {
      user: selectedUser
    }

    try {
      const response = await fetch(`${apiUrl}/reports/forms/submit`, {
        method: 'POST',
        headers: {
          'Content-Type': 'application/json'
        },
        body: JSON.stringify(data)
      })

      if (response.ok) {
        const {processId} = await response.json()
        navigate(`/weeklyReport/${processId}`)
      } else {
        const errorText = await response.text()
        showError(Error(errorText || `HTTP error ${response.status}`))
      }
    } catch (error) {
      showError(error as Error)
    } finally {
      setLoading(false)
    }
  }, [apiUrl, navigate, selectedUser, showError])

  return (
    <Button appearance="primary" color="red" loading={loading} onClick={handleClick}>
      Create new report
    </Button>
  )
}
