import { useMemo, useState } from 'react'
import { AUTH_TYPES } from '../Enums'

function AnaApprovalEdit({ site, rows, equipments, employees, saving, onCreate, onDelete }) {
  const [form, setForm] = useState({
    eqpName: '',
    authType: AUTH_TYPES[0].id,
    empNo: '',
  })

  const sortedEquipments = useMemo(() => {
    return [...equipments].sort((a, b) => a.eqpId.localeCompare(b.eqpId))
  }, [equipments])

  const sortedEmployees = useMemo(() => {
    return [...employees].sort((a, b) => a.name.localeCompare(b.name))
  }, [employees])

  const handleSubmit = async (event) => {
    event.preventDefault()

    if (!form.eqpName || !form.empNo) {
      return
    }

    await onCreate({
      site,
      eqpName: form.eqpName,
      authType: form.authType,
      empNo: form.empNo,
    })
  }

  return (
    <section className="panel-card">
      <div className="panel-title">권한 관리</div>

      <form className="inline-form" onSubmit={handleSubmit}>
        <select
          value={form.eqpName}
          onChange={(event) => setForm((prev) => ({ ...prev, eqpName: event.target.value }))}
          required
        >
          <option value="">설비 선택</option>
          {sortedEquipments.map((equipment) => (
            <option key={equipment.id} value={equipment.eqpId}>
              {equipment.eqpId}
            </option>
          ))}
        </select>

        <select
          value={form.authType}
          onChange={(event) => setForm((prev) => ({ ...prev, authType: event.target.value }))}
          required
        >
          {AUTH_TYPES.map((authType) => (
            <option key={authType.id} value={authType.id}>
              {authType.label}
            </option>
          ))}
        </select>

        <select
          value={form.empNo}
          onChange={(event) => setForm((prev) => ({ ...prev, empNo: event.target.value }))}
          required
        >
          <option value="">사용자 선택</option>
          {sortedEmployees.map((employee) => (
            <option key={employee.empNo} value={employee.empNo}>
              {employee.name} ({employee.empNo})
            </option>
          ))}
        </select>

        <button className="btn btn-primary" type="submit" disabled={saving}>
          {saving ? '처리중...' : '권한추가'}
        </button>
      </form>

      <div className="simple-table-wrap">
        <table className="simple-table">
          <thead>
            <tr>
              <th>SITE</th>
              <th>EQP</th>
              <th>권한</th>
              <th>사번</th>
              <th>사용자</th>
              <th>부서</th>
              <th></th>
            </tr>
          </thead>
          <tbody>
            {rows.length === 0 && (
              <tr>
                <td colSpan={7} className="status-cell">
                  권한 데이터가 없습니다.
                </td>
              </tr>
            )}

            {rows.map((row) => (
              <tr key={row.id}>
                <td>{row.site}</td>
                <td>{row.eqpName}</td>
                <td>{row.authType}</td>
                <td>{row.empNo}</td>
                <td>{row.empName}</td>
                <td>{row.deptCode}</td>
                <td>
                  <button
                    className="btn btn-outline"
                    type="button"
                    onClick={() => onDelete(row.id)}
                    disabled={saving}
                  >
                    삭제
                  </button>
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </section>
  )
}

export default AnaApprovalEdit
