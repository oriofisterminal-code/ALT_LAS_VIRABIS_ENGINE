using Xunit;
using Virabis.Core.DI;

namespace Virabis.Core.Tests;

/// <summary>
/// Tests for DI Container.
/// "Tembelim, uzatmayalım çok" - Mehmet Temel (İnşaat Mühendisi)
/// </summary>
public class ServiceContainerTests
{
    public ServiceContainerTests()
    {
        Services.Reset();
    }

    // ========================================
    // REGISTRATION TESTS
    // ========================================

    [Fact, Trait("Category", "Smoke")]
    public void Register_Transient_GetReturnsNewInstance()
    {
        var container = new ServiceContainer();
        container.Register<IWeapon, TestWeapon>();

        var weapon1 = container.Get<IWeapon>();
        var weapon2 = container.Get<IWeapon>();

        Assert.NotSame(weapon1, weapon2);
    }

    [Fact, Trait("Category", "Unit")]
    public void Register_Singleton_GetReturnsSameInstance()
    {
        var container = new ServiceContainer();
        container.Register<IWeapon, TestWeapon>(ServiceLifetime.Singleton);

        var weapon1 = container.Get<IWeapon>();
        var weapon2 = container.Get<IWeapon>();

        Assert.Same(weapon1, weapon2);
    }

    [Fact, Trait("Category", "Unit")]
    public void Register_Factory_WorksCorrectly()
    {
        var container = new ServiceContainer();
        var counter = 0;

        container.Register<IWeapon>(c =>
        {
            counter++;
            return new TestWeapon { Damage = counter * 10 };
        });

        var weapon = container.Get<IWeapon>();

        Assert.Equal(10, weapon.Damage);
        Assert.Equal(1, counter);
    }

    [Fact, Trait("Category", "Unit")]
    public void RegisterInstance_ReturnsSameInstance()
    {
        var container = new ServiceContainer();
        var instance = new TestWeapon { Damage = 999 };

        container.RegisterInstance<IWeapon>(instance);

        var weapon = container.Get<IWeapon>();

        Assert.Same(instance, weapon);
        Assert.Equal(999, weapon.Damage);
    }

    // ========================================
    // GET TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Get_NotRegistered_Throws()
    {
        var container = new ServiceContainer();

        Assert.Throws<InvalidOperationException>(() => container.Get<IWeapon>());
    }

    [Fact, Trait("Category", "Unit")]
    public void TryGet_NotRegistered_ReturnsNull()
    {
        var container = new ServiceContainer();

        var weapon = container.TryGet<IWeapon>();

        Assert.Null(weapon);
    }

    [Fact, Trait("Category", "Unit")]
    public void IsRegistered_Correct()
    {
        var container = new ServiceContainer();

        Assert.False(container.IsRegistered<IWeapon>());

        container.Register<IWeapon, TestWeapon>();

        Assert.True(container.IsRegistered<IWeapon>());
    }

    // ========================================
    // SCOPE TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Scope_CreatesChildContainer()
    {
        var container = new ServiceContainer();
        container.Register<IWeapon, TestWeapon>(ServiceLifetime.Singleton);

        using var scope = container.CreateScope();

        Assert.NotNull(scope.Container);
    }

    // ========================================
    // GLOBAL SERVICES TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Services_Global_QuickAccess()
    {
        Services.Register<IWeapon, TestWeapon>();

        var weapon = Services.Get<IWeapon>();

        Assert.NotNull(weapon);
        Assert.IsType<TestWeapon>(weapon);

        Services.Reset();
    }

    // ========================================
    // DISPOSE TESTS
    // ========================================

    [Fact, Trait("Category", "Unit")]
    public void Dispose_DisposesSingletons()
    {
        var container = new ServiceContainer();
        container.Register<IDisposable, DisposableService>(ServiceLifetime.Singleton);

        var service = container.Get<IDisposable>();

        container.Dispose();

        Assert.True(((DisposableService)service).IsDisposed);
    }

    // ========================================
    // TEST HELPERS
    // ========================================

    private interface IWeapon
    {
        int Damage { get; }
    }

    private class TestWeapon : IWeapon
    {
        public int Damage { get; set; } = 10;
    }

    private class DisposableService : IDisposable
    {
        public bool IsDisposed { get; private set; }

        public void Dispose() => IsDisposed = true;
    }
}
