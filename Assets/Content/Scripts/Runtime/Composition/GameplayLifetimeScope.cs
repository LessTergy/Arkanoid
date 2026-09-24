using Arkanoid.Ball;
using Arkanoid.Composition;
using Arkanoid.Input;
using Arkanoid.Paddle;
using Arkanoid.Playfield;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Arkanoid
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [SerializeField] private InputSystemPlayerInput _playerInput;
        [SerializeField] private PaddleMovement _paddleMovement;
        [SerializeField] private PaddleConfig _paddleConfig;
        [SerializeField] private BallController _ballController;
        [SerializeField] private BallConfig _ballConfig;
        [SerializeField] private PlayfieldCamera _playfieldCamera;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_playerInput).As<IPlayerInput>();
            builder.RegisterComponent(_paddleMovement);
            builder.RegisterInstance(_paddleConfig);
            builder.RegisterComponent(_ballController);
            builder.RegisterInstance(_ballConfig);
            builder.RegisterComponent(_playfieldCamera);
            builder.Register<ScopeLifetimeProbe>(Lifetime.Scoped)
                .WithParameter("scopeName", nameof(GameplayLifetimeScope));
            builder.RegisterBuildCallback(resolver => resolver.Resolve<ScopeLifetimeProbe>());
        }
    }
}
