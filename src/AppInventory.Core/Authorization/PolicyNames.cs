namespace AppInventory.Core.Authorization;

public static class PolicyNames
{
    public const string ApplicationRead = "Application.Read";
    public const string ApplicationCreate = "Application.Create";
    public const string ApplicationEditOwn = "Application.EditOwn";
    public const string ApplicationEditAny = "Application.EditAny";
    public const string ApplicationDelete = "Application.Delete";

    public const string UserDocRead = "UserDoc.Read";
    public const string UserDocWrite = "UserDoc.Write";

    public const string DeveloperDocRead = "DeveloperDoc.Read";
    public const string DeveloperDocWrite = "DeveloperDoc.Write";

    public const string OpsDocReadWrite = "OpsDoc.ReadWrite";

    public const string AuditLogRead = "AuditLog.Read";
    public const string GroupMappingManage = "GroupMapping.Manage";
    public const string UserManage = "User.Manage";

    public const string NonPublicEnvironments = "NonPublicEnvironments";
}
