namespace OrderHub.Application.Interfaces.Services;
public interface IHashShervice
{
    string Hash(string password);
    bool Verify(string password, string storedHash);
}
