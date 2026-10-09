using System.Collections;
using System.Collections.Generic;
using Arkanoid.Ball;
using Arkanoid.Bricks;
using NUnit.Framework;
using UnityEngine;

namespace Arkanoid.Tests.PlayMode
{
    internal static class BrickHitTestSupport
    {
        public static Dictionary<BrickView, BrickCollisionProbe> Prepare(BrickView[] bricks)
        {
            var probes = new Dictionary<BrickView, BrickCollisionProbe>();
            foreach (var brick in bricks)
            {
                Assert.That(brick.State, Is.Not.Null, "LevelView must initialize blocks during injection.");
                var collider = brick.GetComponent<Collider2D>();
                Assert.That(collider, Is.Not.Null, $"Brick {brick.name} needs a Collider2D.");
                Assert.That(collider.isTrigger, Is.False);
                collider.enabled = false;
                probes.Add(brick, brick.gameObject.AddComponent<BrickCollisionProbe>());
            }

            return probes;
        }

        public static IEnumerator Hit(BallController ball, BrickView brick, BrickCollisionProbe probe)
        {
            var body = ball.GetComponent<Rigidbody2D>();
            var ballCollider = ball.GetComponent<CircleCollider2D>();
            var brickCollider = brick.GetComponent<Collider2D>();
            var brickName = brick.name;
            var collisionCount = probe.CollisionCount;
            Assert.That(ball.State, Is.EqualTo(BallState.Flying));
            Assert.That(ballCollider.enabled, Is.True);
            Assert.That(ballCollider.isTrigger, Is.False);
            Assert.That(Physics2D.GetIgnoreLayerCollision(ball.gameObject.layer, brick.gameObject.layer), Is.False);

            body.simulated = false;
            brickCollider.enabled = false;
            yield return new WaitForFixedUpdate();
            yield return null;

            brickCollider.enabled = true;
            Physics2D.SyncTransforms();
            var bounds = brickCollider.bounds;
            Assert.That(bounds.size.x, Is.GreaterThan(0f));
            Assert.That(bounds.size.y, Is.GreaterThan(0f));
            var offset = (Vector2)ball.transform.TransformVector(ballCollider.offset);
            var radius = ballCollider.radius * Mathf.Abs(ball.transform.lossyScale.y);
            var startPosition = new Vector2(bounds.center.x - offset.x, bounds.min.y - radius - 0.1f - offset.y);
            ResumePhysicsAt(body, startPosition, Vector2.up * 8f);

            var deadline = Time.realtimeSinceStartup + 2f;
            while (probe.CollisionCount == collisionCount && Time.realtimeSinceStartup < deadline)
            {
                yield return new WaitForFixedUpdate();
                yield return null;
            }

            Assert.That(probe.CollisionCount, Is.EqualTo(collisionCount + 1),
                $"Brick {brickName} must receive exactly one ball collision within 2 seconds. "
                + $"Start: {startPosition}, ball: {body.position}, target: {bounds}, "
                + $"velocity: {body.linearVelocity}, simulated: {body.simulated}.");
            if (ball.State == BallState.Flying)
            {
                Assert.That(body.linearVelocity.y, Is.LessThan(0f), "A surviving flight must reflect off the brick.");
            }

            if (brick != null)
            {
                brickCollider.enabled = false;
            }

            body.simulated = false;
        }

        public static void ResumePhysicsAt(Rigidbody2D body, Vector2 position, Vector2 velocity)
        {
            body.simulated = true;
            body.transform.position = new Vector3(position.x, position.y, body.transform.position.z);
            Physics2D.SyncTransforms();
            body.linearVelocity = velocity;
        }
    }
}
