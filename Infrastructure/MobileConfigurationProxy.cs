using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Primitives;

namespace BaqueanoAutoTest.Infrastructure;

/// <summary>
/// Proxy sobre IConfiguration que redirige las claves de cantidad de tests
/// de TestSettings:* → {sectionName}:* para que los ITest usen los conteos
/// configurados para el viewport (Mobile / Tablet) sin cambios de código.
/// Todas las demás claves se delegan al inner sin modificación.
/// </summary>
public sealed class MobileConfigurationProxy : IConfiguration
{
    private readonly IConfiguration _inner;
    private readonly string _sectionName;

    private static readonly HashSet<string> _redirected =
        new(StringComparer.OrdinalIgnoreCase)
        {
            "TestSettings:TotalPerfilesTests",
            "TestSettings:TotalUsuariosTests",
            "TestSettings:TotalParametrosTests",
            "TestSettings:ContactosStateTests",
            "TestSettings:ContactosDeleteCount"
        };

    /// <param name="sectionName">"MobileSettings" | "TabletSettings"</param>
    public MobileConfigurationProxy(IConfiguration inner, string sectionName = "MobileSettings")
    {
        _inner       = inner;
        _sectionName = sectionName;
    }

    public string? this[string key]
    {
        get
        {
            if (_redirected.Contains(key))
            {
                // TestSettings:TotalPerfilesTests → {section}:TotalPerfilesTests
                var sectionKey = _sectionName + ":" + key["TestSettings:".Length..];
                return _inner[sectionKey] ?? _inner[key];   // fallback al valor desktop
            }
            return _inner[key];
        }
        set => _inner[key] = value;
    }

    public IConfigurationSection GetSection(string key) => _inner.GetSection(key);
    public IEnumerable<IConfigurationSection> GetChildren() => _inner.GetChildren();
    public IChangeToken GetReloadToken() => _inner.GetReloadToken();
}
