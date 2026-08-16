namespace WediFrame.Modules.Billing.Services;

/// <summary>Request to open a hosted checkout for one purchase.</summary>
public sealed record PaymentCheckoutRequest(
    Guid PurchaseId,
    int AmountCents,
    string Currency,
    string Description,
    string SuccessUrl,
    string CancelUrl);

/// <summary>Result of opening checkout: gateway reference + the URL to redirect the buyer to.</summary>
public sealed record PaymentCheckoutResult(string Reference, string Url);

/// <summary>Parsed, verified webhook outcome for a completed payment.</summary>
public sealed record PaymentWebhookResult(Guid PurchaseId, string Reference, bool Paid);

/// <summary>
/// Thrown by a gateway when a webhook signature is invalid/forged, so the endpoint
/// can answer 400 without the caller depending on the concrete SDK's exception type.
/// </summary>
public sealed class PaymentSignatureException(string message, Exception? inner = null)
    : Exception(message, inner);

/// <summary>
/// Payment PORT. Callers depend on this, never on Stripe directly — the gateway is
/// swappable (like <see cref="IFiscalizationService"/>). Stripe is the current adapter.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>Short provider id, e.g. "stripe" (stored on the Purchase).</summary>
    string Provider { get; }

    /// <summary>Create a hosted checkout session and return its URL.</summary>
    Task<PaymentCheckoutResult> CreateCheckoutAsync(PaymentCheckoutRequest request, CancellationToken ct = default);

    /// <summary>
    /// Verify the webhook signature and parse it. Returns a result only for a
    /// completed, paid checkout we act on; null for anything else. Throws on a
    /// bad/forged signature so the endpoint can answer 400 (Stripe then retries).
    /// </summary>
    PaymentWebhookResult? ParseWebhook(string payload, string signatureHeader);
}
