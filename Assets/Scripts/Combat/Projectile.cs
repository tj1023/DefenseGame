using Entities;
using Managers;
using UnityEngine;

namespace Combat
{
    public class Projectile : MonoBehaviour, IPoolable
    {
        [Header("Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifeTime = 3f;

        private float _damage;
        private Enemy _target;
        private bool _isInitialized;
        private bool _isDeactivated;

        public void Initialize(Enemy target, float damage)
        {
            _target = target;
            _damage = damage;
            _isInitialized = true;
        }

        public void OnSpawn()
        {
            _isDeactivated = false;
            // 풀에서 꺼내질 때마다 수명 타이머 재설정
            CancelInvoke(nameof(Deactivate));
            Invoke(nameof(Deactivate), lifeTime);
        }

        private void Deactivate()
        {
            if (_isDeactivated) return;
            
            _isDeactivated = true;
            _isInitialized = false;
            CancelInvoke(nameof(Deactivate));
            
            ObjectPoolManager.Instance.Despawn(gameObject);
        }

        private void Update()
        {
            if (!_isInitialized || _isDeactivated) return;

            if (_target == null || _target.IsDead)
            {
                // 타겟이 죽거나 사라지면 투사체 비활성화
                Deactivate();
                return;
            }

            // 타겟을 향해 이동 (2D 회전 적용)
            Vector3 direction = _target.transform.position - transform.position;
            float distanceThisFrame = speed * Time.deltaTime;

            if (direction.sqrMagnitude <= distanceThisFrame * distanceThisFrame)
            {
                HitTarget();
            }
            else
            {
                transform.Translate(direction.normalized * distanceThisFrame, Space.World);
                
                float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);
            }
        }

        private void HitTarget()
        {
            if (_target != null && !_target.IsDead)
            {
                _target.TakeDamage(_damage);
            }
            Deactivate();
        }
    }
}
