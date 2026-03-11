using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance { get; private set; }
        
        [Header("Board Settings")]
        [SerializeField] private int rows = 3;
        [SerializeField] private int cols = 5;
        [SerializeField] private float cellSize = 2f;
        [SerializeField] private Vector3 boardOrigin = Vector3.zero;

        // 그리드의 타일 위치들을 기록
        private Vector3[,] _gridPositions;
        // 각 셀에 있는 유닛 참조
        private Entities.Unit[,] _gridUnits;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeBoard();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeBoard()
        {
            _gridPositions = new Vector3[cols, rows];
            _gridUnits = new Entities.Unit[cols, rows];

            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    _gridPositions[x, y] = boardOrigin + new Vector3(x * cellSize, y * cellSize, 0);
                }
            }
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.cyan;
            Vector3 origin = boardOrigin;
            
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    Vector3 pos = origin + new Vector3(x * cellSize, y * cellSize, 0);
                    Gizmos.DrawWireCube(pos, new Vector3(cellSize, cellSize, 0.1f) * 0.9f);
                }
            }
        }
        
        // 빈 셀 중 하나를 무작위로 선택하여 반환
        public bool TryGetRandomEmptyCell(out Vector2Int cellIndex, out Vector3 worldPos)
        {
            List<Vector2Int> emptyCells = new List<Vector2Int>();

            for (int x = 0; x < cols; x++)
                for (int y = 0; y < rows; y++)
                    if (_gridUnits[x, y] == null)
                        emptyCells.Add(new Vector2Int(x, y));

            if (emptyCells.Count > 0)
            {
                int randomIndex = Random.Range(0, emptyCells.Count);
                cellIndex = emptyCells[randomIndex];
                worldPos = _gridPositions[cellIndex.x, cellIndex.y];
                return true;
            }

            cellIndex = Vector2Int.zero;
            worldPos = Vector3.zero;
            return false;
        }

        public void RegisterUnitAt(Vector2Int cellIndex, Entities.Unit unitObj)
        {
            if (cellIndex.x >= 0 && cellIndex.x < cols && cellIndex.y >= 0 && cellIndex.y < rows)
                _gridUnits[cellIndex.x, cellIndex.y] = unitObj;
        }

        public void UnregisterUnitAt(Vector2Int cellIndex)
        {
            if (cellIndex.x >= 0 && cellIndex.x < cols && cellIndex.y >= 0 && cellIndex.y < rows)
                _gridUnits[cellIndex.x, cellIndex.y] = null;
        }
    }
}
