namespace Online_Tuition_Systems.Services.Security;

public interface IPasswordHasher
{
    string Hash(string password);

    bool Verify(string password, string storedHash);
}
