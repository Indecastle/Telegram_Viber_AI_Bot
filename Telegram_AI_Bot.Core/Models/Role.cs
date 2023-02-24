using MyTemplate.App.Core.Models.Types;
using Telegram_AI_Bot.Core.Models.Types;

namespace Telegram_AI_Bot.Core.Models;

public sealed class Role : EnumValue<string>
{
    public Role(string value) : base(value)
    {
    }

    protected Role()
    {
    }

    public static readonly Role ADMIN = new() { Value = "ADMIN" };
    public static readonly Role CLIENT_ADMIN = new() { Value = "CLIENT_ADMIN" };
    public static readonly Role CLIENT_USER = new() { Value = "CLIENT_USER" };

    private static readonly HashSet<Role> SUPERADMIN_ROLES = new HashSet<Role>
    {
        ADMIN,
    };
    
    
    private static readonly HashSet<Role> CLIENT_ROLES = new HashSet<Role>
    {
        CLIENT_ADMIN, CLIENT_USER,
    };

    public bool IsSuperAdmin()
    {
        return SUPERADMIN_ROLES.Contains(this);
    }

    public bool IsClient()
    {
        return CLIENT_ROLES.Contains(this);
    }

    public bool IsClientUser()
    {
        return CLIENT_USER == Value;
    }

    public bool IsClientAdmin()
    {
        return CLIENT_ADMIN == Value;
    }
    
    public bool IsAnyAdmin()
    {
        return CLIENT_ADMIN == Value || ADMIN == Value;
    }

    public bool IsAllowed()
    {
        if (!SUPERADMIN_ROLES.Contains(this))
            return false;

        return true;
    }

    protected override HashSet<string> PossibleValues { get; } = new()
    {
        ADMIN!,
        CLIENT_USER!,
        CLIENT_ADMIN!,
    };

    public static implicit operator string?(Role? role)
    {
        return role?.Value;
    }

    public static implicit operator Role?(string? role)
    {
        return role != null ? new Role(role) : null;
    }

    public static bool operator ==(Role? left, Role? right)
    {
        return EqualOperator(left, right);
    }

    public static bool operator !=(Role? left, Role? right)
    {
        return NotEqualOperator(left, right);
    }
}