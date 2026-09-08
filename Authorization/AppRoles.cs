namespace Online_Tuition_Systems.Authorization;

public static class AppRoles
{
    public const string Admin = "Admin";
    public const string Student = "Student";
    public const string Tutor = "Tutor";
    public const string StudentOrTutor = Student + "," + Tutor;
    public const string AdminOrTutor = Admin + "," + Tutor;
    public const string ModuleUsers = Admin + "," + Student + "," + Tutor;
}
