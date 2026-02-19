/**
 * 실무 P00090 프로젝트에서 사용하는 Dictionary 유틸리티.
 * new Dictionary<K, V>().push("key", value).push("key2", value2)
 * 형태로 체이닝하여 사용한다.
 */
export default class Dictionary<K extends string, V> {
  private _map: Map<K, V>

  constructor() {
    this._map = new Map()
  }

  push(key: K, value: V): this {
    this._map.set(key, value)
    return this
  }

  getValue(key: K): V | undefined {
    return this._map.get(key)
  }

  has(key: K): boolean {
    return this._map.has(key)
  }

  keys(): K[] {
    return [...this._map.keys()]
  }

  values(): V[] {
    return [...this._map.values()]
  }

  entries(): [K, V][] {
    return [...this._map.entries()]
  }

  map<R>(fn: (value: V, key: K) => R): R[] {
    return this.entries().map(([key, value]) => fn(value, key))
  }
}
