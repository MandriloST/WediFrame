namespace WediFrame.Modules.Admin;

/// <summary>Authorization policy that requires the Admin role. Registered by
/// <see cref="AdminModule"/>, applied to the whole /admin endpoint group.</summary>
public static class AdminPolicy
{
    public const string Name = "Admin";
}
