using System;
using System.Threading.Tasks;

namespace OrderHub.Application.Interfaces.Services;

public interface ICacheService
{
    Task<T> GetOrCreateAsync<T>(string key, Func<Task<T>> factory, TimeSpan? expiration = null);
    T GetOrCreate<T>(string key, Func<T> factory, TimeSpan? expiration = null);
    void Remove(string key);
    void RemoveByPrefix(string prefix);
    void Clear();
}
