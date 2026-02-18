import { useCallback, useEffect, useMemo, useState } from 'react'
import {
  checkReceptionAuth,
  createOpenLabAuth,
  createOpenLabReservation,
  deleteOpenLabAuth,
  deleteOpenLabReservation,
  getMainLookups,
  getOpenLabAuths,
  getOpenLabEquipments,
  getOpenLabReservationById,
  getOpenLabReservations,
  saveSearchHistory,
  updateOpenLabReservation,
} from './api/api'
import { BIZ_SITES, PAGE_TABS, PURPOSE_TABS } from './Enums'
import {
  AnaApprovalEdit,
  AnaResultEdit,
  ReceptionEdit,
  RequestEdit,
} from './components'

const toggleSelection = (items, value) => {
  if (items.includes(value)) {
    return items.filter((item) => item !== value)
  }

  return [...items, value]
}

const sleep = (ms) => {
  return new Promise((resolve) => {
    setTimeout(resolve, ms)
  })
}

function Main() {
  const [activePageTab, setActivePageTab] = useState(PAGE_TABS[0].id)
  const [bizSite, setBizSite] = useState(BIZ_SITES[0].id)

  const [lookups, setLookups] = useState({
    lines: [],
    classes: [],
    purposes: [],
    equipments: [],
    employees: [],
  })

  const [resvFilters, setResvFilters] = useState({
    lines: [],
    classes: [],
    purpose: 'ALL',
  })

  const [currentUserEmpNo, setCurrentUserEmpNo] = useState('')

  const [reservationRows, setReservationRows] = useState([])
  const [equipmentRows, setEquipmentRows] = useState([])
  const [authRows, setAuthRows] = useState([])

  const [lookupLoading, setLookupLoading] = useState(false)
  const [reservationLoading, setReservationLoading] = useState(false)
  const [managementLoading, setManagementLoading] = useState(false)
  const [saving, setSaving] = useState(false)

  const [errorMessage, setErrorMessage] = useState('')
  const [infoMessage, setInfoMessage] = useState('')
  const [filtersReady, setFiltersReady] = useState(false)

  const [drawerOpen, setDrawerOpen] = useState(false)
  const [drawerMode, setDrawerMode] = useState('create')
  const [activeReservation, setActiveReservation] = useState(null)
  const [drawerReceiverUserIds, setDrawerReceiverUserIds] = useState([])

  const employeeByEmpNo = useMemo(() => {
    return new Map(lookups.employees.map((employee) => [employee.empNo, employee]))
  }, [lookups.employees])

  const currentUser = useMemo(() => {
    return employeeByEmpNo.get(currentUserEmpNo) ?? lookups.employees[0] ?? null
  }, [employeeByEmpNo, currentUserEmpNo, lookups.employees])

  const purposeTabs = useMemo(() => {
    const tabMap = new Map(PURPOSE_TABS.map((tab) => [tab.id, tab]))

    lookups.purposes.forEach((purpose) => {
      if (!tabMap.has(purpose)) {
        tabMap.set(purpose, { id: purpose, label: purpose })
      }
    })

    return [...tabMap.values()]
  }, [lookups.purposes])

  const loadReservationRows = useCallback(async (filters) => {
    if ((lookups.lines.length > 0 && filters.lines.length === 0)
      || (lookups.classes.length > 0 && filters.classes.length === 0)) {
      setReservationRows([])
      return
    }

    setReservationLoading(true)

    try {
      const rows = await getOpenLabReservations({
        lineId: filters.lines,
        largeClass: filters.classes,
        purpose: filters.purpose,
        site: bizSite,
      })

      setReservationRows(rows)
      setErrorMessage('')
    } catch (error) {
      setErrorMessage(error?.response?.data ?? '예약 목록 조회에 실패했습니다.')
    } finally {
      setReservationLoading(false)
    }
  }, [bizSite, lookups.lines.length, lookups.classes.length])

  const loadEquipmentRows = useCallback(async (filters) => {
    setManagementLoading(true)

    try {
      const rows = await getOpenLabEquipments({
        lineId: filters.lines,
        largeClass: filters.classes,
      })

      setEquipmentRows(rows)
      setErrorMessage('')
    } catch (error) {
      setErrorMessage(error?.response?.data ?? '설비 목록 조회에 실패했습니다.')
    } finally {
      setManagementLoading(false)
    }
  }, [])

  const loadAuthRows = useCallback(async () => {
    setManagementLoading(true)

    try {
      const rows = await getOpenLabAuths({ site: bizSite })
      setAuthRows(rows)
      setErrorMessage('')
    } catch (error) {
      setErrorMessage(error?.response?.data ?? '권한 목록 조회에 실패했습니다.')
    } finally {
      setManagementLoading(false)
    }
  }, [bizSite])

  useEffect(() => {
    let disposed = false

    const initialize = async () => {
      setLookupLoading(true)
      setFiltersReady(false)

      try {
        let response = null

        for (let attempt = 0; attempt < 8; attempt += 1) {
          try {
            response = await getMainLookups({ site: bizSite })
            break
          } catch (error) {
            const isNetworkError = !error?.response
            const canRetry = isNetworkError && attempt < 7 && !disposed

            if (!canRetry) {
              throw error
            }

            await sleep(800)
          }
        }

        if (!response || disposed) {
          return
        }

        setLookups(response)

        const nextFilters = {
          lines: response.lines,
          classes: response.classes,
          purpose: 'ALL',
        }

        setResvFilters(nextFilters)
        setCurrentUserEmpNo((prev) => {
          if (prev && response.employees.some((employee) => employee.empNo === prev)) {
            return prev
          }

          return response.employees[0]?.empNo ?? ''
        })

        setFiltersReady(true)
        setReservationRows([])
        setEquipmentRows([])
        setAuthRows([])
        setErrorMessage('')
      } catch (error) {
        setErrorMessage(error?.response?.data ?? '초기 데이터 로딩에 실패했습니다.')
      } finally {
        setLookupLoading(false)
      }
    }

    initialize()

    return () => {
      disposed = true
    }
  }, [bizSite, loadAuthRows, loadEquipmentRows])

  const handleSearch = useCallback(async () => {
    if (!filtersReady) {
      return
    }

    await Promise.all([
      loadReservationRows(resvFilters),
      loadEquipmentRows(resvFilters),
      loadAuthRows(),
    ])

    if (currentUser?.userId) {
      const searchValue = JSON.stringify({
        pageTab: activePageTab,
        site: bizSite,
        lineFilters: resvFilters.lines,
        classFilters: resvFilters.classes,
        purpose: resvFilters.purpose,
      })

      saveSearchHistory({
        appId: 'P00090',
        controlId: 'MAIN_FILTER',
        userId: currentUser.userId,
        searchValue,
      }).catch(() => {
        // Search history should not block the UI.
      })
    }
  }, [filtersReady, resvFilters, activePageTab, bizSite, currentUser, loadReservationRows, loadEquipmentRows, loadAuthRows])

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
      const detail = await getOpenLabReservationById(row.id)
      setActiveReservation(detail)
      setDrawerReceiverUserIds(detail.receiverUserIds ?? [])
    } catch {
      // fallback to grid row
    }
  }

  const closeDrawer = () => {
    setDrawerOpen(false)
    setActiveReservation(null)
    setDrawerReceiverUserIds([])
  }

  const handleSaveReservation = async (payload) => {
    setSaving(true)

    try {
      const selectedEquipment = lookups.equipments.find((equipment) => equipment.id === payload.equipmentId)
      const selectedEmployee = employeeByEmpNo.get(payload.empNum)

      if (!selectedEquipment || !selectedEmployee) {
        setErrorMessage('사용자 또는 설비 정보 확인에 실패했습니다.')
        return
      }

      if (bizSite === 'HQ') {
        const authResult = await checkReceptionAuth({
          site: bizSite,
          eqpName: selectedEquipment.eqpId,
          authType: 'RESV',
          empNo: payload.empNum,
          singleId: selectedEmployee.singleId,
        })

        if (!authResult?.isAuthorized) {
          setErrorMessage('접수 권한이 없습니다.')
          return
        }
      }

      const request = {
        ...payload,
        site: bizSite,
        authType: 'RESV',
        singleId: selectedEmployee.singleId,
      }

      if (drawerMode === 'edit' && activeReservation) {
        await updateOpenLabReservation(activeReservation.id, request)
        setInfoMessage('예약 정보를 수정했습니다.')
      } else {
        await createOpenLabReservation(request)
        setInfoMessage('예약을 등록했습니다.')
      }

      closeDrawer()
      await Promise.all([
        loadReservationRows(resvFilters),
        loadEquipmentRows(resvFilters),
        loadAuthRows(),
      ])
      setErrorMessage('')
    } catch (error) {
      setErrorMessage(error?.response?.data ?? '저장에 실패했습니다.')
    } finally {
      setSaving(false)
    }
  }

  const handleDeleteReservation = async (reservationId) => {
    setSaving(true)

    try {
      await deleteOpenLabReservation(reservationId)
      closeDrawer()
      await Promise.all([
        loadReservationRows(resvFilters),
        loadEquipmentRows(resvFilters),
        loadAuthRows(),
      ])

      setInfoMessage('예약을 삭제했습니다.')
      setErrorMessage('')
    } catch (error) {
      setErrorMessage(error?.response?.data ?? '삭제에 실패했습니다.')
    } finally {
      setSaving(false)
    }
  }

  const handleCreateAuth = async (payload) => {
    setSaving(true)

    try {
      await createOpenLabAuth(payload)
      await loadAuthRows()
      setInfoMessage('권한을 추가했습니다.')
      setErrorMessage('')
    } catch (error) {
      setErrorMessage(error?.response?.data ?? '권한 추가에 실패했습니다.')
    } finally {
      setSaving(false)
    }
  }

  const handleDeleteAuth = async (id) => {
    setSaving(true)

    try {
      await deleteOpenLabAuth(id)
      await loadAuthRows()
      setInfoMessage('권한을 삭제했습니다.')
      setErrorMessage('')
    } catch (error) {
      setErrorMessage(error?.response?.data ?? '권한 삭제에 실패했습니다.')
    } finally {
      setSaving(false)
    }
  }

  return (
    <div className="app-shell">
      <header className="top-header">
        <div className="header-title">분석설비예약 (P00090)</div>

        <div className="header-tools">
          <label className="header-field">
            Site
            <select value={bizSite} onChange={(event) => setBizSite(event.target.value)}>
              {BIZ_SITES.map((site) => (
                <option key={site.id} value={site.id}>
                  {site.label}
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
              {lookups.employees.map((employee) => (
                <option key={employee.empNo} value={employee.empNo}>
                  {employee.name}
                </option>
              ))}
            </select>
          </label>
        </div>
      </header>

      <div className="page-tab-bar">
        {PAGE_TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            className={`page-tab ${activePageTab === tab.id ? 'active' : ''}`}
            onClick={() => setActivePageTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {lookupLoading && <div className="info-banner">기준정보를 불러오는 중입니다...</div>}
      {errorMessage && <div className="error-banner">{errorMessage}</div>}
      {!errorMessage && infoMessage && <div className="info-banner">{infoMessage}</div>}

      <main className="main-body">
        {activePageTab === 'OPENLAB_RESV' && (
          <ReceptionEdit
            lines={lookups.lines}
            classes={lookups.classes}
            selectedLines={resvFilters.lines}
            selectedClasses={resvFilters.classes}
            onToggleLine={(value) => {
              setResvFilters((prev) => ({
                ...prev,
                lines: toggleSelection(prev.lines, value),
              }))
            }}
            onToggleClass={(value) => {
              setResvFilters((prev) => ({
                ...prev,
                classes: toggleSelection(prev.classes, value),
              }))
            }}
            onSearch={handleSearch}
            onResetFilters={() => {
              setResvFilters({
                lines: lookups.lines,
                classes: lookups.classes,
                purpose: 'ALL',
              })
            }}
            purposeTabs={purposeTabs}
            selectedPurpose={resvFilters.purpose}
            onSelectPurpose={(purpose) => {
              setResvFilters((prev) => ({
                ...prev,
                purpose,
              }))
            }}
            rows={reservationRows}
            loading={reservationLoading}
            onRowClick={openEditDrawer}
            onOpenCreate={openCreateDrawer}
          />
        )}

        {activePageTab === 'EQP_AUTH' && (
          <section className="management-grid">
            <AnaResultEdit rows={equipmentRows} />
            <AnaApprovalEdit
              site={bizSite}
              rows={authRows}
              equipments={lookups.equipments}
              employees={lookups.employees}
              saving={saving || managementLoading}
              onCreate={handleCreateAuth}
              onDelete={handleDeleteAuth}
            />
          </section>
        )}
      </main>

      <RequestEdit
        open={drawerOpen}
        mode={drawerMode}
        reservation={activeReservation}
        employees={lookups.employees}
        equipments={lookups.equipments}
        defaultEmpNo={currentUserEmpNo}
        receiverCandidates={lookups.employees}
        initialReceiverUserIds={drawerReceiverUserIds}
        saving={saving}
        onClose={closeDrawer}
        onSubmit={handleSaveReservation}
        onDelete={handleDeleteReservation}
      />
    </div>
  )
}

export default Main
