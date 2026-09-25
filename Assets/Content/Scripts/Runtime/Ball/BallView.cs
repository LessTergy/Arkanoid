using System;
using UnityEngine;

namespace Arkanoid.Ball
{
    internal sealed class BallView
    {
        private readonly Rigidbody2D _body;
        private readonly Transform _transform;
        private readonly Func<float> _attachedOffsetY;

        public BallView(Rigidbody2D body, Transform ballTransform, Func<float> attachedOffsetY)
        {
            _body = body;
            _transform = ballTransform;
            _attachedOffsetY = attachedOffsetY;

            _body.bodyType = RigidbodyType2D.Dynamic;
            _body.gravityScale = 0f;
            _body.simulated = false;
        }

        public Rigidbody2D Body => _body;
        public Transform Transform => _transform;

        public void HoldAbove(Transform paddle)
        {
            var paddlePosition = paddle.position;
            var position = _transform.position;
            _transform.position = new Vector3(
                paddlePosition.x, paddlePosition.y + _attachedOffsetY(), position.z);
        }

        public void Launch(Vector2 direction, float speed)
        {
            var position = _transform.position;
            _body.position = new Vector2(position.x, position.y);
            _body.simulated = true;
            _body.linearVelocity = direction * speed;
        }

        public void Stop()
        {
            _body.linearVelocity = Vector2.zero;
            _body.simulated = false;
        }
    }
}
