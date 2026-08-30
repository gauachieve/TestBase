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

    public DevDemoModel(
        IBankIdProvider bankId,
        IVippsClient vipps,
        ISmsSender sms,
        IEmailSender email,
        IAuditLogger auditLogger,
        ICurrentUserContext currentUser)
    {
        _bankId = bankId;
        _vipps = vipps;
        _sms = sms;
        _email = email;
        _auditLogger = auditLogger;
        _currentUser = currentUser;
    }

    public BankIdResult? BankIdResult { get; private set; }
    public VippsPaymentResult? VippsResult { get; private set; }

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        BankIdResult = await _bankId.AuthenticateAsync(cancellationToken: cancellationToken);
        VippsResult = await _vipps.ChargeAsync(0m, "Dev-demo testbelastning", cancellationToken);
        await _sms.SendAsync("+4700000000", "Dev-demo: dette er en test-SMS.", cancellationToken);
        await _email.SendAsync("dev@example.test", "Dev-demo", "Dette er en test-e-post.", cancellationToken);

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
