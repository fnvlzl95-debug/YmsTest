/**
 * 실무 P00090 프로젝트에서 사용하는 DataInfo 클래스.
 * 프론트에서 백엔드 메서드를 호출할 때 사용하는 요청 객체.
 *
 * 예: new DataInfo("Controls", "GetEmployeeList", params, "General")
 *  → 서버의 Controls 클래스에서 GetEmployeeList 메서드를 호출
 */
import Dictionary from './Dictionary'

export default class DataInfo {
  className: string
  methodName: string
  params: Dictionary<string, any>
  category: string

  constructor(
    className: string,
    methodName: string,
    params: Dictionary<string, any> = new Dictionary(),
    category: string = 'General',
  ) {
    this.className = className
    this.methodName = methodName
    this.params = params
    this.category = category
  }

  toPayload(): Record<string, any> {
    const paramObj: Record<string, any> = {}

    this.params.entries().forEach(([key, value]) => {
      paramObj[key] = value
    })

    return {
      className: this.className,
      methodName: this.methodName,
      params: paramObj,
      category: this.category,
    }
  }
}
