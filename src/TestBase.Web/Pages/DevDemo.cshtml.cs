using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.RazorPages;
using TestBase.Shared.Providers;
using TestBase.Shared.Security;

namespace TestBase.Web.Pages;

public sealed class DevDemoModel : PageModel
{
    private readonly IBankIdProvider _bankId;
    private readonly IVippsClient _vipps;
    private readonly ISmsSender _sms;
    private readonly IEmailSender _email;
    private readonly IAuditLogger _auditLogger;
    private readonly ICurrentUserContext _currentUser;
    private readonly IAuthenticationSchemeProvider _schemes;

    public DevDemoModel(
        IBankIdProvider bankId,
        IVippsClient vipps,
        ISmsSender sms,
        IEmailSender email,
        IAuditLogger auditLogger,
        ICurrentUserContext currentUser,
        IAuthenticationSchemeProvider schemes)
    {
        _bankId = bankId;
        _vipps = vipps;
        _sms = sms;
        _email = email;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
        _schemes = schemes;
    }

    public BankIdResult? BankIdResult { get; private set; }
    public VippsPaymentResult? VippsResult { get; private set; }
    public bool EktBankIdTilgjengelig { get; private set; }
    public string? SmsFeilmelding { get; private set; }
    public string? EpostFeilmelding { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        EktBankIdTilgjengelig = await _schemes.GetSchemeAsync("BankIdTest") is not null;

        BankIdResult = await _bankId.AuthenticateAsync(cancellationToken: cancellationToken);
        VippsResult = await _vipps.ChargeAsync(0m, "Dev-demo testbelastning", cancellationToken);

        // Fiktive mottakere ("+4700000000"/"dev@example.test") — mock-leverandørene bryr seg
        // ikke, men ekte leverandører (Azure Communication Services/Vonage, når konfigurert,
        // se Program.cs) kan avvise dem. Fanger feilen i stedet for å la hele siden krasje,
        // siden dette kun er en diagnostisk demo-side.
        try
        {
            await _sms.SendAsync("+4700000000", "Dev-demo: dette er en test-SMS.", cancellationToken);
        }
        catch (Exception ex)
        {
            SmsFeilmelding = ex.Message;
        }

        try
        {
            await _email.SendAsync("dev@example.test", "Dev-demo", "Dette er en test-e-post.", cancellationToken);
        }
        catch (Exception ex)
        {
            EpostFeilmelding = ex.Message;
        }

        await _auditLogger.LogAsync(
            _currentUser.UserId,
            _currentUser.Role.ToString(),
            action: "ViewDevDemo",
            entityType: "DevDemoPage",
            entityId: "n/a",
            details: "Dev-demo-siden ble besøkt, mock-leverandører ble kalt.",
            cancellationToken: cancellationToken);
    }
}
