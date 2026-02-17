-- ============================================
-- YMS 추가 테스트 데이터 INSERT 쿼리
-- SQLite 기준 (yms.db에 직접 실행)
-- ============================================

-- ■ 설비 추가 (DDB_EQUIPMENT_MST)
-- 기존: AWB07B2, CDA03A, YMD02A, YLM01B, M810A, M810B
INSERT INTO DDB_EQUIPMENT_MST (LineId, LargeClass, EqpType, EqpId, EqpGroupName)
VALUES ('CAS2', 'BOND', 'WIRE_BONDER', 'CWB01A', 'KNS-ICONN');

INSERT INTO DDB_EQUIPMENT_MST (LineId, LargeClass, EqpType, EqpId, EqpGroupName)
VALUES ('CAS2', 'BOND', 'FLIP_CHIP', 'CFC02A', 'FC-3000');

INSERT INTO DDB_EQUIPMENT_MST (LineId, LargeClass, EqpType, EqpId, EqpGroupName)
VALUES ('YAS1', 'MOLE', 'PLASMA_CLEAN', 'YPC01A', 'MARCH-AP300');

INSERT INTO DDB_EQUIPMENT_MST (LineId, LargeClass, EqpType, EqpId, EqpGroupName)
VALUES ('YAS1', 'MOLE', 'TRIM_FORM', 'YTF03A', 'DTF-5080');

INSERT INTO DDB_EQUIPMENT_MST (LineId, LargeClass, EqpType, EqpId, EqpGroupName)
VALUES ('CTS6', 'TEST', 'TEST_HANDLER', 'M810C', 'M810');

INSERT INTO DDB_EQUIPMENT_MST (LineId, LargeClass, EqpType, EqpId, EqpGroupName)
VALUES ('CTS6', 'TEST', 'TESTER', 'CTE01A', 'V93K');

INSERT INTO DDB_EQUIPMENT_MST (LineId, LargeClass, EqpType, EqpId, EqpGroupName)
VALUES ('CTS6', 'TEST', 'TESTER', 'CTE02A', 'J750');


-- ■ 직원 추가 (MST_EMPLOYEE)
-- 기존: yyj1204, kcs0301, lyh0515, pdh0922, yda0101, hqadmin
INSERT INTO MST_EMPLOYEE (UserId, EmpNo, HName, EName, DeptCode, DeptName, SingleId, SingleMailAddr, Site)
VALUES ('ljh0820', 'LJH0820', '이지현', 'LEE JIHYUN', 'CAS2', 'CAS2 BOND', 'ljh0820', 'ljh0820@samsung.com', 'HQ');

INSERT INTO MST_EMPLOYEE (UserId, EmpNo, HName, EName, DeptCode, DeptName, SingleId, SingleMailAddr, Site)
VALUES ('cms0612', 'CMS0612', '최민수', 'CHOI MINSU', 'YAS1', 'YAS1 MOLE', 'cms0612', 'cms0612@samsung.com', 'HQ');

INSERT INTO MST_EMPLOYEE (UserId, EmpNo, HName, EName, DeptCode, DeptName, SingleId, SingleMailAddr, Site)
VALUES ('ksj1105', 'KSJ1105', '강서준', 'KANG SEOJUN', 'CTS6', 'CTS6 TEST', 'ksj1105', 'ksj1105@samsung.com', 'HQ');

INSERT INTO MST_EMPLOYEE (UserId, EmpNo, HName, EName, DeptCode, DeptName, SingleId, SingleMailAddr, Site)
VALUES ('hjy0403', 'HJY0403', '한지윤', 'HAN JIYUN', 'CAS2', 'CAS2 BOND', 'hjy0403', 'hjy0403@samsung.com', 'HQ');

INSERT INTO MST_EMPLOYEE (UserId, EmpNo, HName, EName, DeptCode, DeptName, SingleId, SingleMailAddr, Site)
VALUES ('snw0728', 'SNW0728', '송나윤', 'SONG NAYUN', 'YAS1', 'YAS1 MOLE', 'snw0728', 'snw0728@samsung.com', 'FAB');


-- ■ 설비 권한 추가 (DDB_OPENLAB_AUTH)
INSERT INTO DDB_OPENLAB_AUTH (Site, EqpName, AuthType, EmpNo)
VALUES ('HQ', 'CWB01A', 'RESV', 'LJH0820');

INSERT INTO DDB_OPENLAB_AUTH (Site, EqpName, AuthType, EmpNo)
VALUES ('HQ', 'CFC02A', 'RESV', 'LJH0820');

INSERT INTO DDB_OPENLAB_AUTH (Site, EqpName, AuthType, EmpNo)
VALUES ('HQ', 'YPC01A', 'RESV', 'CMS0612');

INSERT INTO DDB_OPENLAB_AUTH (Site, EqpName, AuthType, EmpNo)
VALUES ('HQ', 'YTF03A', 'RESV', 'CMS0612');

