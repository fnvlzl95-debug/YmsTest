function MailSettingEdit({ receiverCandidates, selectedReceiverUserIds, onToggleReceiver }) {
  return (
    <div className="receiver-section">
      <div className="receiver-label">수신자</div>
      <div className="receiver-list">
        {receiverCandidates.map((receiver) => (
          <label key={receiver.userId} className="receiver-item">
            <input
              type="checkbox"
              checked={selectedReceiverUserIds.includes(receiver.userId)}
              onChange={() => onToggleReceiver(receiver.userId)}
            />
            <span>
              {receiver.name} ({receiver.deptCode})
            </span>
          </label>
        ))}

        {receiverCandidates.length === 0 && <div className="status-cell">수신자 후보가 없습니다.</div>}
      </div>
    </div>
  )
}

export default MailSettingEdit
