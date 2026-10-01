using System.Collections.Generic;
using Arkanoid.Core.GameFlow;
using NUnit.Framework;

namespace Arkanoid.Tests.EditMode
{
    public sealed class GameSessionTests
    {
        [Test]
        public void LifeLost_ResumesToReadyAndCanStartAgain()
        {
            var session = new GameSession();
            var states = new List<GameSessionState>();
            session.StateChanged += states.Add;

            Assert.AreEqual(GameSessionState.Ready, session.State);
            Assert.IsTrue(session.TryStartPlaying());
            Assert.IsTrue(session.TryLoseLife());
            session.ResumeAfterLifeLoss();
            Assert.AreEqual(GameSessionState.Ready, session.State);
            Assert.IsTrue(session.TryStartPlaying());

            CollectionAssert.AreEqual(
                new[] { GameSessionState.Playing, GameSessionState.LifeLost, GameSessionState.Ready, GameSessionState.Playing },
                states);
        }

        [Test]
        public void LifeLost_CanEndGame()
        {
            var session = new GameSession();
            var states = new List<GameSessionState>();
            session.StateChanged += states.Add;

            session.TryStartPlaying();
            session.TryLoseLife();
            session.EndGame();
            Assert.AreEqual(GameSessionState.GameOver, session.State);

            CollectionAssert.AreEqual(
                new[] { GameSessionState.Playing, GameSessionState.LifeLost, GameSessionState.GameOver },
                states);
        }

        [Test]
        public void LevelComplete_IsTerminalInCurrentSession()
        {
            var session = new GameSession();
            var states = new List<GameSessionState>();
            session.StateChanged += states.Add;

            session.TryStartPlaying();
            session.CompleteLevel();
            Assert.AreEqual(GameSessionState.LevelComplete, session.State);
            Assert.IsFalse(session.TryStartPlaying());
            Assert.IsFalse(session.TryLoseLife());
            session.CompleteLevel();
            session.EndGame();
            Assert.AreEqual(GameSessionState.LevelComplete, session.State);

            CollectionAssert.AreEqual(
                new[] { GameSessionState.Playing, GameSessionState.LevelComplete },
                states);
        }

        [Test]
        public void InvalidTransitions_DoNotChangeStateOrRaiseEvent()
        {
            var session = new GameSession();
            var states = new List<GameSessionState>();
            session.StateChanged += states.Add;

            Assert.IsFalse(session.TryLoseLife());
            session.ResumeAfterLifeLoss();
            session.EndGame();
            session.CompleteLevel();
            Assert.AreEqual(GameSessionState.Ready, session.State);
            Assert.IsEmpty(states);

            Assert.IsTrue(session.TryStartPlaying());
            Assert.IsFalse(session.TryStartPlaying());
            session.EndGame();
            Assert.AreEqual(GameSessionState.Playing, session.State);

            Assert.IsTrue(session.TryLoseLife());
            Assert.IsFalse(session.TryLoseLife());
            Assert.IsFalse(session.TryStartPlaying());
            session.CompleteLevel();
            Assert.AreEqual(GameSessionState.LifeLost, session.State);

            session.EndGame();
            Assert.IsFalse(session.TryStartPlaying());
            Assert.IsFalse(session.TryLoseLife());
            session.EndGame();
            session.CompleteLevel();
            Assert.AreEqual(GameSessionState.GameOver, session.State);

            CollectionAssert.AreEqual(
                new[] { GameSessionState.Playing, GameSessionState.LifeLost, GameSessionState.GameOver },
                states);
        }
    }
}
