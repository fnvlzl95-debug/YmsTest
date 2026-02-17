import { useCallback, useEffect, useMemo, useState } from 'react'
import './styles/variables.css'
import './App.css'
import {
  checkReceptionAuth,
  createReservation,
  deleteReservation,
  getClasses,
  getEmployees,
  getEquipments,
  getLines,
  getNotificationReceivers,
  getReservationById,
  getReservations,
  saveSearchHistory,
  updateReservation,
} from './api/api'
import DataGrid from './components/DataGrid'
import EquipmentTabs from './components/EquipmentTabs'
import FilterSidebar from './components/FilterSidebar'
import ReservationDrawer from './components/ReservationDrawer'

const TABS = [
  { id: 'ALL', label: '전체' },
  { id: 'ESD 측정', label: 'ESD 측정' },
  { id: 'P-TURN', label: 'P-TURN' },
]

const SITES = ['HQ', 'FAB']

const toggleSelection = (items, value) => {
  if (items.includes(value)) {
    return items.filter((item) => item !== value)
  }

  return [...items, value]
}

function App() {
  const [lines, setLines] = useState([])
  const [allClasses, setAllClasses] = useState([])
  const [classes, setClasses] = useState([])
  const [equipments, setEquipments] = useState([])
  const [employees, setEmployees] = useState([])
  const [reservations, setReservations] = useState([])

  const [selectedLines, setSelectedLines] = useState([])
  const [selectedClasses, setSelectedClasses] = useState([])
  const [selectedTab, setSelectedTab] = useState('ALL')

  const [bizSite, setBizSite] = useState('HQ')
  const [currentUserEmpNo, setCurrentUserEmpNo] = useState('')

  const [loadingReservations, setLoadingReservations] = useState(false)
  const [saving, setSaving] = useState(false)
  const [errorMessage, setErrorMessage] = useState('')

  const [drawerOpen, setDrawerOpen] = useState(false)
  const [drawerMode, setDrawerMode] = useState('create')
  const [activeReservation, setActiveReservation] = useState(null)
  const [drawerReceiverUserIds, setDrawerReceiverUserIds] = useState([])

  const employeeByEmpNo = useMemo(() => {
    return new Map(employees.map((employee) => [employee.empNo, employee]))
  }, [employees])

  const currentUser = useMemo(() => {
    return employeeByEmpNo.get(currentUserEmpNo) ?? employees[0] ?? null
  }, [employeeByEmpNo, currentUserEmpNo, employees])

  const loadReservations = useCallback(async () => {
    if ((lines.length > 0 && selectedLines.length === 0) || (classes.length > 0 && selectedClasses.length === 0)) {
      setReservations([])
      return
    }

    setLoadingReservations(true)

    try {
      const result = await getReservations({
        lineId: selectedLines,
        largeClass: selectedClasses,
        tab: selectedTab === 'ALL' ? undefined : selectedTab,
      })
      setReservations(result)
      setErrorMessage('')
    } catch (error) {
      setErrorMessage(error?.response?.data ?? '예약 목록 조회에 실패했습니다.')
    } finally {
      setLoadingReservations(false)
    }
  }, [selectedLines, selectedClasses, selectedTab, lines.length, classes.length])

  useEffect(() => {
    const initialize = async () => {
      try {
        const [lineList, classList, equipmentList, employeeList] = await Promise.all([
          getLines(),
          getClasses(),
          getEquipments(),
          getEmployees(),
        ])

        setLines(lineList)
        setAllClasses(classList)
        setClasses(classList)
        setEquipments(equipmentList)
        setEmployees(employeeList)
        setSelectedLines(lineList)
        setSelectedClasses(classList)
        setCurrentUserEmpNo((prev) => prev || employeeList[0]?.empNo || '')
        setErrorMessage('')
      } catch (error) {
        setErrorMessage(error?.response?.data ?? '초기 데이터 로딩에 실패했습니다.')
      }
    }

    initialize()
  }, [])

  useEffect(() => {
    const refreshClasses = async () => {
      if (selectedLines.length === 0) {
        setClasses(allClasses)
        return
      }

      try {
        const classList = await getClasses(selectedLines)
        setClasses(classList)
        setSelectedClasses((prev) => {
          const nextSelected = prev.filter((value) => classList.includes(value))
          return nextSelected.length === 0 ? classList : nextSelected
        })
      } catch {
        // Keep previous state when class refresh fails.
      }
    }

    refreshClasses()
  }, [selectedLines, allClasses])

  useEffect(() => {
    loadReservations()
  }, [loadReservations])

  useEffect(() => {
    if (!currentUser?.userId) {
      return
    }

    const searchValue = JSON.stringify({
      lineIds: selectedLines,
      classes: selectedClasses,
      tab: selectedTab,
      site: bizSite,
    })

    saveSearchHistory({
      appId: 'YMS_RESERVATION',
      controlId: 'MAIN_FILTER',
      userId: currentUser.userId,
      searchValue,
    }).catch(() => {
      // Search history failures should not interrupt the main UX.
    })
  }, [selectedLines, selectedClasses, selectedTab, bizSite, currentUser])

  const drawerEquipments = useMemo(() => {
    const filtered = equipments.filter((equipment) => {
      const lineMatch = selectedLines.length === 0 ? true : selectedLines.includes(equipment.lineId)
      const classMatch = selectedClasses.length === 0 ? true : selectedClasses.includes(equipment.largeClass)
      return lineMatch && classMatch
    })

    return filtered.length > 0 ? filtered : equipments
  }, [equipments, selectedLines, selectedClasses])

  const openCreateDrawer = () => {
    setDrawerMode('create')
    setActiveReservation(null)
    setDrawerReceiverUserIds([])
    setDrawerOpen(true)
  }

  const openEditDrawer = async (row) => {
    setDrawerMode('edit')
    setDrawerOpen(true)
    setActiveReservation(row)
    setDrawerReceiverUserIds([])

    try {
      const detail = await getReservationById(row.id)
      setActiveReservation(detail)

      const receivers = await getNotificationReceivers(detail.issueNo, '0')
      setDrawerReceiverUserIds(receivers.map((receiver) => receiver.userId))
    } catch {
      // Row data already exists, so keep editing with fallback values.
    }
  }

  const closeDrawer = () => {
    setDrawerOpen(false)
    setActiveReservation(null)
    setDrawerReceiverUserIds([])
  }

  const handleSave = async (payload) => {
    setSaving(true)

    try {
      const selectedEquipment = equipments.find((equipment) => equipment.id === payload.equipmentId)
      const selectedEmployee = employeeByEmpNo.get(payload.empNum)

      if (!selectedEquipment) {
        setErrorMessage('설비 정보 확인에 실패했습니다.')
        return
      }

      if (bizSite === 'HQ') {
        const authResult = await checkReceptionAuth({
          site: bizSite,
          eqpName: selectedEquipment.eqpId,
          authType: 'RESV',
          empNo: payload.empNum,
          singleId: selectedEmployee?.singleId ?? '',
        })

        if (!authResult?.isAuthorized) {
          setErrorMessage('접수 권한이 없습니다.')
          return
        }
      }

      const requestPayload = {
        ...payload,
        site: bizSite,
        authType: 'RESV',
        singleId: selectedEmployee?.singleId ?? '',
      }

      if (drawerMode === 'edit' && activeReservation) {
        await updateReservation(activeReservation.id, requestPayload)
      } else {
        await createReservation(requestPayload)
      }

      closeDrawer()
      await loadReservations()
      setErrorMessage('')
    } catch (error) {
      setErrorMessage(error?.response?.data ?? '저장에 실패했습니다.')
    } finally {
      setSaving(false)
    }
  }

  const handleDelete = async (id) => {
    setSaving(true)

    try {
      await deleteReservation(id)
      closeDrawer()
      await loadReservations()
      setErrorMessage('')
    } catch (error) {
      setErrorMessage(error?.response?.data ?? '삭제에 실패했습니다.')
    } finally {
      setSaving(false)
    }
  }

  const handleResetFilters = () => {
    setSelectedLines(lines)
    setClasses(allClasses)
    setSelectedClasses(allClasses)
    setSelectedTab('ALL')
  }

  return (
    <div className="app-shell">
      <header className="top-header">
        <div className="header-title">분석설비</div>

        <EquipmentTabs tabs={TABS} selectedTab={selectedTab} onSelectTab={setSelectedTab} />

        <div className="header-tools">
          <label className="header-field">
            Site
            <select value={bizSite} onChange={(event) => setBizSite(event.target.value)}>
              {SITES.map((site) => (
                <option key={site} value={site}>
                  {site}
                </option>
              ))}
            </select>
          </label>

          <label className="header-field">
            사용자
            <select
              value={currentUserEmpNo}
              onChange={(event) => setCurrentUserEmpNo(event.target.value)}
            >
              {employees.map((employee) => (
                <option key={employee.empNo} value={employee.empNo}>
                  {employee.name}
                </option>
              ))}
            </select>
          </label>

          <button className="btn btn-primary" onClick={openCreateDrawer}>
            Add
          </button>
        </div>
      </header>

      {errorMessage && <div className="error-banner">{errorMessage}</div>}

      <main className="content-grid">
        <FilterSidebar
          lines={lines}
          classes={classes}
          selectedLines={selectedLines}
          selectedClasses={selectedClasses}
          onToggleLine={(value) => setSelectedLines((prev) => toggleSelection(prev, value))}
          onToggleClass={(value) => setSelectedClasses((prev) => toggleSelection(prev, value))}
          onReset={handleResetFilters}
        />

        <section className="grid-panel">
          <DataGrid rows={reservations} loading={loadingReservations} onRowClick={openEditDrawer} />
        </section>
      </main>

      <ReservationDrawer
        open={drawerOpen}
        mode={drawerMode}
        reservation={activeReservation}
        employees={employees}
        equipments={drawerEquipments}
        defaultEmpNum={currentUserEmpNo}
        receiverCandidates={employees}
        initialReceiverUserIds={drawerReceiverUserIds}
        saving={saving}
        onClose={closeDrawer}
        onSubmit={handleSave}
        onDelete={handleDelete}
      />
    </div>
  )
}

export default App
