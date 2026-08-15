using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using WediFrame.Modules.Billing.Configuration;

namespace WediFrame.Modules.Billing.Services;

/// <summary>
/// Stripe Checkout adapter. Uses the hosted Checkout page (no card data touches us)
/// and verifies webhooks by signature. Classic service-class API (stable across
/// Stripe.net v46–v51). Per-call ApiKey via RequestOptions keeps it free of global state.
/// </summary>
public sealed class StripePaymentGateway(IOptions<StripeOptions> options) : IPaymentGateway
{
    private readonly StripeOptions _options = options.Value;

    public string Provider => "stripe";

    public async Task<PaymentCheckoutResult> CreateCheckoutAsync(
        PaymentCheckoutRequest request, CancellationToken ct = default)
    {
        if (!_options.Enabled)
        {
            throw new InvalidOperationException("Stripe is not configured — set Stripe:SecretKey.");
        }

        var sessionOptions = new SessionCreateOptions
        {
            Mode = "payment",
            SuccessUrl = request.SuccessUrl,
            CancelUrl = request.CancelUrl,
            ClientReferenceId = request.PurchaseId.ToString(),
            Metadata = new Dictionary<string, string>
            {
                ["purchaseId"] = request.PurchaseId.ToString(),
            },
            LineItems =
            [
                new SessionLineItemOptions
                {
                    Quantity = 1,
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        Currency = request.Currency.ToLowerInvariant(),
                        UnitAmount = request.AmountCents,
                        ProductData = new SessionLineItemPriceDataProductDataOptions
                        {
                            Name = request.Description,
                        },
                    },
                },
            ],
        };

        var service = new SessionService();
        var session = await service.CreateAsync(
            sessionOptions, new RequestOptions { ApiKey = _options.SecretKey }, ct);

        return new PaymentCheckoutResult(session.Id, session.Url);
    }

    public PaymentWebhookResult? ParseWebhook(string payload, string signatureHeader)
    {
        // Verify the signature. throwOnApiVersionMismatch: false — the account's
        // webhook API version usually differs from the SDK's pinned version, and
        // that mismatch must NOT be treated as an error (only a bad signature is).
        Event stripeEvent;
        try
        {
            stripeEvent = EventUtility.ConstructEvent(
                payload, signatureHeader, _options.WebhookSecret, throwOnApiVersionMismatch: false);
        }
        catch (StripeException ex)
        {
            // Keep the Stripe type from leaking to callers (module boundary).
            throw new PaymentSignatureException("Invalid Stripe webhook signature.", ex);
        }

        if (stripeEvent.Type != "checkout.session.completed"
            || stripeEvent.Data.Object is not Session session)
        {
            return null;
        }

        var paid = string.Equals(session.PaymentStatus, "paid", StringComparison.OrdinalIgnoreCase);
        if (!paid || !Guid.TryParse(session.ClientReferenceId, out var purchaseId))
        {
            return null;
        }

        return new PaymentWebhookResult(purchaseId, session.Id, true);
    }
}
