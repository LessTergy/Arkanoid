using Arkanoid.Composition;
using VContainer;
using VContainer.Unity;

namespace Arkanoid
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ScopeLifetimeProbe>(Lifetime.Scoped)
                .WithParameter("scopeName", nameof(GameplayLifetimeScope));
            builder.RegisterBuildCallback(resolver => resolver.Resolve<ScopeLifetimeProbe>());
        }
    }
}
