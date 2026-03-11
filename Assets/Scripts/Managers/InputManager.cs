using UnityEngine;
using UnityEngine.InputSystem;
using Entities;
using UnityEngine.EventSystems;

namespace Managers
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        private Camera _mainCamera;
        private IDraggable _currentDraggable;
        private bool _isDragging;

        [Header("Settings")]
        [SerializeField] private LayerMask draggableLayer;

        private InputAction _pressAction;
        private InputAction _positionAction;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                _mainCamera = Camera.main;
                InitializeInputs();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeInputs()
        {
            // <Pointer> 바인딩은 마우스 좌클릭, 모바일 터치, 펜 입력을 모두 하나로 통합하여 감지함
            _pressAction = new InputAction("Press", binding: "<Pointer>/press");
            _pressAction.Enable();

            _positionAction = new InputAction("Position", binding: "<Pointer>/position");
            _positionAction.Enable();
        }

        private void OnDestroy()
        {
            if (_pressAction != null)
            {
                _pressAction.Disable();
                _pressAction.Dispose();
            }
            if (_positionAction != null)
            {
                _positionAction.Disable();
                _positionAction.Dispose();
            }
        }

        private void Update()
        {
            if (_pressAction == null || _positionAction == null) return;

            if (_pressAction.WasPressedThisFrame())
                HandlePointerDown();
            else if (_pressAction.WasReleasedThisFrame())
                HandlePointerUp();

            // 드래그 중일 때는 매 프레임 위치를 갱신
            if (_isDragging && _currentDraggable != null)
            {
                Vector2 pointerScreenPos = _positionAction.ReadValue<Vector2>();
                Vector2 pointerWorldPos = GetWorldPosition(pointerScreenPos);
                _currentDraggable.OnDrag(pointerWorldPos);
            }
        }

        private void HandlePointerDown()
        {
            // 터치/클릭한 곳이 UI 위라면 무시
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject()) return;

            Vector2 screenPos = _positionAction.ReadValue<Vector2>();
            Vector2 worldPos = GetWorldPosition(screenPos);
            
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            
            // 2D 충돌 검사
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, draggableLayer);

            if (hit.collider != null)
            {
                _currentDraggable = hit.collider.GetComponent<IDraggable>();
                if (_currentDraggable != null)
                {
                    _isDragging = true;
                    _currentDraggable.OnDragStart(worldPos);
                }
            }
        }

        private void HandlePointerUp()
        {
            if (_isDragging && _currentDraggable != null)
            {
                Vector2 screenPos = _positionAction.ReadValue<Vector2>();
                Vector2 worldPos = GetWorldPosition(screenPos);
                
                _currentDraggable.OnDragEnd(worldPos);
                
                _currentDraggable = null;
                _isDragging = false;
            }
        }

        /// <summary>
        /// 스크린 좌표를 2D 월드 좌표로 변환
        /// </summary>
        private Vector2 GetWorldPosition(Vector2 screenPos)
        {
            Vector3 screenPos3D = new Vector3(screenPos.x, screenPos.y, 10f); // 카메라 Z: -10
            return _mainCamera.ScreenToWorldPoint(screenPos3D);
        }
    }
}
