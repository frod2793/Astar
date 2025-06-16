using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;
using System.Linq; // LINQ 사용

public class Pathfinding : MonoBehaviour
{
    [Header("Tilemap Reference")]
    public Tilemap tilemap; // AStarSetupManager에서 이 참조를 설정해 줄 수 있습니다.
    [Header("Pathfinding Options")]
    public bool allowDiagonalMovement = true; // 대각선 이동 허용 여부

    private Grid<Node> grid; // 노드 정보를 담을 그리드
    
    public class Node
    {
        public Vector3Int position; // 타일맵에서의 셀 좌표
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

    // Grid 클래스 (Pathfinding 클래스 내부에 정의)
    public class Grid<TGridObject>
    {
        private readonly int width;
        private readonly int height;
        private readonly TGridObject[,] gridArray;
        private readonly Vector3Int originPosition;

        public Grid(int width, int height, Vector3Int origin, System.Func<Grid<TGridObject>, Vector3Int, TGridObject> createGridObject)
        {
            this.width = width;
            this.height = height;
            this.originPosition = origin; // 타일맵의 실제 시작 좌표 (bounds.min)
            gridArray = new TGridObject[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // 생성 시 실제 셀 좌표를 전달
                    Vector3Int cellPos = new Vector3Int(originPosition.x + x, originPosition.y + y, originPosition.z);
                    gridArray[x, y] = createGridObject(this, cellPos);
                }
            }
        }

        public int GetWidth() => width;
        public int GetHeight() => height;
        public Vector3Int GetOriginPosition() => originPosition;


        public TGridObject GetGridObject(Vector3Int worldPosition) // worldPosition은 실제 타일맵 셀 좌표
        {
            // 월드 좌표를 그리드 배열 인덱스로 변환
            int x = worldPosition.x - originPosition.x;
            int y = worldPosition.y - originPosition.y;

            if (x >= 0 && y >= 0 && x < width && y < height)
            {
                return gridArray[x, y];
            }
            return default(TGridObject); // 범위 밖이면 null 또는 기본값 반환
        }

