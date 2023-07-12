using System.Security.Claims;
using SMDataServiceProto.V1;

namespace BlazorLibrary.ServiceColection
{
    public interface IAuthenticationService
    {
        //Task<RegistrationResponseDto> RegisterUser(UserForRegistrationDto userForRegistration);
        Task<bool> Login(RequestLogin userForAuthentication);
        //Task<bool> Refresh(string NameUser);
        Task<bool> RemoteLogin(RequestLogin userForAuthentication);

        Task<ClaimsPrincipal> GetUser();

        void SetNewUser(IEnumerable<Claim> claims);

        Task Logout();

        Task<bool> SetTokenAsync(string token);
    }
}
