using Arkanoid.Composition;
using VContainer;
using VContainer.Unity;

namespace Arkanoid
{
    public sealed class AppLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<ScopeLifetimeProbe>(Lifetime.Scoped)
                .WithParameter("scopeName", nameof(AppLifetimeScope));
        }
    }
}
