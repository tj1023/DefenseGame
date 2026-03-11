using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

namespace Managers
{
    public class InputManager : MonoBehaviour
    {
        public static InputManager Instance { get; private set; }

        private Camera _mainCamera;
        private IClickable _currentClickable;
        private IDraggable _currentDraggable;
        private bool _isDragging;
        
        private Vector2 _pointerDownScreenPos;
        private const float DragThreshold = 10f; // 드래그 판단 기준 (픽셀)

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
            _pointerDownScreenPos = screenPos;
            
            Ray ray = _mainCamera.ScreenPointToRay(screenPos);
            
            // 2D 충돌 검사
            RaycastHit2D hit = Physics2D.GetRayIntersection(ray, Mathf.Infinity, draggableLayer);

            if (hit.collider != null)
            {
                _currentClickable = hit.collider.GetComponent<IClickable>();
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
                
                // 이동 거리가 임계값 이하면 클릭으로 간주
                if (Vector2.Distance(screenPos, _pointerDownScreenPos) <= DragThreshold)
                {
                    // 클릭일지라도 기존 드래그 상태를 안전하게 초기화 (원래 자리 드랍)
                    _currentDraggable.OnDragEnd(GetWorldPosition(_pointerDownScreenPos));

                    // 클릭 처리
                    if (_currentClickable != null)
                        _currentClickable.OnClick();
                }
                else
                {
                    // 드래그 종료 처리
                    _currentDraggable.OnDragEnd(worldPos);
                }
                
                _currentClickable = null;
                _currentDraggable = null;
                _isDragging = false;
            }
            else if (_currentClickable != null)
            {
                // 드래그 불가능하지만 클릭 가능한 오브젝트인 경우
                Vector2 screenPos = _positionAction.ReadValue<Vector2>();
                if (Vector2.Distance(screenPos, _pointerDownScreenPos) <= DragThreshold)
                    _currentClickable.OnClick();
                _currentClickable = null;
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
