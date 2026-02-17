const formatDate = (value) => {
  if (!value) {
    return '-'
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return value
  }

  return new Intl.DateTimeFormat('ko-KR', {
    year: '2-digit',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit',
  }).format(date)
}

function DataGrid({ rows, loading, onRowClick }) {
  return (
    <div className="data-grid-wrapper">
      <table className="data-grid">
        <thead>
          <tr>
            <th>EQP_ID</th>
            <th>LINE</th>
            <th>CLASS</th>
            <th>사용자</th>
            <th>예약일시</th>
            <th>상태</th>
          </tr>
        </thead>
        <tbody>
          {loading && (
            <tr>
              <td colSpan={6} className="status-cell">
                로딩 중...
              </td>
            </tr>
          )}

          {!loading && rows.length === 0 && (
            <tr>
              <td colSpan={6} className="status-cell">
                조회 결과가 없습니다.
              </td>
            </tr>
          )}

          {!loading &&
            rows.map((row) => (
              <tr key={row.id} onClick={() => onRowClick(row)}>
                <td>{row.eqpId}</td>
                <td>{row.lineId}</td>
                <td>{row.largeClass}</td>
                <td>{row.empName}</td>
                <td>{formatDate(row.reservedDate)}</td>
                <td>{row.status}</td>
              </tr>
            ))}
        </tbody>
      </table>
    </div>
  )
}

export default DataGrid