        // 모든 그리드 객체에 접근하기 위한 메서드 (예시)
        public IEnumerable<TGridObject> GetAllGridObjects()
        {
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    yield return gridArray[x,y];
                }
            }
        }
    }
    
    public void InitializeGrid()
    {
        if (tilemap == null)
        {
            Debug.LogError("Tilemap is not assigned in Pathfinding script for InitializeGrid!");
            grid = null; // 그리드를 null로 설정하여 이후 작업 방지
            return;
        }

        BoundsInt bounds = tilemap.cellBounds;
        if (bounds.size.x == 0 || bounds.size.y == 0)
        {
            Debug.LogWarning("Tilemap bounds are zero, grid not initialized. Map might not be generated yet.");
            grid = null; // 유효하지 않은 경계면 그리드를 null로 설정
            return;
        }
        
        grid = new Grid<Node>(bounds.size.x, bounds.size.y, bounds.min,
            (g, cellPos) => { // cellPos는 실제 타일맵 좌표
                bool isInitiallyWalkable = tilemap.HasTile(cellPos);
                // 실제 이동 가능 여부는 UpdateWalkableNodes에서 최종 결정됨
                return new Node(cellPos, isInitiallyWalkable);
            });
        Debug.Log($"Pathfinding Grid Initialized. Origin: {bounds.min}, Size: ({bounds.size.x}, {bounds.size.y})");
    }

    // AStarSetupManager가 호출하여 장애물 정보를 업데이트하고 노드의 walkability를 설정하는 함수
    public void UpdateWalkableNodes(HashSet<Vector3Int> obstaclePositions, Vector3Int startPos, Vector3Int endPos)
    {
        if (grid == null)
        {
            // 그리드가 아직 초기화되지 않았으면 먼저 초기화 시도
            Debug.LogWarning("Grid was null in UpdateWalkableNodes. Attempting to initialize.");
            InitializeGrid();
            if (grid == null) // 초기화 후에도 null이면 중단
            {
                 Debug.LogError("Grid could not be initialized for UpdateWalkableNodes.");
                 return;
            }
        }

        // 모든 노드를 순회하며 walkability 업데이트
        foreach (Node node in grid.GetAllGridObjects())
        {
            if (node == null) continue;

            node.ResetCosts(); // 경로 탐색 전 비용 초기화
            TileBase currentTile = tilemap.GetTile(node.position);


            if (obstaclePositions.Contains(node.position))
            {
                node.isWalkable = false;
            }
            else if (currentTile == null && !(node.position == startPos || node.position == endPos)) // 타일이 아예 없는 곳 (시작/끝 제외)
            {
                node.isWalkable = false; // 타일이 없는 곳은 이동 불가
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
         Debug.Log("Walkable nodes updated based on obstacles.");
    }


    public List<Vector3Int> FindPath(Vector3Int startWorldPos, Vector3Int targetWorldPos)
    {
        if (grid == null)
        {
            Debug.LogError("Grid is not initialized. Cannot find path.");
            return null;
        }

        Node startNode = grid.GetGridObject(startWorldPos);
        Node targetNode = grid.GetGridObject(targetWorldPos);

        if (startNode == null || targetNode == null)
        {
            Debug.LogError($"Start node ({startWorldPos}) or Target node ({targetWorldPos}) is out of grid bounds.");
            return null;
        }
        if (!startNode.isWalkable)
        {
            Debug.LogWarning($"Start node ({startWorldPos}) is not walkable.");
            // return null; // 시작점이 막혀있어도 탐색을 시도할지, 아니면 바로 중단할지 결정
        }
        if (!targetNode.isWalkable)
        {
            Debug.LogWarning($"Target node ({targetWorldPos}) is not walkable.");
            // return null; // 도착점이 막혀있어도 탐색을 시도할지, 아니면 바로 중단할지 결정
        }


        List<Node> openList = new List<Node>();
        HashSet<Node> closedList = new HashSet<Node>();

        openList.Add(startNode);
        startNode.gCost = 0;
        startNode.hCost = CalculateHeuristic(startNode, targetNode);
        // startNode.parent는 Node 생성자에서 이미 null로 초기화됨

        while (openList.Count > 0)
        {
            // F 코스트가 가장 낮은 노드 선택 (간단한 LINQ 사용, 성능 최적화 필요시 우선순위 큐 사용)
            Node currentNode = openList.OrderBy(node => node.FCost).ThenBy(node => node.hCost).First();

            openList.Remove(currentNode);
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

                // 현재 노드에서 이웃 노드까지의 실제 이동 비용 (여기서는 단순화하여 휴리스틱과 동일하게 처리)
                // 실제로는 직선/대각선에 따라 10 또는 14 같은 값을 사용해야 함
                int moveCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (moveCostToNeighbour < neighbour.gCost || !openList.Contains(neighbour))
                {
                    neighbour.gCost = moveCostToNeighbour;
                    neighbour.hCost = CalculateHeuristic(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openList.Contains(neighbour))
                    {
                        openList.Add(neighbour);
                    }
                }
            }
        }

        Debug.LogWarning("Path not found from " + startWorldPos + " to " + targetWorldPos);
        return null; // 경로를 찾지 못함
    }

    List<Node> GetNeighbours(Node node)
{
    List<Node> neighbours = new List<Node>();
    
    // 상하좌우 (4방향) - 항상 포함
    Vector3Int[] cardinalOffsets = new Vector3Int[]
    {
        new Vector3Int(0, 1, 0),  // 상
        new Vector3Int(0, -1, 0), // 하
        new Vector3Int(1, 0, 0),  // 우
        new Vector3Int(-1, 0, 0)  // 좌
    };
    
    // 대각선 방향 (4방향) - 옵션에 따라 포함
    Vector3Int[] diagonalOffsets = new Vector3Int[]
    {
        new Vector3Int(1, 1, 0),   // 우상
        new Vector3Int(1, -1, 0),  // 우하
        new Vector3Int(-1, 1, 0),  // 좌상
        new Vector3Int(-1, -1, 0)  // 좌하
    };
    
    // 상하좌우 이웃 추가
    foreach (Vector3Int offset in cardinalOffsets)
    {
        Vector3Int neighbourPos = node.position + offset;
        Node neighbourNode = grid.GetGridObject(neighbourPos);
        if (neighbourNode != null)
        {
            neighbours.Add(neighbourNode);
        }
    }
    
    // 대각선 이동이 허용된 경우에만 대각선 이웃 추가
    if (allowDiagonalMovement)
    {
        foreach (Vector3Int offset in diagonalOffsets)
        {
            Vector3Int neighbourPos = node.position + offset;
            Node neighbourNode = grid.GetGridObject(neighbourPos);
            if (neighbourNode != null)
            {
                // 대각선 이동 추가 조건: 벽 모서리를 통과하지 못하도록 설정
                Vector3Int horizontalPos = new Vector3Int(node.position.x + offset.x, node.position.y, node.position.z);
                Vector3Int verticalPos = new Vector3Int(node.position.x, node.position.y + offset.y, node.position.z);
                
                Node horizontalNode = grid.GetGridObject(horizontalPos);
                Node verticalNode = grid.GetGridObject(verticalPos);
                
                // 모서리 체크를 하지 않거나, 두 인접 노드 중 하나 이상이 이동 가능하면 대각선 이동 허용
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

    // 두 노드 사이의 실제 이동 비용 계산 (G 코스트 계산용)
    int GetDistance(Node nodeA, Node nodeB)
    {
        int dstX = Mathf.Abs(nodeA.position.x - nodeB.position.x);
        int dstY = Mathf.Abs(nodeA.position.y - nodeB.position.y);

        // 일반적인 비용: 직선 10, 대각선 14
        if (dstX > dstY)
            return 14 * dstY + 10 * (dstX - dstY);
        return 14 * dstX + 10 * (dstY - dstX);
    }

    // 휴리스틱 비용 계산 (H 코스트 계산용) - GetDistance와 동일한 로직 사용 가능 (Admissible & Consistent)
    int CalculateHeuristic(Node a, Node b)
    {
        return GetDistance(a, b); // 여기서는 GetDistance와 동일한 휴리스틱 사용
    }

    List<Vector3Int> RetracePath(Node startNode, Node endNode)
    {
        List<Vector3Int> path = new List<Vector3Int>();
        Node currentNode = endNode;

        while (currentNode != startNode && currentNode != null) // currentNode가 null이 되는 경우 방지
        {
            path.Add(currentNode.position);
            currentNode = currentNode.parent;
        }
        if (currentNode == startNode && startNode != null) // 시작 노드도 경로에 포함
        {
            path.Add(startNode.position);
        }
        path.Reverse(); // 시작점 -> 끝점 순서로
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