INSERT INTO DDB_OPENLAB_AUTH (Site, EqpName, AuthType, EmpNo)
VALUES ('HQ', 'M810C', 'RESV', 'KSJ1105');

INSERT INTO DDB_OPENLAB_AUTH (Site, EqpName, AuthType, EmpNo)
VALUES ('HQ', 'CTE01A', 'RESV', 'KSJ1105');

INSERT INTO DDB_OPENLAB_AUTH (Site, EqpName, AuthType, EmpNo)
VALUES ('HQ', 'CTE02A', 'RESV', 'HJY0403');

INSERT INTO DDB_OPENLAB_AUTH (Site, EqpName, AuthType, EmpNo)
VALUES ('HQ', 'AWB07B2', 'RESV', 'HJY0403');

INSERT INTO DDB_OPENLAB_AUTH (Site, EqpName, AuthType, EmpNo)
VALUES ('HQ', 'YPC01A', 'RESV', 'SNW0728');

INSERT INTO DDB_OPENLAB_AUTH (Site, EqpName, AuthType, EmpNo)
VALUES ('HQ', 'CWB01A', 'ADMIN', 'ADM0001');

INSERT INTO DDB_OPENLAB_AUTH (Site, EqpName, AuthType, EmpNo)
VALUES ('HQ', 'CTE01A', 'ADMIN', 'ADM0001');


-- ■ 예약 추가 (DDB_EQUIPMENT_RESV)
-- EquipmentId는 기존 설비 Id 기준 (1~6 기존, 7~13 추가분)
-- 기존 예약: RESV-20251204-001 ~ RESV-20251207-001

-- CAS2 BOND 라인 예약들
INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251210-001', 1, 'AWB07B2', 'CAS2', 'BOND', '이지현', 'LJH0820', '2025-12-10', 'ESD 측정', '대기', datetime('now'));

INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251210-002', 2, 'CDA03A', 'CAS2', 'BOND', '한지윤', 'HJY0403', '2025-12-10', 'P-TURN 점검', '승인', datetime('now'));

INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251211-001', 7, 'CWB01A', 'CAS2', 'BOND', '양윤정', 'YYJ1204', '2025-12-11', 'Wire Bonding 상태 점검', '대기', datetime('now'));

INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251212-001', 8, 'CFC02A', 'CAS2', 'BOND', '이지현', 'LJH0820', '2025-12-12', 'Flip Chip 정렬 확인', '승인', datetime('now'));

-- YAS1 MOLE 라인 예약들
INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251211-002', 3, 'YMD02A', 'YAS1', 'MOLE', '최민수', 'CMS0612', '2025-12-11', 'Mold 두께 측정', '대기', datetime('now'));

INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251213-001', 4, 'YLM01B', 'YAS1', 'MOLE', '유다은', 'YDA0101', '2025-12-13', 'Laser Mark 품질 확인', '승인', datetime('now'));

INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251214-001', 9, 'YPC01A', 'YAS1', 'MOLE', '송나윤', 'SNW0728', '2025-12-14', 'Plasma Clean 효과 검증', '반려', datetime('now'));

INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251215-001', 10, 'YTF03A', 'YAS1', 'MOLE', '최민수', 'CMS0612', '2025-12-15', 'Trim Form 정밀도 점검', '대기', datetime('now'));

-- CTS6 TEST 라인 예약들
INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251211-003', 5, 'M810A', 'CTS6', 'TEST', '강서준', 'KSJ1105', '2025-12-11', 'Handler 속도 테스트', '승인', datetime('now'));

INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251212-002', 6, 'M810B', 'CTS6', 'TEST', '이영희', 'LYH0515', '2025-12-12', 'ESD 측정', '대기', datetime('now'));

INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251213-002', 11, 'M810C', 'CTS6', 'TEST', '박도현', 'PDH0922', '2025-12-13', 'P-TURN 점검', '대기', datetime('now'));

INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251216-001', 12, 'CTE01A', 'CTS6', 'TEST', '강서준', 'KSJ1105', '2025-12-16', 'V93K 테스터 캘리브레이션', '승인', datetime('now'));

INSERT INTO DDB_EQUIPMENT_RESV (IssueNo, EquipmentId, EqpId, LineId, LargeClass, EmpName, EmpNum, ReservedDate, Purpose, Status, CreatedAt)
VALUES ('RESV-20251217-001', 13, 'CTE02A', 'CTS6', 'TEST', '한지윤', 'HJY0403', '2025-12-17', 'J750 테스터 프로그램 검증', '반려', datetime('now'));


