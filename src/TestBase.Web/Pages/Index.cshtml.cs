using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Security;

namespace TestBase.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly ICurrentUserContext _currentUser;

    public IndexModel(ICurrentUserContext currentUser)
    {
        _currentUser = currentUser;
    }

    public string CurrentUserDisplayName => _currentUser.DisplayName;
    public UserRole CurrentUserRole => _currentUser.Role;

    public void OnGet()
    {
    }
}
