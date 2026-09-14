ONLINE TUITION SYSTEM (Anywhere Edureach)
BMIT2023 Web and Mobile Systems | RIS2(S3), Group 5

REQUIREMENTS
Windows, .NET 10 SDK, SQL Server Express LocalDB (MSSQLLocalDB),
and internet access. Use Visual Studio or VS Code.

RUN THE PROJECT
Extract the solution ZIP. Keep these database files inside App_Data:
  OnlineTuitionDb.mdf
  OnlineTuitionDb_log.ldf

From the project folder, run in PowerShell:
  dotnet restore .\Online_Tuition_Systems.csproj
  dotnet run --project .\Online_Tuition_Systems.csproj --launch-profile AnywhereEdureach -p:UseAppHost=false

Open https://localhost:7100 and complete CAPTCHA to log in.
Development startup automatically applies pending database migrations.

TEST ACCOUNTS (current supplied database)
Role          Email                 Password
Administrator admin@test.com        password
Tutor         tutor@test.com        password
Student       student@test.com      password

Running Database/AddDemoUsers.sql resets these passwords to password123.
Use read.txt for optional fresh demo-data setup; reseeding is not required
when using the supplied database.

EXTERNAL SERVICES
Login requires valid GoogleRecaptcha configuration. Email verification and
recovery require Smtp settings; checkout requires Stripe test configuration.
Developer user-secrets are not included in the ZIP. Obtain evaluation settings
securely from the team; do not include private service keys in this README.


