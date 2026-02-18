const FilterSection = ({ title, items, selectedItems, onToggle }) => (
  <section className="filter-section">
    <h3>{title}</h3>
    <div className="filter-options">
      {items.map((item) => (
        <label key={item} className="filter-option">
          <input
            type="checkbox"
            checked={selectedItems.includes(item)}
            onChange={() => onToggle(item)}
          />
          <span>{item}</span>
        </label>
      ))}
    </div>
  </section>
)

function FilterSidebar({
  lines,
  classes,
  selectedLines,
  selectedClasses,
  onToggleLine,
  onToggleClass,
  onReset,
  onSearch,
}) {
  return (
    <aside className="filter-sidebar">
      <FilterSection
        title="라인"
        items={lines}
        selectedItems={selectedLines}
        onToggle={onToggleLine}
      />

      <FilterSection
        title="공정"
        items={classes}
        selectedItems={selectedClasses}
        onToggle={onToggleClass}
      />

      <div className="filter-actions">
        <button className="btn btn-primary search-btn" onClick={onSearch}>
          조회
        </button>
        <button className="btn btn-outline return-btn" onClick={onReset}>
          리턴
        </button>
      </div>
    </aside>
  )
}

export default FilterSidebar
