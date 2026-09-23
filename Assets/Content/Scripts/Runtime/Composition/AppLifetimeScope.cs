using Arkanoid.Composition;
using Arkanoid.GameFlow;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Arkanoid
{
    public sealed class AppLifetimeScope : LifetimeScope
    {
        [SerializeField] private SceneNavigator _sceneNavigatorPrefab;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ScopeLifetimeProbe>(Lifetime.Scoped)
                .WithParameter("scopeName", nameof(AppLifetimeScope));
            builder.RegisterBuildCallback(resolver => resolver.Resolve<ScopeLifetimeProbe>());
            
            builder.RegisterComponentInNewPrefab(_sceneNavigatorPrefab, Lifetime.Singleton)
                .DontDestroyOnLoad();
            builder.RegisterEntryPoint<BootstrapEntryPoint>();
        }
    }
}
