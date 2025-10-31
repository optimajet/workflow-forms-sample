import {useCallback, useEffect, useMemo, useState} from 'react'
import {Link} from 'react-router-dom'
import {Pagination, SelectPicker, Table} from 'rsuite'
import {AppProps, camelCaseToWords, useAppState, useShowError} from '../types.ts'
import {CreateReportButton} from './CreateReportButton.tsx'

const {Column, HeaderCell, Cell} = Table

type Report = Record<string, unknown>

const defaultPageSize = 10

const pageSizeOptions = [
  {label: '10 items', value: 10},
  {label: '20 items', value: 20},
  {label: '50 items', value: 50},
  {label: '100 items', value: 100},
]

export const WeeklyReportsPage = (props: AppProps) => {
  const {apiUrl} = props
  const {selectedUser} = useAppState()
  const [reports, setReports] = useState<Report[]>([])
  const [columns, setColumns] = useState<string[]>([])
  const [loading, setLoading] = useState(false)
  const [total, setTotal] = useState(0)
  const [page, setPage] = useState(1)
  const [pageSize, setPageSize] = useState(defaultPageSize)
  const showError = useShowError()

  const fetchReports = useCallback(async () => {
    if (selectedUser === null) return
    setLoading(true)
    try {
      const skip = (page - 1) * pageSize
      const queryParams = new URLSearchParams({
        user: selectedUser,
        skip: skip.toString(),
        take: pageSize.toString(),
      })

      const [dataRes, countRes] = await Promise.all([
        fetch(`${apiUrl}/reports/data/query?${queryParams}`)
          .then(res => res.json())
          .catch(error => showError('Failed to load reports: ' + error.message)),
        fetch(`${apiUrl}/reports/data/count?user=${encodeURIComponent(selectedUser)}`)
          .then(res => res.json())
          .catch(error => showError('Failed to load reports: ' + error.message)),
      ])

      setReports(dataRes)
      setTotal(countRes)

      if (dataRes.length > 0) {
        const allKeys = new Set<string>();
        dataRes.forEach((row: Report) => {
          Object.keys(row).forEach(key => allKeys.add(key));
        });
        
        setColumns(Array.from(allKeys))
      }
    } finally {
      setLoading(false)
    }
  }, [page, pageSize, selectedUser, showError, apiUrl])

  useEffect(() => {
    fetchReports().catch(error => console.error('Error fetching reports:', error))
  }, [fetchReports])

  const handlePageSizeChange = (value: number | null) => {
    if (value !== null) {
      setPageSize(value)
      setPage(1)
    }
  }

  const tableColumns = useMemo(() => columns.map((key) => (
    <Column key={key} flexGrow={1} align="left" fullText>
      <HeaderCell>{camelCaseToWords(key)}</HeaderCell>
      <Cell>
        {(rowData: Report) => {
          const value = rowData[key]
          if (value === null || value === undefined) return '-'

          if (key === 'id') {
            return (
              <Link to={`/weeklyReport/${value}`}>Details</Link>
            )
          }

          if (typeof value === 'string' && /\d{4}-\d{2}-\d{2}T/.test(value)) {
            return new Date(value).toLocaleDateString()
          }
          return String(value)
        }}
      </Cell>
    </Column>
  )), [columns])

  return (<div style={{display: 'flex', flexDirection: 'column', height: '100%', overflow: 'hidden'}}>
      <CreateReportButton {...props}></CreateReportButton>

      <div style={{flexGrow: 1, overflow: 'auto'}}>
        <Table data={reports} loading={loading} autoHeight={true} bordered cellBordered hover>
          {tableColumns}
        </Table>
      </div>

      <div style={{marginTop: 15}}>
        <div style={{display: 'flex', justifyContent: 'space-between', alignItems: 'center', flexWrap: 'wrap', gap: 12}}>
          <div style={{display: 'flex', alignItems: 'center', gap: 16, flexWrap: 'wrap'}}>
            <Pagination
              prev
              next
              first
              last
              ellipsis
              boundaryLinks
              total={total}
              limit={pageSize}
              maxButtons={5}
              activePage={page}
              onChangePage={setPage}
            />
            <span>Total: {total}</span>
          </div>
          <div style={{display: 'flex', alignItems: 'center', gap: 8}}>
            <span>Items per page:</span>
            <SelectPicker
              data={pageSizeOptions}
              value={pageSize}
              onChange={handlePageSizeChange}
              cleanable={false}
              searchable={false}
              preventOverflow={true}
              style={{width: 120}}
              placement="bottomEnd"
            />
          </div>
        </div>
      </div>
    </div>
  )
}
