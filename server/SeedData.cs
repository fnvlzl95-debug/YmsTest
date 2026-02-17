using Microsoft.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using YMS.Server.Data;
using YMS.Server.Models;

namespace YMS.Server;

public static class SeedData
{
    public static async Task InitializeAsync(AppDbContext context)
    {
        await EnsureDatabaseReadyAsync(context);

        if (!await context.Employees.AnyAsync())
        {
            var employees = new List<Employee>
            {
                new() { UserId = "yyj1204", EmpNo = "YYJ1204", HName = "양윤정", EName = "YANG YOONJUNG", DeptCode = "CAS2", DeptName = "CAS2 BOND", SingleId = "yyj1204", SingleMailAddr = "yyj1204@samsung.com", Site = "HQ" },
                new() { UserId = "kcs0301", EmpNo = "KCS0301", HName = "김철수", EName = "KIM CHULSU", DeptCode = "CAS2", DeptName = "CAS2 BOND", SingleId = "kcs0301", SingleMailAddr = "kcs0301@samsung.com", Site = "HQ" },
                new() { UserId = "lyh0515", EmpNo = "LYH0515", HName = "이영희", EName = "LEE YOUNGHEE", DeptCode = "CTS6", DeptName = "CTS6 TEST", SingleId = "lyh0515", SingleMailAddr = "lyh0515@samsung.com", Site = "HQ" },
                new() { UserId = "pdh0922", EmpNo = "PDH0922", HName = "박도현", EName = "PARK DOHYUN", DeptCode = "CTS6", DeptName = "CTS6 TEST", SingleId = "pdh0922", SingleMailAddr = "pdh0922@samsung.com", Site = "HQ" },
                new() { UserId = "yda0101", EmpNo = "YDA0101", HName = "유다은", EName = "YU DAEUN", DeptCode = "YAS1", DeptName = "YAS1 MOLE", SingleId = "yda0101", SingleMailAddr = "yda0101@samsung.com", Site = "HQ" },
                new() { UserId = "hqadmin", EmpNo = "ADM0001", HName = "본사관리자", EName = "HQ ADMIN", DeptCode = "HQ", DeptName = "HQ YMS", SingleId = "hqadmin", SingleMailAddr = "hqadmin@samsung.com", Site = "HQ" },
            };

            await context.Employees.AddRangeAsync(employees);
            await context.SaveChangesAsync();
        }

        if (!await context.Equipments.AnyAsync())
        {
            var equipments = new List<Equipment>
            {
                new() { LineId = "CAS2", LargeClass = "BOND", EqpType = "WAFER_CHIP_MOUNT", EqpId = "AWB07B2", EqpGroupName = "SDB-30WR" },
                new() { LineId = "CAS2", LargeClass = "BOND", EqpType = "DIE_ATTACH", EqpId = "CDA03A", EqpGroupName = "DA-800" },
                new() { LineId = "YAS1", LargeClass = "MOLE", EqpType = "MOLD", EqpId = "YMD02A", EqpGroupName = "MLP-800" },
                new() { LineId = "YAS1", LargeClass = "MOLE", EqpType = "LASER_MARK", EqpId = "YLM01B", EqpGroupName = "VJ-7510" },
                new() { LineId = "CTS6", LargeClass = "TEST", EqpType = "TEST_HANDLER", EqpId = "M810A", EqpGroupName = "M810" },
                new() { LineId = "CTS6", LargeClass = "TEST", EqpType = "TEST_HANDLER", EqpId = "M810B", EqpGroupName = "M810" },
            };

            await context.Equipments.AddRangeAsync(equipments);
            await context.SaveChangesAsync();
        }

        if (!await context.OpenLabAuths.AnyAsync())
        {
            var authRows = new List<OpenLabAuth>
            {
                new() { Site = "HQ", EqpName = "AWB07B2", AuthType = "RESV", EmpNo = "YYJ1204" },
                new() { Site = "HQ", EqpName = "CDA03A", AuthType = "RESV", EmpNo = "KCS0301" },
                new() { Site = "HQ", EqpName = "M810A", AuthType = "RESV", EmpNo = "LYH0515" },
                new() { Site = "HQ", EqpName = "M810B", AuthType = "RESV", EmpNo = "PDH0922" },
                new() { Site = "HQ", EqpName = "AWB07B2", AuthType = "ADMIN", EmpNo = "ADM0001" },
                new() { Site = "HQ", EqpName = "M810A", AuthType = "ADMIN", EmpNo = "ADM0001" },
                new() { Site = "FAB", EqpName = "AWB07B2", AuthType = "RESV", EmpNo = "YYJ1204" },
                new() { Site = "FAB", EqpName = "CDA03A", AuthType = "RESV", EmpNo = "KCS0301" },
                new() { Site = "FAB", EqpName = "M810A", AuthType = "RESV", EmpNo = "LYH0515" },
                new() { Site = "FAB", EqpName = "M810B", AuthType = "RESV", EmpNo = "PDH0922" },
            };

            await context.OpenLabAuths.AddRangeAsync(authRows);
            await context.SaveChangesAsync();
        }

        if (!await context.Reservations.AnyAsync())
        {
            var equipmentByEqpId = await context.Equipments.ToDictionaryAsync(e => e.EqpId, e => e);

            var reservations = new List<Reservation>
            {
                CreateReservation("RESV-20251204-001", equipmentByEqpId["AWB07B2"], "양윤정", "YYJ1204", "2025-12-04", "ESD 측정", "승인"),
                CreateReservation("RESV-20251205-001", equipmentByEqpId["CDA03A"], "김철수", "KCS0301", "2025-12-05", "P-TURN 점검", "대기"),
                CreateReservation("RESV-20251206-001", equipmentByEqpId["M810A"], "이영희", "LYH0515", "2025-12-06", "핸들러 테스트", "대기"),
                CreateReservation("RESV-20251207-001", equipmentByEqpId["M810B"], "박도현", "PDH0922", "2025-12-07", "ESD 측정", "반려"),
            };

            await context.Reservations.AddRangeAsync(reservations);
            await context.SaveChangesAsync();
        }

        if (!await context.ApprovalNotifications.AnyAsync())
        {
            var employeeByEmpNo = await context.Employees.ToDictionaryAsync(e => e.EmpNo, e => e);
            var employeeByUserId = await context.Employees.ToDictionaryAsync(e => e.UserId, e => e);
            var reservations = await context.Reservations.AsNoTracking().ToListAsync();

            var notifications = new List<ApprovalNotification>
            {
                CreateNotification("NOTICE-ESD 측정", "0", "0", employeeByUserId["hqadmin"]),
                CreateNotification("NOTICE-P-TURN", "0", "0", employeeByUserId["hqadmin"]),
                CreateNotification("NOTICE--", "0", "0", employeeByUserId["hqadmin"]),
            };

            foreach (var reservation in reservations)
            {
                if (employeeByEmpNo.TryGetValue(reservation.EmpNum, out var requester))
                {
                    notifications.Add(CreateNotification(reservation.IssueNo, "0", "1", requester));
                }

                var admin = employeeByUserId["hqadmin"];
                if (!notifications.Any(n => n.IssueNo == reservation.IssueNo && n.NotiUserId == admin.UserId))
                {
                    notifications.Add(CreateNotification(reservation.IssueNo, "0", "1", admin));
                }
            }

            var deduped = notifications
                .GroupBy(n => new { n.IssueNo, n.ApprovalSeq, n.NotiUserId })
                .Select(g => g.First())
                .ToList();

            await context.ApprovalNotifications.AddRangeAsync(deduped);
            await context.SaveChangesAsync();
        }
    }

