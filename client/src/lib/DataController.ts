/**
 * 실무 P00090 프로젝트에서 사용하는 DataController.
 * DataInfo 객체를 받아서 서버에 요청을 보내고, DataTable 형태로 응답을 받는다.
 *
 * 사용법:
 *   const dataInfo = new DataInfo("Controls", "GetEmployeeList", params)
 *   const result = await DataController.execute(dataInfo)
 *   result.rows.forEach(row => { ... })
 *
 * 실무에서는 이 패턴이 모든 공용 컴포넌트(Input, Grid 등)에서 사용된다.
 */
import apiClient from '../api/api'
import DataInfo from './DataInfo'

export interface DataRow {
  [key: string]: any
  getValue(columnName: string): any
}

export interface DataTable {
  columns: string[]
  rows: DataRow[]
  Rows: { Count: number; [index: number]: DataRow }
}

const wrapRow = (raw: Record<string, any>): DataRow => {
  return {
    ...raw,
    getValue(columnName: string) {
      return raw[columnName]
    },
  }
}

const wrapTable = (data: any): DataTable => {
  if (!data || !Array.isArray(data.rows)) {
    const emptyRows: DataRow[] = []
    return {
      columns: [],
      rows: emptyRows,
      Rows: Object.assign(emptyRows, { Count: 0 }),
    }
  }

  const columns: string[] = data.columns ?? []
  const rows: DataRow[] = data.rows.map(wrapRow)

  return {
    columns,
    rows,
    Rows: Object.assign([...rows], { Count: rows.length }),
  }
}

const DataController = {
  /**
   * DataInfo를 서버에 보내고 DataTable 형태로 응답을 받는다.
   * 실무에서는 공용 Input 컴포넌트가 내부에서 이 함수를 호출한다.
   */
  async execute(dataInfo: DataInfo): Promise<DataTable> {
    const response = await apiClient.post('/datainfo/execute', dataInfo.toPayload())
    return wrapTable(response.data)
  },
}

export default DataController
