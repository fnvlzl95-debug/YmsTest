/**
 * 실무 공용 Input 컴포넌트 (P00090 패턴 재현).
 *
 * type에 따라 내부 동작이 완전히 달라진다:
 *   - "text"     : 일반 텍스트 입력
 *   - "select"   : dataSource로 옵션 목록을 서버에서 가져와서 드롭다운
 *   - "employee" : ★ dataSource를 무시하고, 내부에서 Controls.GetEmployeeList를 강제 호출
 *
 * 실무 디버깅 포인트:
 *   개발자가 dataSource={Main.GetAdmin}을 넘겨도
 *   type="employee"이면 공용이 Controls.GetEmployeeList로 덮어씀.
 *   → 이것이 "공용을 타고 있다"의 정체.
 */
import { useCallback, useEffect, useState } from 'react'
import { DataController, DataInfo, Dictionary } from '../lib'

export interface InputValue {
  value: string
  display?: string
  text?: string
}

interface ConditionInputProps {
  id: string
  label: string
  type: 'text' | 'select' | 'employee'
  dataSource?: DataInfo | null
  values?: InputValue[]
  onChange?: (id: string, values: InputValue[]) => void
}

function ConditionInput({ id, label, type, dataSource, values = [], onChange }: ConditionInputProps) {
  const [options, setOptions] = useState<InputValue[]>([])
  const [loading, setLoading] = useState(false)
  const [searchText, setSearchText] = useState('')

  const currentValue = values.length > 0 ? values[0]?.value ?? '' : ''
  const currentDisplay = values.length > 0 ? (values[0]?.display ?? values[0]?.text ?? values[0]?.value ?? '') : ''

  // ──────────────────────────────────────────────
  // type="select": dataSource를 그대로 사용하여 옵션 로딩
  // ──────────────────────────────────────────────
  useEffect(() => {
    if (type !== 'select' || !dataSource) {
      return
    }

    let cancelled = false

    const loadOptions = async () => {
      setLoading(true)

      try {
        const dt = await DataController.execute(dataSource)

        if (cancelled) return

        const nextOptions: InputValue[] = dt.rows.map((row) => ({
          value: String(row.getValue('INPUT_KEY') ?? row.getValue('EMP_NO') ?? ''),
          display: String(row.getValue('INPUT_NAME') ?? row.getValue('SINGLE_ID') ?? ''),
        }))

        setOptions(nextOptions)
      } catch {
        setOptions([])
      } finally {
        if (!cancelled) setLoading(false)
      }
    }

    loadOptions()
    return () => { cancelled = true }
  }, [type, dataSource])

  // ──────────────────────────────────────────────
  // type="employee": ★ dataSource를 완전히 무시하고
  // Controls.GetEmployeeList를 강제 호출한다.
  // 이것이 실무에서 "공용을 탄다"의 정체.
  // ──────────────────────────────────────────────
  const loadEmployeeList = useCallback(async (searchName: string) => {
    if (!searchName.trim()) {
      setOptions([])
      return
    }

    setLoading(true)

    try {
      // ★ 핵심: dataSource(props)를 무시하고 Controls.GetEmployeeList를 직접 호출
      const params = new Dictionary<string, any>().push('userNames', [searchName])
      const employeeDataInfo = new DataInfo('Controls', 'GetEmployeeList', params, 'General')
      const dt = await DataController.execute(employeeDataInfo)

      const nextOptions: InputValue[] = dt.rows.map((row) => ({
        value: String(row.getValue('EMP_NO') ?? ''),
        display: String(row.getValue('USER_NAME') ?? ''),
        text: String(row.getValue('USER_NAME') ?? ''),
      }))

      setOptions(nextOptions)
    } catch {
      setOptions([])
    } finally {
      setLoading(false)
    }
  }, [])

  // ──────────────────────────────────────────────
  // Render: type별로 다른 UI
  // ──────────────────────────────────────────────

  // type="text"
  if (type === 'text') {
    return (
      <label className="condition-input">
        <span className="condition-label">{label}</span>
        <input
          type="text"
          value={currentValue}
          onChange={(e) => {
            onChange?.(id, [{ value: e.target.value, display: e.target.value }])
          }}
        />
      </label>
    )
  }

  // type="employee" — 검색창 + 결과 드롭다운
  if (type === 'employee') {
    return (
      <label className="condition-input">
        <span className="condition-label">{label}</span>
        <div className="employee-search-wrap">
          <input
            type="text"
            placeholder="이름 검색..."
            value={searchText || currentDisplay}
            onChange={(e) => {
              setSearchText(e.target.value)
              loadEmployeeList(e.target.value)
            }}
          />
          {options.length > 0 && searchText && (
            <ul className="employee-dropdown">
              {options.map((opt) => (
                <li
                  key={opt.value}
                  onClick={() => {
                    onChange?.(id, [{ value: opt.value, display: opt.display }])
                    setSearchText('')
                    setOptions([])
                  }}
                >
                  {opt.display} ({opt.value})
                </li>
              ))}
            </ul>
          )}
          {loading && <span className="employee-loading">검색 중...</span>}
        </div>
      </label>
    )
  }

  // type="select" — 드롭다운
  return (
    <label className="condition-input">
      <span className="condition-label">{label}</span>
      <select
        value={currentValue}
        onChange={(e) => {
          const selected = options.find((opt) => opt.value === e.target.value)
          onChange?.(id, selected ? [selected] : [])
        }}
        disabled={loading}
      >
        <option value="">-- 선택 --</option>
        {options.map((opt) => (
          <option key={opt.value} value={opt.value}>
            {opt.display}
          </option>
        ))}
      </select>
    </label>
  )
}

export default ConditionInput
