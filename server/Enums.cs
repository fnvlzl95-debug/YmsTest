namespace YMS.Server;

public enum OpenLabPageTab
{
    OpenLabReservation = 0,
    EquipmentAuthManagement = 1,
}

public enum OpenLabAuthType
{
    Reservation = 0,
    Admin = 1,
}

public enum OpenLabReservationStatus
{
    Waiting = 0,
    Approved = 1,
    Rejected = 2,
}

public static class OpenLabCodes
{
    public static string ToCode(this OpenLabAuthType authType)
    {
        return authType switch
        {
            OpenLabAuthType.Admin => "ADMIN",
            _ => "RESV",
        };
    }

    public static string ToDisplay(this OpenLabReservationStatus status)
    {
        return status switch
        {
            OpenLabReservationStatus.Approved => "승인",
            OpenLabReservationStatus.Rejected => "반려",
            _ => "대기",
        };
    }

    public static OpenLabAuthType ParseAuthType(string? authTypeCode)
    {
        return string.Equals(authTypeCode?.Trim(), "ADMIN", StringComparison.OrdinalIgnoreCase)
            ? OpenLabAuthType.Admin
            : OpenLabAuthType.Reservation;
    }

    public static string NormalizeSite(string? site)
    {
        return string.IsNullOrWhiteSpace(site)
            ? "HQ"
            : site.Trim().ToUpperInvariant();
    }
}
