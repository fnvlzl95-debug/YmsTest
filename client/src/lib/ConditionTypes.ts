/**
 * 실무 P00090 패턴에서 사용하는 조건(Condition) 관련 타입 정의.
 *
 * ConTypes: 조건 Input의 타입 (text / select / employee)
 * ConditionValue: 조건 한 개의 현재 값 (id별 InputValue 배열)
 * ConditionDataSource: 조건 한 개의 데이터 소스 (DataInfo 객체)
 *
 * 실무에서 화면 조건 영역은 이 타입들로 상태를 관리한다:
 *   conditions: Record<string, ConditionValue>
 *   dataSources: Record<string, ConditionDataSource>
 */
import type { InputValue } from '../components/ConditionInput'
import DataInfo from './DataInfo'
import Dictionary from './Dictionary'

export type ConTypes = 'text' | 'select' | 'employee'

export interface ConditionValue {
  type: ConTypes
  values: InputValue[]
}

export interface ConditionDataSource {
  type: ConTypes
  dataInfo: DataInfo | null
}

/**
 * ★ 실무 핵심: makeConditionDataSource
 *
 * conType별로 적절한 DataInfo를 생성하는 팩토리 함수.
 * 화면에서 조건 Input의 dataSource를 설정할 때 이 함수를 사용한다.
 *
 * 중요: type="employee"일 때도 여기서 DataInfo를 만들어 넘기지만,
 * 실제로 ConditionInput 내부에서 Controls.GetEmployeeList로 덮어쓴다.
 * → 이것이 "공용을 타고 있다"의 정체.
 *
 * 개발자가 Main.GetAdmin을 넘겨도 type="employee"이면 무시됨.
 */
export function makeConditionDataSource(
  conType: ConTypes,
  overrideDataInfo?: DataInfo | null,
): ConditionDataSource {
  if (overrideDataInfo) {
    return { type: conType, dataInfo: overrideDataInfo }
  }

  switch (conType) {
    case 'employee': {
      // ★ 여기서 Main.GetAdmin을 넘겨도 ConditionInput이 무시하고
      // Controls.GetEmployeeList를 강제 호출한다.
      const params = new Dictionary<string, any>()
      return {
        type: 'employee',
        dataInfo: new DataInfo('Main', 'GetAdmin', params, 'General'),
      }
    }

    case 'select': {
      const params = new Dictionary<string, any>()
      return {
        type: 'select',
        dataInfo: new DataInfo('Main', 'GetAdmin', params, 'General'),
      }
    }

    case 'text':
    default:
      return { type: 'text', dataInfo: null }
  }
}

/**
 * handleConditionChange 패턴의 타입.
 * 실무에서 (id, values) 두 인수 패턴으로 조건 변경을 처리한다.
 */
export type ConditionChangeHandler = (id: string, values: InputValue[]) => void

/**
 * 조건 초기값 생성 헬퍼.
 */
export function createConditionValue(type: ConTypes, values: InputValue[] = []): ConditionValue {
  return { type, values }
}
