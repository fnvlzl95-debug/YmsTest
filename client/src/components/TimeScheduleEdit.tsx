function TimeScheduleEdit({
  reservedDate,
  purpose,
  status,
  statuses,
  onReservedDateChange,
  onPurposeChange,
  onStatusChange,
}) {
  return (
    <>
      <label>
        예약일시
        <input type="datetime-local" value={reservedDate} onChange={onReservedDateChange} required />
      </label>

      <label>
        목적
        <input type="text" value={purpose} onChange={onPurposeChange} placeholder="예약 목적" required />
      </label>

      <label>
        상태
        <select value={status} onChange={onStatusChange} required>
          {statuses.map((statusValue) => (
            <option key={statusValue} value={statusValue}>
              {statusValue}
            </option>
          ))}
        </select>
      </label>
    </>
  )
}

export default TimeScheduleEdit
