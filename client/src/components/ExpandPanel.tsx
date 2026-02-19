/**
 * 실무 공용 ExpandPanel 컴포넌트.
 * 접었다 펼 수 있는 패널. 실무에서 조건 입력 영역을 감싸는 용도로 사용.
 *
 * 사용법:
 *   <ExpandPanel title="조건" defaultExpanded>
 *     <ConditionInput ... />
 *   </ExpandPanel>
 */
import { useState } from 'react'

interface ExpandPanelProps {
  title: string
  defaultExpanded?: boolean
  children: React.ReactNode
}

function ExpandPanel({ title, defaultExpanded = true, children }: ExpandPanelProps) {
  const [expanded, setExpanded] = useState(defaultExpanded)

  return (
    <div className={`expand-panel ${expanded ? 'expanded' : 'collapsed'}`}>
      <div
        className="expand-panel-header"
        onClick={() => setExpanded((prev) => !prev)}
      >
        <span className="expand-panel-arrow">{expanded ? '▼' : '▶'}</span>
        <span className="expand-panel-title">{title}</span>
      </div>

      {expanded && (
        <div className="expand-panel-body">
          {children}
        </div>
      )}
    </div>
  )
}

export default ExpandPanel
