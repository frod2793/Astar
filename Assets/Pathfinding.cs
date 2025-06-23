using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;


public class Pathfinding : MonoBehaviour
{
    [Header("Tilemap Reference")] public Tilemap tilemap;
    [Header("Pathfinding Options")] public bool allowDiagonalMovement = true;

    private Grid<Node> grid;

    public class Node
    {
        public Vector3Int position;
        public bool isWalkable;
        public int gCost;
        public int hCost;
        public Node parent;

        public int FCost => gCost + hCost;

        public Node(Vector3Int pos, bool walkable)
        {
            position = pos;
            isWalkable = walkable;
            gCost = int.MaxValue;
            hCost = 0;
            parent = null;
        }

        public void ResetCosts()
        {
            gCost = int.MaxValue;
            hCost = 0;
            parent = null;
        }
    }

    public class Grid<TGridObject>
    {
        private readonly int width;
        private readonly int height;
        private readonly TGridObject[,] gridArray;
        private readonly Vector3Int originPosition;

        public Grid(int width, int height, Vector3Int origin,
            System.Func<Grid<TGridObject>, Vector3Int, TGridObject> createGridObject)
        {
            this.width = width;
            this.height = height;
            this.originPosition = origin;
            gridArray = new TGridObject[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Vector3Int cellPos = new Vector3Int(originPosition.x + x, originPosition.y + y, originPosition.z);
                    gridArray[x, y] = createGridObject(this, cellPos);
                }
            }
        }

        public int GetWidth() => width;
        public int GetHeight() => height;
        public Vector3Int GetOriginPosition() => originPosition;

        public TGridObject GetGridObject(Vector3Int worldPosition)
        {
            int x = worldPosition.x - originPosition.x;
            int y = worldPosition.y - originPosition.y;

            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                return gridArray[x, y];
            }

