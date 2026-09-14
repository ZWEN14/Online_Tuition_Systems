
##### Setup Secret for Stripe :  (need to put in your own secret key from stripe)
dotnet user-secrets set "Stripe:SecretKey" "" --project .\Online_Tuition_Systems.csproj



##### Check Secret 
dotnet user-secrets list --project .\Online_Tuition_Systems.csproj


###
Prerequisites
1. SQL Server Express LocalDB (MSSQLLocalDB) and sqlcmd are available.
2. OnlineTuitionDb already exists and its EF Core migrations have been applied.
   If the database or tables are missing, run this manually from the project
   root to create/update the development schema:

   dotnet ef database update

   This command changes the database schema. Do not rerun migrations merely
   to insert demo data.

Run the insert/refresh scripts in this order, one command at a time:

   sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d OnlineTuitionDb -b -i 'Database\AddDemoUsers.sql'
   sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d OnlineTuitionDb -b -i 'Database\SeedDemoSubjects.sql'
   sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d OnlineTuitionDb -b -i 'Database\AddDemoCourses.sql'
   sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d OnlineTuitionDb -b -i 'Database\SeedEnrollment.sql'
   sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d OnlineTuitionDb -b -i 'Database\SeedSampleSurveysAndComplaints.sql'
   sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d OnlineTuitionDb -b -i 'Database\AddDemoAnnouncementsAndEvents.sql'

AddDemoUsers creates or refreshes demo accounts. AddDemoCourses refreshes
demo categories/courses. SeedEnrollment requires the demo users and course.
AddDemoAnnouncementsAndEvents requires the users and courses and replaces
matching DEMO notifications. Rerunning seeds can refresh existing demo rows.

Finally, inspect the database without changing it:

   sqlcmd -S '(localdb)\MSSQLLocalDB' -E -d OnlineTuitionDb -b -i 'Database\InspectDatabase.sql'


{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Initial Catalog=OnlineTuitionDb;Integrated Security=True;MultipleActiveResultSets=true;TrustServerCertificate=True"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "GoogleRecaptcha": {
    "SiteKey": "6LfaE7AtAAAAAMgnGoSFfNXHd-fwBpn3i2MfFYxS",
    "ProjectId": "bmit2023-anywhereedureach",
    "ApiKey": "AIzaSyAhJia4RjKnOawb0dVyPb5t_ocDLQcPKlo"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "EnableSsl": true,
    "UserName": "hosc-wm23@student.tarc.edu.my", //can change
    "Password": "vvgg ahmp tyzi pbrh",
    "From": "hosc-wm23@student.tarc.edu.my" //can change
  }
}