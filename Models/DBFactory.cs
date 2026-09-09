//using Microsoft.EntityFrameworkCore;
//using Microsoft.EntityFrameworkCore.Design;

//namespace AnywhereEdureach.Models;

//// Used ONLY by EF Core's design-time tooling (Add-Migration and
//// Update-Database in the Package Manager Console, or `dotnet ef` on the
//// command line). Program.cs uses top-level statements + the minimal
//// hosting model, which can occasionally confuse EF's automatic discovery
//// of the configured connection string when running design-time commands -
//// this factory sidesteps that by providing it directly.
////
//// NOTE: if you change the connection string in Program.cs, update it
//// here too so migrations keep targeting the same database.
//public class DBFactory : IDesignTimeDbContextFactory<DB>
//{
//    public DB CreateDbContext(string[] args)
//    {
//        var optionsBuilder = new DbContextOptionsBuilder<DB>();

//        // Package Manager Console's working directory for design-time
//        // commands (Add-Migration, Update-Database) is the PROJECT folder
//        // - the same folder Program.cs's ContentRootPath resolves to at
//        // runtime. Using Directory.GetCurrentDirectory() here (instead of
//        // AppContext.BaseDirectory, which is the bin\Debug\... build
//        // output folder) keeps both pointed at the exact same .mdf file.
//        optionsBuilder.UseSqlServer(@"
//            Data Source=(LocalDB)\MSSQLLocalDB;
//            AttachDbFilename=" + Directory.GetCurrentDirectory() + @"\AnywhereEdureach.mdf;
//        ");

//        return new DB(optionsBuilder.Options);
//    }
//}
