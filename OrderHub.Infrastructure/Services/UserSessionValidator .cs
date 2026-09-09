using OrderHub.Application.Context;
using OrderHub.Application.Interfaces.Services;
using OrderHub.Application.Interfaces.Stores;
using OrderHub.Domain.Models;
using System.Threading.Tasks;

namespace OrderHub.Infrastructure.Services;
internal sealed class UserSessionValidator : IUserSessionValidator
{
    private readonly IUserRepository _userRepository;

    public UserSessionValidator(IUserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    public async Task<bool> IsValidAsync(UserSession session)
    {
        User user = await _userRepository.GetByIdAsync(session.Id);

        return user is not null && user.IsActive;
    }
}