using UnityEngine;

namespace Arkanoid.Ball
{
    internal abstract class BallStateBase
    {
        public abstract BallState Id { get; }

        public abstract void Enter();

        public virtual BallStateBase Update()
        {
            return null;
        }

        public virtual void LateUpdate()
        {
        }

        public virtual void FixedUpdate()
        {
        }

        public virtual void OnCollisionEnter2D(Collision2D collision)
        {
        }

#if UNITY_EDITOR
        public virtual void OnDrawGizmos()
        {
        }
#endif

    }
}
