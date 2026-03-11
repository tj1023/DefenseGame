using Data;
using Managers;
using UnityEngine;

namespace Entities
{
    public class Enemy : MonoBehaviour
    {
        [Header("Data")]
        [SerializeField] private EnemyData enemyData;

        private float _currentHealth;
        private Transform[] _waypoints;
        private int _currentWaypointIndex;
        
        public bool IsDead { get; private set; }

        public void Initialize(EnemyData data, Transform[] waypoints)
        {
            this.enemyData = data;
            _waypoints = waypoints;
            _currentHealth = this.enemyData.maxHealth;
            _currentWaypointIndex = 0;
            IsDead = false;

            if (_waypoints.Length > 0)
            {
                transform.position = _waypoints[0].position;
            }
        }

        private void Update()
        {
            if (IsDead || _waypoints == null || _waypoints.Length == 0) return;

            MoveAlongWaypoints();
        }

        private void MoveAlongWaypoints()
        {
            if (_currentWaypointIndex < _waypoints.Length)
            {
                Transform targetWaypoint = _waypoints[_currentWaypointIndex];
                Vector3 direction = targetWaypoint.position - transform.position;
                float distance = direction.magnitude;

                if (distance <= 0.1f)
                {
                    _currentWaypointIndex++;
                }
                else
                {
                    transform.position = Vector3.MoveTowards(transform.position, targetWaypoint.position, enemyData.moveSpeed * Time.deltaTime);

                    // 회전 처리 (2D 좌우 반전)
                    if (direction.x != 0)
                    {
                        Vector3 scale = transform.localScale;
                        scale.x = direction.x > 0 ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
                        transform.localScale = scale;
                    }
                }
            }
            else
            {
                ReachEnd();
            }
        }

        public void TakeDamage(float amount)
        {
            if (IsDead) return;

            _currentHealth -= amount;
            
            if (_currentHealth <= 0) Die();
        }

        private void Die()
        {
            IsDead = true;
            GameManager.Instance.RegisterEnemyKilled(enemyData.killRewardGold);
            
            Destroy(gameObject);
        }

        private void ReachEnd()
        {
            // 루프 돌기 (다시 처음 웨이포인트로)
            _currentWaypointIndex = 0;
        }
    }
}
