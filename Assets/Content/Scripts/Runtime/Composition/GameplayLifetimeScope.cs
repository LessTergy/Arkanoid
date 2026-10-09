using Arkanoid.Ball;
using Arkanoid.Composition;
using Arkanoid.Core.Bricks;
using Arkanoid.Core.GameFlow;
using Arkanoid.Core.Score;
using Arkanoid.GameFlow;
using Arkanoid.Input;
using Arkanoid.Levels;
using Arkanoid.Paddle;
using Arkanoid.Playfield;
using Arkanoid.UI;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Arkanoid
{
    public sealed class GameplayLifetimeScope : LifetimeScope
    {
        [Header("Input")]
        [SerializeField] private InputSystemPlayerInput _playerInput;

        [Header("Paddle")]
        [SerializeField] private PaddleMovement _paddleMovement;
        [SerializeField] private PaddleConfig _paddleConfig;

        [Header("Ball")]
        [SerializeField] private BallController _ballController;
        [SerializeField] private BallConfig _ballConfig;

        [Header("Level")]
        [SerializeField] private PlayfieldCamera _playfieldCamera;
        [SerializeField] private DeathZone _deathZone;
        [SerializeField] private LevelView _levelView;

        [Header("UI")]
        [SerializeField] private GameplayHudView _hudView;

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(_playerInput).AsSelf().As<IPlayerInput>();
            builder.RegisterComponent(_paddleMovement);
            builder.RegisterInstance(_paddleConfig);
            builder.RegisterComponent(_ballController);
            builder.RegisterInstance(_ballConfig);
            builder.RegisterComponent(_playfieldCamera);
            builder.RegisterComponent(_deathZone);
            builder.RegisterComponent(_levelView);
            builder.RegisterComponent(_hudView);
            builder.Register<GameSession>(Lifetime.Scoped);
            builder.Register<LivesModel>(Lifetime.Scoped);
            builder.Register(_ => new BrickHitProcessor(
                new IndestructibleHitHandler(new ShieldHitHandler(new DamageHitHandler()))), Lifetime.Singleton);
            builder.Register<ComboModel>(Lifetime.Singleton);
            builder.Register(resolver =>
                {
                    var baseScore = new BaseScoreCalculator();
                    var comboScore = new ComboScoreDecorator(baseScore, resolver.Resolve<ComboModel>());
                    return new DoubleScoreDecorator(comboScore);
                }, Lifetime.Singleton)
                .AsSelf()
                .As<IScoreCalculator>();
            builder.Register<ScoreService>(Lifetime.Singleton);
            builder.RegisterEntryPoint<GameplayPauseController>(Lifetime.Scoped).AsSelf();
            builder.RegisterEntryPoint<LaunchHandler>(Lifetime.Scoped);
            builder.RegisterEntryPoint<LifeLossHandler>(Lifetime.Scoped);
            builder.RegisterEntryPoint<GameplayScoreHandler>(Lifetime.Scoped);
            builder.RegisterEntryPoint<LevelFinishedHandler>(Lifetime.Scoped);
            builder.RegisterEntryPoint<GameplayHudPresenter>(Lifetime.Scoped);
            builder.Register<ScopeLifetimeProbe>(Lifetime.Scoped)
                .WithParameter("scopeName", nameof(GameplayLifetimeScope));
            builder.RegisterBuildCallback(resolver => resolver.Resolve<ScopeLifetimeProbe>());
        }
    }
}