    private static async Task EnsureDatabaseReadyAsync(AppDbContext context)
    {
        await context.Database.EnsureCreatedAsync();

        var providerName = context.Database.ProviderName ?? string.Empty;
        var isSqlite = providerName.Contains("Sqlite", StringComparison.OrdinalIgnoreCase);

        if (!isSqlite)
        {
            return;
        }

        try
        {
            _ = await context.Employees.AsNoTracking().AnyAsync();
        }
        catch (SqliteException)
        {
            // Existing sqlite file can keep stale schema from previous model.
            await context.Database.EnsureDeletedAsync();
            await context.Database.EnsureCreatedAsync();
        }
    }

    private static Reservation CreateReservation(
        string issueNo,
        Equipment equipment,
        string empName,
        string empNum,
        string reservedDate,
        string purpose,
        string status)
    {
        return new Reservation
        {
            IssueNo = issueNo,
            EquipmentId = equipment.Id,
            EqpId = equipment.EqpId,
            LineId = equipment.LineId,
            LargeClass = equipment.LargeClass,
            EmpName = empName,
            EmpNum = empNum,
            ReservedDate = DateTime.SpecifyKind(DateTime.Parse(reservedDate), DateTimeKind.Utc),
            Purpose = purpose,
            Status = status,
            CreatedAt = DateTime.UtcNow,
        };
    }

    private static ApprovalNotification CreateNotification(
        string issueNo,
        string approvalSeq,
        string approvalReq,
        Employee employee)
    {
        return new ApprovalNotification
        {
            IssueNo = issueNo,
            ApprovalSeq = approvalSeq,
            ApprovalReq = approvalReq,
            NotiUserId = employee.UserId,
            NotiUserName = employee.HName,
            NotiUserDeptCode = employee.DeptCode,
            NotiUserDeptName = employee.DeptName,
            NotiSingleMailAddr = employee.SingleMailAddr,
            LastUpdateTime = DateTime.UtcNow,
        };
    }
}
