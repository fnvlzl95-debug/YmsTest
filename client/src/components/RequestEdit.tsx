import { useEffect, useMemo, useState } from 'react'
import { RESERVATION_STATUSES } from '../Enums'
import MailSettingEdit from './MailSettingEdit'
import TimeScheduleEdit from './TimeScheduleEdit'

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

const createDefaultForm = (employees, equipments, defaultEmpNo, initialReceiverUserIds = []) => {
  const defaultEmployee = employees.find((employee) => employee.empNo === defaultEmpNo) ?? employees[0]
  const defaultEquipment = equipments[0]

  return {
    equipmentId: defaultEquipment ? defaultEquipment.id : '',
    empNo: defaultEmployee ? defaultEmployee.empNo : '',
    empName: defaultEmployee ? defaultEmployee.name : '',
    reservedDate: '',
    purpose: '',
    status: RESERVATION_STATUSES[0],
    receiverUserIds: initialReceiverUserIds,
  }
}

function RequestEdit({
  open,
  mode,
  reservation,
  employees,
  equipments,
  defaultEmpNo,
  receiverCandidates,
  initialReceiverUserIds,
  saving,
  onClose,
  onSubmit,
  onDelete,
}) {
  const [form, setForm] = useState(createDefaultForm(employees, equipments, defaultEmpNo))

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
        empNo: reservation.empNum,
        empName: reservation.empName,
        reservedDate: toInputDateTime(reservation.reservedDate),
        purpose: reservation.purpose,
        status: reservation.status,
        receiverUserIds: initialReceiverUserIds,
      })

      return
    }

    setForm(createDefaultForm(employees, equipments, defaultEmpNo, []))
  }, [open, mode, reservation, employees, equipments, defaultEmpNo, initialReceiverUserIds])

  const handleEmployeeChange = (empNo) => {
    const employee = employeeByEmpNo.get(empNo)

    setForm((prev) => ({
      ...prev,
      empNo,
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
      empNum: form.empNo,
      empName: form.empName,
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
        <div className="drawer-header">OPENLAB 예약 {mode === 'edit' ? '수정' : '등록'}</div>

        <form className="drawer-form" onSubmit={handleSubmit}>
          {mode === 'edit' && reservation && (
            <label>
              ISSUE_NO
              <input type="text" value={reservation.issueNo} disabled />
            </label>
          )}

          <label>
            사용자
            <select
              value={form.empNo}
              onChange={(event) => handleEmployeeChange(event.target.value)}
              required
            >
              {employees.map((employee) => (
                <option key={employee.empNo} value={employee.empNo}>
                  {employee.name} ({employee.empNo})
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

          <TimeScheduleEdit
            reservedDate={form.reservedDate}
            purpose={form.purpose}
            status={form.status}
            statuses={RESERVATION_STATUSES}
            onReservedDateChange={(event) => setForm((prev) => ({ ...prev, reservedDate: event.target.value }))}
            onPurposeChange={(event) => setForm((prev) => ({ ...prev, purpose: event.target.value }))}
            onStatusChange={(event) => setForm((prev) => ({ ...prev, status: event.target.value }))}
          />

          <MailSettingEdit
            receiverCandidates={receiverCandidates}
            selectedReceiverUserIds={form.receiverUserIds}
            onToggleReceiver={toggleReceiver}
          />

          <div className="drawer-actions">
            <button type="submit" className="btn btn-primary" disabled={saving}>
              {saving ? '저장 중...' : mode === 'edit' ? 'Save' : 'Add'}
            </button>

            {mode === 'edit' && reservation && (
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

export default RequestEdit
