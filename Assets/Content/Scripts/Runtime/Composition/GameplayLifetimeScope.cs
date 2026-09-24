using Arkanoid.Composition;
using Arkanoid.Input;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Arkanoid
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private InputSystemPlayerInput _playerInput;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_playerInput).As<IPlayerInput>();
            builder.Register<ScopeLifetimeProbe>(Lifetime.Scoped)
                .WithParameter("scopeName", nameof(GameplayLifetimeScope));
            builder.RegisterBuildCallback(resolver => resolver.Resolve<ScopeLifetimeProbe>());
        }
    }
}