            return default(TGridObject);
        }

        public IEnumerable<TGridObject> GetAllGridObjects()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    yield return gridArray[x, y];
                }
            }
        }
    }

    public void InitializeGrid()
    {
        if (tilemap == null)
        {
            Debug.LogError("타일맵이 Pathfinding 스크립트의 InitializeGrid 함수에 할당되지 않았습니다!");
            grid = null;
            return;
        }

        BoundsInt bounds = tilemap.cellBounds;
        if (bounds.size.x == 0 || bounds.size.y == 0)
        {
            Debug.LogWarning("타일맵 경계가 0입니다. 그리드가 초기화되지 않았습니다. 맵이 아직 생성되지 않았을 수 있습니다.");
            grid = null;
            return;
        }

        grid = new Grid<Node>(bounds.size.x, bounds.size.y, bounds.min,
            (g, cellPos) =>
            {
                bool isInitiallyWalkable = tilemap.HasTile(cellPos);
                return new Node(cellPos, isInitiallyWalkable);
            });
        Debug.Log($"경로 탐색 그리드 초기화 완료. 원점: {bounds.min}, 크기: ({bounds.size.x}, {bounds.size.y})");
    }

    public void UpdateWalkableNodes(HashSet<Vector3Int> obstaclePositions, Vector3Int startPos, Vector3Int endPos)
    {
        if (grid == null)
        {
            Debug.LogWarning("UpdateWalkableNodes에서 그리드가 null입니다. 초기화를 시도합니다.");
            InitializeGrid();
            if (grid == null)
            {
                Debug.LogError("UpdateWalkableNodes 함수에서 그리드를 초기화할 수 없습니다.");
                return;
            }
        }

        foreach (Node node in grid.GetAllGridObjects())
        {
            if (node == null) continue;

            node.ResetCosts();
            TileBase currentTile = tilemap.GetTile(node.position);

            if (obstaclePositions.Contains(node.position))
            {
                node.isWalkable = false;
            }
            else if (currentTile == null &&
                     !(node.position == startPos || node.position == endPos))
            {
                node.isWalkable = false;
            }
            else
            {
                node.isWalkable = true;
            }

            if (node.position == startPos || node.position == endPos)
            {
                node.isWalkable = true;
            }
        }

        Debug.Log("장애물 정보를 기반으로 이동 가능한 노드가 업데이트되었습니다.");
    }

    public List<Vector3Int> FindPath(Vector3Int startWorldPos, Vector3Int targetWorldPos)
    {
        if (grid == null)
        {
            Debug.LogError("그리드가 초기화되지 않았습니다. 경로를 찾을 수 없습니다.");
            return null;
        }

        Node startNode = grid.GetGridObject(startWorldPos);
        Node targetNode = grid.GetGridObject(targetWorldPos);

        if (startNode == null || targetNode == null)
        {
            Debug.LogError($"시작 노드 ({startWorldPos}) 또는 목표 노드 ({targetWorldPos})가 그리드 범위를 벗어났습니다.");
            return null;
        }

        if (!startNode.isWalkable)
        {
            Debug.LogWarning($"시작 노드 ({startWorldPos})는 이동이 불가능한 위치입니다.");
            // return null;
        }

        if (!targetNode.isWalkable)
        {
            Debug.LogWarning($"목표 노드 ({targetWorldPos})는 이동이 불가능한 위치입니다.");
            // return null;
        }

        // 우선순위 큐 사용
        NodePriorityQueue openQueue = new NodePriorityQueue();
        HashSet<Node> closedList = new HashSet<Node>();

        openQueue.Add(startNode);
        startNode.gCost = 0;
        startNode.hCost = CalculateHeuristic(startNode, targetNode);

        while (openQueue.Count > 0)
        {
            // 우선순위 큐에서 최소 비용 노드를 O(log n)으로 추출
            Node currentNode = openQueue.RemoveFirst();
            closedList.Add(currentNode);

            if (currentNode == targetNode)
            {
                // 경로 찾음!
                return RetracePath(startNode, targetNode);
            }

            foreach (Node neighbour in GetNeighbours(currentNode))
            {
                if (neighbour == null || !neighbour.isWalkable || closedList.Contains(neighbour))
                {
                    continue;
                }

                int moveCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (moveCostToNeighbour < neighbour.gCost || !openQueue.Contains(neighbour))
                {
                    neighbour.gCost = moveCostToNeighbour;
                    neighbour.hCost = CalculateHeuristic(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openQueue.Contains(neighbour))
                        openQueue.Add(neighbour);
                    else
                        openQueue.UpdateItem(neighbour);
                }
            }
        }

        Debug.LogWarning($"경로를 찾을 수 없습니다. 시작 위치: {startWorldPos}, 목표 위치: {targetWorldPos}");
        return null;
    }

    List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        Vector3Int[] cardinalOffsets = new Vector3Int[]
        {
            new Vector3Int(0, 1, 0), // 상
            new Vector3Int(0, -1, 0), // 하
            new Vector3Int(1, 0, 0), // 우
            new Vector3Int(-1, 0, 0) // 좌
        };

        Vector3Int[] diagonalOffsets = new Vector3Int[]
        {
            new Vector3Int(1, 1, 0), // 우상
            new Vector3Int(1, -1, 0), // 우하
            new Vector3Int(-1, 1, 0), // 좌상
            new Vector3Int(-1, -1, 0) // 좌하
        };

        foreach (Vector3Int offset in cardinalOffsets)
        {
            Vector3Int neighbourPos = node.position + offset;
            Node neighbourNode = grid.GetGridObject(neighbourPos);
            if (neighbourNode != null)
            {
                neighbours.Add(neighbourNode);
            }
        }

        if (allowDiagonalMovement)
        {
            foreach (Vector3Int offset in diagonalOffsets)
            {
                Vector3Int neighbourPos = node.position + offset;
                Node neighbourNode = grid.GetGridObject(neighbourPos);
                if (neighbourNode != null)
                {
                    Vector3Int horizontalPos =
                        new Vector3Int(node.position.x + offset.x, node.position.y, node.position.z);
                    Vector3Int verticalPos =
                        new Vector3Int(node.position.x, node.position.y + offset.y, node.position.z);

                    Node horizontalNode = grid.GetGridObject(horizontalPos);
                    Node verticalNode = grid.GetGridObject(verticalPos);

                    if ((horizontalNode == null || horizontalNode.isWalkable) ||
                        (verticalNode == null || verticalNode.isWalkable))
                    {
                        neighbours.Add(neighbourNode);
                    }
                }
            }
        }

        return neighbours;
    }

    /// <summary>
    /// 두 노드 간의 거리를 계산하는 함수 - 옥타일 거리(Octile Distance) 사용
    /// 대각선 이동 비용(14)과 직선 이동 비용(10)을 조합하여 최단 경로 비용 계산
    /// </summary>
    /// <param name="nodeA">시작 노드</param>
    /// <param name="nodeB">목표 노드</param>
    /// <returns>두 노드 사이의 예상 이동 비용 (대각선 및 직선 이동 조합)</returns>
    int GetDistance(Node nodeA, Node nodeB)
    {
        // X축과 Y축의 거리 차이 계산
        int dstX = Mathf.Abs(nodeA.position.x - nodeB.position.x);
        int dstY = Mathf.Abs(nodeA.position.y - nodeB.position.y);

        // 옥타일 거리 계산:
        // 1. 대각선 이동 비용(14) = 10 * √2 ≈ 14
        // 2. 직선 이동 비용(10)
        // 최대한 대각선으로 이동한 후 남은 거리는 직선으로 이동
        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    /// <summary>
    /// A* 알고리즘의 휴리스틱 함수 - 목표까지의 예상 비용을 추정
    /// 휴리스틱은 항상 실제 비용을 과대평가하지 않아야 최적의 경로를 보장함(허용 가능한 휴리스틱)
    /// </summary>
    /// <param name="a">현재 노드</param>
    /// <param name="b">목표 노드</param>
    /// <returns>현재 노드에서 목표까지의 예상 비용</returns>
    int CalculateHeuristic(Node a, Node b)
    {
        // 옥타일 거리를 휴리스틱으로 사용
        // F = G + H에서 H 부분을 구성하며, 
        // G: 시작점에서 현재 노드까지의 실제 비용
        // H: 현재 노드에서 목표까지의 추정 비용
        return GetDistance(a, b);
    }
    List<Vector3Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Node currentNode = endNode;

        while (currentNode != startNode && currentNode != null)
        {
            path.Add(currentNode.position);
            currentNode = currentNode.parent;
        }

        if (currentNode == startNode && startNode != null)
        {
            path.Add(startNode.position);
        }

        path.Reverse();
        return path;
    }
    //  디버깅을 위해 Gizmos를 사용하여 그리드나 노드 정보 시각화
    // void OnDrawGizmos()
    // {
    //     if (grid != null)
    //     {
    //         foreach (Node node in grid.GetAllGridObjects())
    //         {
    //             if (node == null) continue;
    //             Gizmos.color = node.isWalkable ? Color.white : Color.red;
    //             if (pathfinding != null && pathfinding.GetComponent<AStarSetupManager>() != null) // 예시
    //             {
    //                 // 현재 경로에 포함된 노드 시각화 등
    //             }
    //             Vector3 worldPos = tilemap.GetCellCenterWorld(node.position);
    //             Gizmos.DrawCube(worldPos, Vector3.one * 0.8f); // 셀 크기에 맞게 조절
    //         }
    //     }
    // }
}