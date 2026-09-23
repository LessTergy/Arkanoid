using System;
using UnityEngine;
using VContainer.Unity;

namespace Arkanoid.Composition
{
    internal sealed class ScopeLifetimeProbe : IStartable, IDisposable
    {
        private readonly string _scopeName;

        public ScopeLifetimeProbe(string scopeName)
        {
            _scopeName = scopeName;
            Debug.Log($"[VContainer] {_scopeName} created.");
        }

        public void Start()
        {
            Debug.Log($"[VContainer] {_scopeName} started.");
        }

        public void Dispose()
        {
            Debug.Log($"[VContainer] {_scopeName} disposed.");
        }
    }
}
