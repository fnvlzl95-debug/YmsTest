function AnaResultEdit({ rows }) {
  return (
    <section className="panel-card">
      <div className="panel-title">설비 관리</div>

      <div className="simple-table-wrap">
        <table className="simple-table">
          <thead>
            <tr>
              <th>EQP_ID</th>
              <th>LINE</th>
              <th>CLASS</th>
              <th>TYPE</th>
              <th>GROUP</th>
              <th>예약건수</th>
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 && (
              <tr>
                <td colSpan={6} className="status-cell">
                  설비 데이터가 없습니다.
                </td>
              </tr>
            )}

            {rows.map((row) => (
              <tr key={row.id}>
                <td>{row.eqpId}</td>
                <td>{row.lineId}</td>
                <td>{row.largeClass}</td>
                <td>{row.eqpType}</td>
                <td>{row.eqpGroupName}</td>
                <td>{row.reservationCount}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

export default AnaResultEdit
