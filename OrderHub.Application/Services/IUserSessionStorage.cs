using OrderHub.Application.Context;

namespace OrderHub.Application.Services;
public interface IUserSessionStorage
{
    void Save(UserSession session);
    UserSession Load();
    void Clear();
}