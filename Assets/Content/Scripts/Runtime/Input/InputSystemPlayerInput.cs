using UnityEngine;
using UnityEngine.InputSystem;

namespace Arkanoid.Input
{
    public sealed class InputSystemPlayerInput : MonoBehaviour, IPlayerInput
    {
        [SerializeField] private InputActionReference _move;
        [SerializeField] private InputActionReference _touchPosition;
        [SerializeField] private InputActionReference _touchPress;
        [SerializeField] private InputActionReference _launch;
        [SerializeField] private InputActionReference _pause;
        [SerializeField] private Camera _gameplayCamera;

        public PlayerMoveIntent Move
        {
            get
            {
                if (_touchPress.action.IsPressed())
                {
                    var position = _touchPosition.action.ReadValue<Vector2>();
                    var worldPosition = _gameplayCamera.ScreenToWorldPoint(new Vector3(position.x, position.y, 0f));
                    return new PlayerMoveIntent(0f, worldPosition.x);
                }

                return new PlayerMoveIntent(_move.action.ReadValue<float>(), null);
            }
        }

        public bool LaunchPressedThisFrame => _launch.action.WasPressedThisFrame();
        public bool PausePressedThisFrame => _pause.action.WasPressedThisFrame();

        private void OnEnable()
        {
            _move.action.Enable();
            _touchPosition.action.Enable();
            _touchPress.action.Enable();
            _launch.action.Enable();
            _pause.action.Enable();
        }

        private void OnDisable()
        {
            _move.action.Disable();
            _touchPosition.action.Disable();
            _touchPress.action.Disable();
            _launch.action.Disable();
            _pause.action.Disable();
        }
    }
}
