namespace SportFlow.Domain.Identity;

public static class SystemRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string TenantOwner = "TenantOwner";
    public const string TenantManager = "TenantManager";
    public const string Staff = "Staff";
    public const string Coach = "Coach";
    public const string Member = "Member";

    public static readonly IReadOnlyList<string> All =
        [SuperAdmin, TenantOwner, TenantManager, Staff, Coach, Member];

    public static bool IsTenantRole(string role) =>
        role is TenantOwner or TenantManager or Staff or Coach or Member;

    public static string[] GetScopesForRole(string role) => role switch
    {
        SuperAdmin => ["platform:admin"],
        TenantOwner => ["tenant:admin", "billing:read", "billing:write", "scheduling:write", "persons:write", "member:self"],
        TenantManager => ["tenant:manage", "billing:read", "billing:write", "scheduling:write", "persons:write", "member:self"],
        Staff => ["scheduling:read", "attendance:write", "persons:read", "member:self"],
        Coach => ["scheduling:read", "attendance:write", "persons:read", "member:self"],
        Member => ["member:self"],
        _ => []
    };
}
