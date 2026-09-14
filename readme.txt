## Prerequisites
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