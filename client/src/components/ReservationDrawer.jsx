import { useEffect, useMemo, useState } from 'react'

const toInputDateTime = (value) => {
  if (!value) {
    return ''
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return ''
  }

  const offset = date.getTimezoneOffset()
  const localDate = new Date(date.getTime() - offset * 60000)
  return localDate.toISOString().slice(0, 16)
}

const toApiDate = (value) => {
  if (!value) {
    return ''
  }

  const date = new Date(value)
  if (Number.isNaN(date.getTime())) {
    return ''
  }

  return date.toISOString()
}

const createDefaultForm = (employees, equipments, defaultEmpNum, initialReceiverUserIds = []) => {
  const defaultEmployee = employees.find((employee) => employee.empNo === defaultEmpNum) ?? employees[0]
  const firstEquipment = equipments[0]

  return {
    equipmentId: firstEquipment ? firstEquipment.id : '',
    empName: defaultEmployee ? defaultEmployee.name : '',
    empNum: defaultEmployee ? defaultEmployee.empNo : '',
    reservedDate: '',
    purpose: '',
    status: '대기',
    receiverUserIds: initialReceiverUserIds,
  }
}

function ReservationDrawer({
  open,
  mode,
  reservation,
  employees,
  equipments,
  defaultEmpNum,
  receiverCandidates,
  initialReceiverUserIds,
  saving,
  onClose,
  onSubmit,
  onDelete,
}) {
  const [form, setForm] = useState(createDefaultForm(employees, equipments, defaultEmpNum))

  const employeeByEmpNo = useMemo(() => {
    return new Map(employees.map((employee) => [employee.empNo, employee]))
  }, [employees])

  useEffect(() => {
    if (!open) {
      return
    }

    if (mode === 'edit' && reservation) {
      // eslint-disable-next-line react-hooks/set-state-in-effect
      setForm({
        equipmentId: reservation.equipmentId,
        empName: reservation.empName,
        empNum: reservation.empNum,
        reservedDate: toInputDateTime(reservation.reservedDate),
        purpose: reservation.purpose,
        status: reservation.status,
        receiverUserIds: initialReceiverUserIds,
      })

      return
    }

    setForm(createDefaultForm(employees, equipments, defaultEmpNum, []))
  }, [open, mode, reservation, employees, equipments, defaultEmpNum, initialReceiverUserIds])

  const handleEmployeeChange = (empNo) => {
    const employee = employeeByEmpNo.get(empNo)

    setForm((prev) => ({
      ...prev,
      empNum: empNo,
      empName: employee ? employee.name : '',
    }))
  }

  const toggleReceiver = (userId) => {
    setForm((prev) => {
      const exists = prev.receiverUserIds.includes(userId)
      const next = exists
        ? prev.receiverUserIds.filter((value) => value !== userId)
        : [...prev.receiverUserIds, userId]

      return {
        ...prev,
        receiverUserIds: next,
      }
    })
  }

  const handleSubmit = async (event) => {
    event.preventDefault()

    await onSubmit({
      equipmentId: Number(form.equipmentId),
      empName: form.empName,
      empNum: form.empNum,
      reservedDate: toApiDate(form.reservedDate),
      purpose: form.purpose,
      status: form.status,
      receiverUserIds: form.receiverUserIds,
    })
  }

  return (
    <>
      <div className={`drawer-overlay ${open ? 'show' : ''}`} onClick={onClose} />

      <aside className={`drawer-panel ${open ? 'open' : ''}`}>
        <div className="drawer-header">설비예약등록</div>

        <form className="drawer-form" onSubmit={handleSubmit}>
          <label>
            사용자
            <select
              value={form.empNum}
              onChange={(event) => handleEmployeeChange(event.target.value)}
              required
            >
              {employees.map((employee) => (
                <option key={employee.empNo} value={employee.empNo}>
                  {employee.name}
                </option>
              ))}
            </select>
          </label>

          <label>
            설비
            <select
              value={form.equipmentId}
              onChange={(event) => setForm((prev) => ({ ...prev, equipmentId: event.target.value }))}
              required
            >
              {equipments.map((equipment) => (
                <option key={equipment.id} value={equipment.id}>
                  {equipment.eqpId}
                </option>
              ))}
            </select>
          </label>

          <label>
            예약일시
            <input
              type="datetime-local"
              value={form.reservedDate}
              onChange={(event) => setForm((prev) => ({ ...prev, reservedDate: event.target.value }))}
              required
            />
          </label>

          <label>
            목적
            <input
              type="text"
              value={form.purpose}
              onChange={(event) => setForm((prev) => ({ ...prev, purpose: event.target.value }))}
              placeholder="예약 목적"
              required
            />
          </label>

          <label>
            상태
            <select
              value={form.status}
              onChange={(event) => setForm((prev) => ({ ...prev, status: event.target.value }))}
              required
            >
              <option value="대기">대기</option>
              <option value="승인">승인</option>
              <option value="반려">반려</option>
            </select>
          </label>

          <div className="receiver-section">
            <div className="receiver-label">수신자</div>
            <div className="receiver-list">
              {receiverCandidates.map((receiver) => (
                <label key={receiver.userId} className="receiver-item">
                  <input
                    type="checkbox"
                    checked={form.receiverUserIds.includes(receiver.userId)}
                    onChange={() => toggleReceiver(receiver.userId)}
                  />
                  <span>
                    {receiver.name} ({receiver.deptCode})
                  </span>
                </label>
              ))}
            </div>
          </div>

          <div className="drawer-actions">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? '저장 중...' : mode === 'edit' ? 'Save' : 'Add'}
            </button>

            {mode === 'edit' && (
              <button
                type="button"
                className="btn btn-outline"
                onClick={() => onDelete(reservation.id)}
                disabled={saving}
              >
                Delete
              </button>
            )}

            <button type="button" className="btn btn-secondary" onClick={onClose}>
              Close
            </button>
          </div>
        </form>
      </aside>
    </>
  )
}

export default ReservationDrawer
