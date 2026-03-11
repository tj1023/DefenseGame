using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class BoardManager : MonoBehaviour
    {
        public static BoardManager Instance { get; private set; }
        
        [Header("Board Settings")]
        [SerializeField] private int rows = 3;
        [SerializeField] private int cols = 7;
        [SerializeField] private float cellSize = 0.5f;
        [SerializeField] private Vector3 boardOrigin = Vector3.zero;

        [Header("Grid Visualization")]
        [SerializeField] private Color gridColor = new (0, 1, 1, 0.5f);
        private readonly List<LineRenderer> _gridLines = new();

        private const int MaxUnitsPerCell = 3;

        // 그리드의 타일 위치들을 기록
        private Vector3[,] _gridPositions;
        // 각 셀에 있는 유닛 리스트 (최대 MaxUnitsPerCell개)
        private List<Entities.Unit>[,] _gridUnits;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                InitializeBoard();
                InitializeGridVisuals();
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void InitializeBoard()
        {
            _gridPositions = new Vector3[cols, rows];
            _gridUnits = new List<Entities.Unit>[cols, rows];

            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    _gridPositions[x, y] = boardOrigin + new Vector3(x * cellSize, y * cellSize, 0);
                    _gridUnits[x, y] = new List<Entities.Unit>();
                }
            }
        }
        
        private void InitializeGridVisuals()
        {
            GameObject gridParent = new GameObject("GridVisualizer");
            gridParent.transform.SetParent(transform);
            
            Material lineMat = new Material(Shader.Find("Sprites/Default"));

            Vector3 bottomLeft = boardOrigin + new Vector3(-cellSize / 2f, -cellSize / 2f, 0);

            // 가로선
            for (int y = 0; y <= rows; y++)
            {
                Vector3 startPos = bottomLeft + new Vector3(0, y * cellSize, 0);
                Vector3 endPos = bottomLeft + new Vector3(cols * cellSize, y * cellSize, 0);
                CreateLineRenderer(gridParent.transform, lineMat, startPos, endPos);
            }
            
            // 세로선
            for (int x = 0; x <= cols; x++)
            {
                Vector3 startPos = bottomLeft + new Vector3(x * cellSize, 0, 0);
                Vector3 endPos = bottomLeft + new Vector3(x * cellSize, rows * cellSize, 0);
                CreateLineRenderer(gridParent.transform, lineMat, startPos, endPos);
            }

            ShowGrid(false);
        }

        private void CreateLineRenderer(Transform parent, Material mat, Vector3 start, Vector3 end)
        {
            GameObject lineObj = new GameObject("GridLine");
            lineObj.transform.SetParent(parent);
            LineRenderer lr = lineObj.AddComponent<LineRenderer>();
            
            lr.material = mat;
            lr.startColor = gridColor;
            lr.endColor = gridColor;
            lr.startWidth = 0.05f;
            lr.endWidth = 0.05f;
            lr.positionCount = 2;
            lr.useWorldSpace = true;
            lr.SetPosition(0, start);
            lr.SetPosition(1, end);
            lr.sortingOrder = -1; // 유닛 뒤에 렌더링
            
            _gridLines.Add(lr);
        }

        public void ShowGrid(bool isVisible)
        {
            foreach (var lr in _gridLines)
                if (lr != null)
                    lr.enabled = isVisible;
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

        /// <summary>
        /// 특정 유닛 데이터를 기준으로 묶음 배치가 가능한 빈 공간을 찾습니다.
        /// 1순위: 동일한 UnitData가 있으면서 수량이 MaxUnitsPerCell 미만인 셀
        /// 2순위: 완전히 빈 셀
        /// </summary>
        public bool TryGetCellForSummon(Data.UnitData targetData, out Vector2Int cellIndex, out Vector3 worldPos)
        {
            List<Vector2Int> emptyCells = new List<Vector2Int>();
            List<Vector2Int> matchingCells = new List<Vector2Int>();

            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    int count = _gridUnits[x, y].Count;
                    if (count == 0)
                    {
                        emptyCells.Add(new Vector2Int(x, y));
                    }
                    else if (count < MaxUnitsPerCell)
                    {
                        // 해당 셀의 유닛 타입이 타겟과 같은지 검사 (모두 같은 그룹이므로 첫번째만 확인)
                        if (_gridUnits[x, y][0].UnitData == targetData)
                        {
                            matchingCells.Add(new Vector2Int(x, y));
                        }
                    }
                }
            }

            // 1순위 반환
            if (matchingCells.Count > 0)
            {
                int r = Random.Range(0, matchingCells.Count);
                cellIndex = matchingCells[r];
                worldPos = _gridPositions[cellIndex.x, cellIndex.y];
                return true;
            }

            // 2순위 반환
            if (emptyCells.Count > 0)
            {
                int r = Random.Range(0, emptyCells.Count);
                cellIndex = emptyCells[r];
                worldPos = _gridPositions[cellIndex.x, cellIndex.y];
                return true;
            }

            cellIndex = Vector2Int.zero;
            worldPos = Vector3.zero;
            return false;
        }

        public bool TryGetCellIndexFromWorldPos(Vector3 worldPos, out Vector2Int cellIndex)
        {
            float halfCell = cellSize / 2f;
            for (int x = 0; x < cols; x++)
            {
                for (int y = 0; y < rows; y++)
                {
                    Vector3 cellPos = _gridPositions[x, y];
                    if (worldPos.x >= cellPos.x - halfCell && worldPos.x <= cellPos.x + halfCell &&
                        worldPos.y >= cellPos.y - halfCell && worldPos.y <= cellPos.y + halfCell)
                    {
                        cellIndex = new Vector2Int(x, y);
                        return true;
                    }
                }
            }
            
            cellIndex = Vector2Int.zero;
            return false;
        }

        public void SwapCells(Vector2Int sourceCell, Vector2Int targetCell)
        {
            if (!IsValidCell(sourceCell) || !IsValidCell(targetCell)) return;
            if (sourceCell == targetCell) return;

            // 내용물 스왑
            List<Entities.Unit> sourceList = _gridUnits[sourceCell.x, sourceCell.y];
            List<Entities.Unit> targetList = _gridUnits[targetCell.x, targetCell.y];

            _gridUnits[sourceCell.x, sourceCell.y] = targetList;
            _gridUnits[targetCell.x, targetCell.y] = sourceList;

            // 각 유닛들에게 내부 Index 업데이트 지시
            foreach (var unit in _gridUnits[sourceCell.x, sourceCell.y])
                unit.UpdateTargetCellIndex(sourceCell);

            foreach (var unit in _gridUnits[targetCell.x, targetCell.y])
                unit.UpdateTargetCellIndex(targetCell);

            // 시각적 정렬 업데이트
            UpdateCellVisuals(sourceCell);
            UpdateCellVisuals(targetCell);
        }

        public void RegisterUnitAt(Vector2Int cellIndex, Entities.Unit unitObj)
        {
            if (IsValidCell(cellIndex))
            {
                if (!_gridUnits[cellIndex.x, cellIndex.y].Contains(unitObj))
                {
                    _gridUnits[cellIndex.x, cellIndex.y].Add(unitObj);
                    UpdateCellVisuals(cellIndex);
                }
            }
        }

        public void UnregisterUnitAt(Vector2Int cellIndex, Entities.Unit unitObj)
        {
            if (IsValidCell(cellIndex))
                if (_gridUnits[cellIndex.x, cellIndex.y].Remove(unitObj))
                    UpdateCellVisuals(cellIndex);
        }

        public List<Entities.Unit> GetUnitsAt(Vector2Int cellIndex)
        {
            if (IsValidCell(cellIndex))
            {
                // 외부에서 리스트를 맘대로 수정하지 못하도록 새 리스트 반환
                return new List<Entities.Unit>(_gridUnits[cellIndex.x, cellIndex.y]);
            }
            return new List<Entities.Unit>();
        }

        private bool IsValidCell(Vector2Int cellIndex)
        {
            return cellIndex.x >= 0 && cellIndex.x < cols && cellIndex.y >= 0 && cellIndex.y < rows;
        }

        public void UpdateCellVisuals(Vector2Int cellIndex)
        {
            if (!IsValidCell(cellIndex)) return;

            List<Entities.Unit> units = _gridUnits[cellIndex.x, cellIndex.y];
            int count = units.Count;
            if (count == 0) return;

            Vector3 centerPos = _gridPositions[cellIndex.x, cellIndex.y];
            
            // cellSize에 비례하여 오프셋을 설정 (셀 크기가 작아지면 더 촘촘히 밀집됨)
            float offX = cellSize * 0.2f; 
            float offY = cellSize * 0.15f; 

            // 유닛 개수에 따라 배치: 1->중앙, 2->좌우, 3->삼각형
            switch (count)
            {
                case 1:
                    units[0].transform.position = centerPos;
                    break;
                case 2:
                    units[0].transform.position = centerPos + new Vector3(-offX, 0, 0);
                    units[1].transform.position = centerPos + new Vector3(offX, 0, 0);
                    break;
                case 3:
                    units[0].transform.position = centerPos + new Vector3(0, offY * 1.5f, 0);
                    units[1].transform.position = centerPos + new Vector3(-offX, -offY, 0);
                    units[2].transform.position = centerPos + new Vector3(offX, -offY, 0);
                    break;
            }
        }
    }
}
