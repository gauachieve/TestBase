using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using TestBase.Shared.Data;
using TestBase.Shared.Domain.Pasienter;
using TestBase.Shared.Security;

namespace TestBase.Web.Areas.Behandlerportal.Pages.Pasienter;

public sealed class DetaljerModel : PageModel
{
    private readonly AppDbContext _db;
    private readonly ICurrentUserContext _currentUser;

    public DetaljerModel(AppDbContext db, ICurrentUserContext currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    public Pasient? Pasient { get; private set; }

    public async Task<IActionResult> OnGetAsync(long id, CancellationToken cancellationToken)
    {
        var behandlerId = long.TryParse(_currentUser.UserId.Split(':').LastOrDefault(), out var bid) ? bid : 0;

        Pasient = await _db.Pasienter.FirstOrDefaultAsync(p => p.Id == id && p.BehandlerId == behandlerId, cancellationToken);
        if (Pasient is null)
        {
            return NotFound();
        }

        return Page();
    }
}
