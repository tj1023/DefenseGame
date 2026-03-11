using Entities;
using UnityEngine;

namespace Combat
{
    public class Projectile : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private float speed = 10f;
        [SerializeField] private float lifeTime = 3f;

        private float _damage;
        private Enemy _target;
        private bool _isInitialized;

        public void Initialize(Enemy target, float damage)
        {
            _target = target;
            _damage = damage;
            _isInitialized = true;

            // 일정 시간 후 혹시 모를 누수 방지를 위해 파괴
            Destroy(gameObject, lifeTime);
        }

        private void Update()
        {
            if (!_isInitialized) return;

            if (_target == null || _target.IsDead)
            {
                // 타겟이 죽거나 사라지면 투사체 파괴
                Destroy(gameObject);
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
            Destroy(gameObject);
        }
    }
}
