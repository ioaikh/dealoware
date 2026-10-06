using Dealoware.Domain.Admin;

namespace Dealoware.Api.Tests;

public sealed class CapturingAdminMailSender : IAdminMailSender
{
    private readonly List<AdminMailMessage> _sent = [];
    private readonly object _gate = new();

    public IReadOnlyList<AdminMailMessage> Sent
    {
        get
        {
            lock (_gate)
            {
                return _sent.ToList();
            }
        }
    }

    public Task SendAsync(AdminMailMessage message, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _sent.Add(message);
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        lock (_gate)
        {
            _sent.Clear();
        }
    }
}

public sealed class FakeAdminSecondFactorVerifier : IAdminSecondFactorVerifier
{
    public string ValidTotp { get; set; } = "123456";

    public string ValidRecoveryCode { get; set; } = "unused-recovery-code";

    public HashSet<string> SpentRecoveryCodes { get; } = new(StringComparer.Ordinal);

    public Task<AdminSecondFactorVerifyResult> VerifyAsync(
        string email,
        string? totpCode,
        string? recoveryCode,
        CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(totpCode) && !string.IsNullOrWhiteSpace(recoveryCode))
        {
            return Task.FromResult(AdminSecondFactorVerifyResult.Failed);
        }

        if (!string.IsNullOrWhiteSpace(totpCode)
            && string.Equals(totpCode.Trim(), ValidTotp, StringComparison.Ordinal))
        {
            return Task.FromResult(AdminSecondFactorVerifyResult.TotpSucceeded);
        }

        if (!string.IsNullOrWhiteSpace(recoveryCode))
        {
            var code = recoveryCode.Trim();
            if (string.Equals(code, ValidRecoveryCode, StringComparison.Ordinal)
                && SpentRecoveryCodes.Add(code))
            {
                return Task.FromResult(AdminSecondFactorVerifyResult.RecoverySucceeded);
            }
        }

        return Task.FromResult(AdminSecondFactorVerifyResult.Failed);
    }

    public void Reset()
    {
        SpentRecoveryCodes.Clear();
        ValidTotp = "123456";
        ValidRecoveryCode = "unused-recovery-code";
    }
}

public sealed class FakeAdminClock : IAdminClock
{
    private DateTimeOffset _utcNow = DateTimeOffset.UtcNow;

    public DateTimeOffset UtcNow
    {
        get => _utcNow;
        set => _utcNow = value;
    }

    public void Advance(TimeSpan delta) => _utcNow = _utcNow.Add(delta);
}

public sealed class CapturingAdminResetIpThrottle : IAdminResetIpThrottle
{
    private readonly List<string> _failures = [];
    private readonly object _gate = new();

    public IReadOnlyList<string> Failures
    {
        get
        {
            lock (_gate)
            {
                return _failures.ToList();
            }
        }
    }

    public Task RecordFailureAsync(string ipHmac, CancellationToken cancellationToken)
    {
        lock (_gate)
        {
            _failures.Add(ipHmac);
        }

        return Task.CompletedTask;
    }

    public void Clear()
    {
        lock (_gate)
        {
            _failures.Clear();
        }
    }
}