-- ■ 승인 알림 추가 (DDB_APPROVAL_NOTI)
INSERT INTO DDB_APPROVAL_NOTI (IssueNo, ApprovalSeq, ApprovalReq, NotiUserId, NotiUserName, NotiUserDeptCode, NotiUserDeptName, NotiSingleMailAddr, LastUpdateTime)
VALUES ('RESV-20251210-001', '0', '1', 'ljh0820', '이지현', 'CAS2', 'CAS2 BOND', 'ljh0820@samsung.com', datetime('now'));

INSERT INTO DDB_APPROVAL_NOTI (IssueNo, ApprovalSeq, ApprovalReq, NotiUserId, NotiUserName, NotiUserDeptCode, NotiUserDeptName, NotiSingleMailAddr, LastUpdateTime)
VALUES ('RESV-20251210-001', '0', '1', 'hqadmin', '본사관리자', 'HQ', 'HQ YMS', 'hqadmin@samsung.com', datetime('now'));

INSERT INTO DDB_APPROVAL_NOTI (IssueNo, ApprovalSeq, ApprovalReq, NotiUserId, NotiUserName, NotiUserDeptCode, NotiUserDeptName, NotiSingleMailAddr, LastUpdateTime)
VALUES ('RESV-20251211-001', '0', '1', 'yyj1204', '양윤정', 'CAS2', 'CAS2 BOND', 'yyj1204@samsung.com', datetime('now'));

INSERT INTO DDB_APPROVAL_NOTI (IssueNo, ApprovalSeq, ApprovalReq, NotiUserId, NotiUserName, NotiUserDeptCode, NotiUserDeptName, NotiSingleMailAddr, LastUpdateTime)
VALUES ('RESV-20251211-001', '0', '1', 'hqadmin', '본사관리자', 'HQ', 'HQ YMS', 'hqadmin@samsung.com', datetime('now'));

INSERT INTO DDB_APPROVAL_NOTI (IssueNo, ApprovalSeq, ApprovalReq, NotiUserId, NotiUserName, NotiUserDeptCode, NotiUserDeptName, NotiSingleMailAddr, LastUpdateTime)
VALUES ('RESV-20251211-002', '0', '1', 'cms0612', '최민수', 'YAS1', 'YAS1 MOLE', 'cms0612@samsung.com', datetime('now'));

INSERT INTO DDB_APPROVAL_NOTI (IssueNo, ApprovalSeq, ApprovalReq, NotiUserId, NotiUserName, NotiUserDeptCode, NotiUserDeptName, NotiSingleMailAddr, LastUpdateTime)
VALUES ('RESV-20251211-002', '0', '1', 'hqadmin', '본사관리자', 'HQ', 'HQ YMS', 'hqadmin@samsung.com', datetime('now'));

INSERT INTO DDB_APPROVAL_NOTI (IssueNo, ApprovalSeq, ApprovalReq, NotiUserId, NotiUserName, NotiUserDeptCode, NotiUserDeptName, NotiSingleMailAddr, LastUpdateTime)
VALUES ('RESV-20251213-002', '0', '1', 'pdh0922', '박도현', 'CTS6', 'CTS6 TEST', 'pdh0922@samsung.com', datetime('now'));

INSERT INTO DDB_APPROVAL_NOTI (IssueNo, ApprovalSeq, ApprovalReq, NotiUserId, NotiUserName, NotiUserDeptCode, NotiUserDeptName, NotiSingleMailAddr, LastUpdateTime)
VALUES ('RESV-20251213-002', '0', '1', 'hqadmin', '본사관리자', 'HQ', 'HQ YMS', 'hqadmin@samsung.com', datetime('now'));


-- ■ UI 검색 이력 추가 (TSP_YMS_UI_SEARCH_HISTORY)
INSERT INTO TSP_YMS_UI_SEARCH_HISTORY (AppId, ControlId, UserId, SearchTime, SearchValue)
VALUES ('EqpReservation', 'filterLine', 'yyj1204', datetime('now', '-3 hours'), 'CAS2');

INSERT INTO TSP_YMS_UI_SEARCH_HISTORY (AppId, ControlId, UserId, SearchTime, SearchValue)
VALUES ('EqpReservation', 'filterLine', 'yyj1204', datetime('now', '-2 hours'), 'CTS6');

INSERT INTO TSP_YMS_UI_SEARCH_HISTORY (AppId, ControlId, UserId, SearchTime, SearchValue)
VALUES ('EqpReservation', 'filterClass', 'kcs0301', datetime('now', '-1 hours'), 'BOND');

INSERT INTO TSP_YMS_UI_SEARCH_HISTORY (AppId, ControlId, UserId, SearchTime, SearchValue)
VALUES ('EqpReservation', 'filterLine', 'cms0612', datetime('now', '-30 minutes'), 'YAS1');

INSERT INTO TSP_YMS_UI_SEARCH_HISTORY (AppId, ControlId, UserId, SearchTime, SearchValue)
VALUES ('EqpReservation', 'filterClass', 'ksj1105', datetime('now', '-10 minutes'), 'TEST');
