using UnityEngine;

namespace Managers
{
    /// <summary>
    /// InputManager에 의해 감지되어 터치/클릭이 가능한 객체가 구현해야 할 인터페이스
    /// </summary>
    public interface IClickable
    {
        void OnClick();
    }
    
    /// <summary>
    /// InputManager에 의해 감지되어 터치/마우스 드래그가 가능한 객체가 구현해야 할 인터페이스
    /// </summary>
    public interface IDraggable
    {
        void OnDragStart(Vector2 position);
        void OnDrag(Vector2 position);
        void OnDragEnd(Vector2 position);
    }
}
