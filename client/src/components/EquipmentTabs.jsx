function EquipmentTabs({ tabs, selectedTab, onSelectTab }) {
  return (
    <div className="equipment-tabs" role="tablist" aria-label="점검유형 탭">
      {tabs.map((tab) => {
        const isActive = selectedTab === tab.id

        return (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={isActive}
            className={`tab-item ${isActive ? 'active' : ''}`}
            onClick={() => onSelectTab(tab.id)}
          >
            {tab.label}
          </button>
        )
      })}
    </div>
  )
}

export default EquipmentTabs
