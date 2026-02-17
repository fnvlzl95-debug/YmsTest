import DataGrid from './DataGrid'
import EquipmentTabs from './EquipmentTabs'
import FilterSidebar from './FilterSidebar'

function ReceptionEdit({
  lines,
  classes,
  selectedLines,
  selectedClasses,
  onToggleLine,
  onToggleClass,
  onResetFilters,
  purposeTabs,
  selectedPurpose,
  onSelectPurpose,
  rows,
  loading,
  onRowClick,
  onOpenCreate,
}) {
  return (
    <section className="openlap-resv-wrap">
      <div className="openlap-toolbar">
        <EquipmentTabs tabs={purposeTabs} selectedTab={selectedPurpose} onSelectTab={onSelectPurpose} />

        <button className="btn btn-primary" onClick={onOpenCreate}>
          예약 등록
        </button>
      </div>

      <div className="content-grid">
        <FilterSidebar
          lines={lines}
          classes={classes}
          selectedLines={selectedLines}
          selectedClasses={selectedClasses}
          onToggleLine={onToggleLine}
          onToggleClass={onToggleClass}
          onReset={onResetFilters}
        />

        <section className="grid-panel">
          <DataGrid rows={rows} loading={loading} onRowClick={onRowClick} />
        </section>
      </div>
    </section>
  )
}

export default ReceptionEdit
