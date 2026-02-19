/**
 * ReceptionEdit — OPENLAB 예약 메인 화면.
 *
 * ★ 실무 패턴 재현 포인트:
 *   1. ExpandPanel로 조건 영역이 접기/펼치기
 *   2. ConditionInput type="employee" → dataSource를 무시하고 Controls.GetEmployeeList를 호출
 *   3. ConditionInput type="select" → dataSource를 그대로 사용
 *   4. handleConditionChange(id, values) 패턴
 *   5. makeConditionDataSource / createConditionValue 패턴
 */
import { useCallback, useState } from 'react'
import ConditionInput from './ConditionInput'
import DataGrid from './DataGrid'
import EquipmentTabs from './EquipmentTabs'
import ExpandPanel from './ExpandPanel'
import FilterSidebar from './FilterSidebar'
import {
  DataInfo,
  Dictionary,
  createConditionValue,
  makeConditionDataSource,
} from '../lib'
import type { ConditionValue, ConditionChangeHandler } from '../lib'

function ReceptionEdit({
  lines,
  classes,
  selectedLines,
  selectedClasses,
  onToggleLine,
  onToggleClass,
  onResetFilters,
  onSearch,
  purposeTabs,
  selectedPurpose,
  onSelectPurpose,
  rows,
  loading,
  onRowClick,
  onOpenCreate,
}: any) {
  // ──────────────────────────────────────────────
  // ★ 실무 패턴: 조건(Condition) 상태 관리
  // conditions: 각 조건 Input의 현재 값
  // dataSources: 각 조건 Input의 데이터소스
  // ──────────────────────────────────────────────
  const [conditions, setConditions] = useState<Record<string, ConditionValue>>({
    empName: createConditionValue('employee'),
    adminUser: createConditionValue('select'),
    remark: createConditionValue('text'),
  })

  // ★ 실무 핵심: 개발자가 넘긴 dataSource
  // empName에 Main.GetAdmin을 넘겨도 type="employee"이면 ConditionInput 내부에서
  // Controls.GetEmployeeList로 덮어씀 → 이것이 디버깅 포인트!
  const dataSources = {
    empName: makeConditionDataSource('employee',
      new DataInfo('Main', 'GetAdmin', new Dictionary<string, any>(), 'General')
    ),
    adminUser: makeConditionDataSource('select',
      new DataInfo('Main', 'GetAdmin', new Dictionary<string, any>(), 'General')
    ),
    remark: makeConditionDataSource('text'),
  }

  // ★ 실무 패턴: handleConditionChange(id, values)
  const handleConditionChange: ConditionChangeHandler = useCallback((id, values) => {
    setConditions((prev) => ({
      ...prev,
      [id]: {
        ...prev[id],
        values,
      },
    }))
  }, [])

  return (
    <section className="openlap-resv-wrap">
      <div className="openlap-toolbar">
        <EquipmentTabs tabs={purposeTabs} selectedTab={selectedPurpose} onSelectTab={onSelectPurpose} />

        <button className="btn btn-primary" onClick={onOpenCreate}>
          예약 등록
        </button>
      </div>

      {/* ★ 실무 패턴: ExpandPanel + ConditionInput 조건 영역 */}
      <div className="condition-panel-area">
        <ExpandPanel title="조건" defaultExpanded>
          <div className="condition-row">
            <ConditionInput
              id="empName"
              label="사원명"
              type="employee"
              dataSource={dataSources.empName.dataInfo}
              values={conditions.empName.values}
              onChange={handleConditionChange}
            />
            <ConditionInput
              id="adminUser"
              label="관리자"
              type="select"
              dataSource={dataSources.adminUser.dataInfo}
              values={conditions.adminUser.values}
              onChange={handleConditionChange}
            />
            <ConditionInput
              id="remark"
              label="비고"
              type="text"
              values={conditions.remark.values}
              onChange={handleConditionChange}
            />
          </div>
        </ExpandPanel>
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
          onSearch={onSearch}
        />

        <section className="grid-panel">
          <DataGrid rows={rows} loading={loading} onRowClick={onRowClick} />
        </section>
      </div>
    </section>
  )
}

export default ReceptionEdit
