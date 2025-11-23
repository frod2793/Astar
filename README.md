🤖 A* Pathfinding Algorithm Visualizer
프로젝트 목표
이 프로젝트는 A* 알고리즘의 동작 원리를 시각적으로 증명하고, 핵심 로직 구현 능력을 확보하는 것을 목표로 합니다. 사용자가 직접 맵의 조건을 설정하고, 알고리즘이 최적 경로를 찾아내는 전 과정을 확인할 수 있는 인터랙티브 비주얼라이저를 개발했습니다.

주요 기능 및 적용 기술
🎨 사용자 커스텀 기능
맵 크기 조절: 사용자가 원하는 크기(N x M)를 직접 입력하면, 그에 맞춰 실시간으로 타일맵 그리드가 생성됩니다.

자유로운 노드 배치: 마우스 클릭만으로 시작점, 도착점, 그리고 경로를 막는 장애물을 간편하게 설정할 수 있습니다.

대각선 이동 옵션: 대각선 이동 허용/불허 옵션을 제공하여, 다양한 조건에서 경로 탐색을 실험할 수 있습니다.

🧠 핵심 알고리즘 구현
우선순위 큐(Min-Heap) 활용: 성능 최적화를 위해 우선순위 큐(Min-Heap) 자료구조를 활용하여 A* 알고리즘의 Open Set을 처리했습니다. 이를 통해 매 탐색 시 가장 비용(f=g+h)이 낮은 노드를 효율적으로 찾아냅니다.

동적 로직 변경: 대각선 이동 옵션에 따라 경로 비용을 추정하는 **휴리스틱 함수(Heuristic)**와 이웃 노드를 탐색하는 로직이 동적으로 변경되도록 설계하여 알고리즘의 유연성을 확보했습니다.

## 📜 알고리즘 상세 설명

A* 알고리즘은 최적 경로를 보장하면서도 효율적으로 경로를 탐색하는 대표적인 알고리즘입니다. 이 프로젝트에서는 다음과 같은 핵심 개념을 코드로 구현하여 알고리즘의 동작 원리를 시각적으로 증명했습니다.

### 1. 노드(Node)와 비용(Cost) 계산

모든 타일은 `Node` 객체로 관리되며, 각 노드는 다음과 같은 비용(Cost) 정보를 가집니다.

- **G Cost**: 시작 노드로부터 현재 노드까지의 실제 이동 비용
- **H Cost**: 현재 노드로부터 도착 노드까지의 예상 이동 비용 (Heuristic)
- **F Cost**: G Cost와 H Cost의 합 (`F = G + H`)

A* 알고리즘은 **F Cost가 가장 낮은 노드를 우선적으로 탐색**하여 최적 경로를 찾아냅니다.

```csharp
// Pathfinding.cs
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
```

### 2. Open Set과 Closed Set

- **Open Set (우선순위 큐)**: 앞으로 탐색할 노드들의 집합입니다. 이 프로젝트에서는 `Min-Heap`으로 구현된 우선순위 큐를 사용하여, F Cost가 가장 낮은 노드를 `O(log n)`의 시간 복잡도로 빠르게 추출할 수 있도록 최적화했습니다.
- **Closed Set (해시셋)**: 이미 탐색이 완료된 노드들의 집합입니다. `HashSet`을 사용하여 특정 노드가 Closed Set에 포함되어 있는지 `O(1)`의 시간 복잡도로 빠르게 확인할 수 있습니다.

```csharp
// Pathfinding.cs
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
```

### 3. 휴리스틱 함수 (Heuristic Function)

휴리스틱 함수는 현재 노드에서 목적지까지의 예상 거리를 계산하는 함수입니다. 최적 경로를 보장하기 위해서는 **실제 거리보다 과대평가하지 않는 값(Admissible Heuristic)**을 사용해야 합니다.

이 프로젝트에서는 대각선 이동을 허용하는 경우를 고려하여 **옥타일 거리(Octile Distance)** 방식을 사용했습니다. 이는 직선 이동 비용과 대각선 이동 비용을 함께 고려하여 더 정확한 예상 거리를 계산합니다.

- 직선 이동 비용: 10
- 대각선 이동 비용: 14 (10 * √2의 근사치)

```csharp
// Pathfinding.cs

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
```

### 4. 동적 이웃 노드 탐색

사용자가 '대각선 이동 허용' 옵션을 변경하면, 이웃 노드를 탐색하는 로직이 동적으로 변경됩니다. 대각선 이동 시에는 코너를 통과할 수 없는 상황을 방지하기 위해, 이동하려는 대각선 방향의 두 직선 경로가 모두 이동 가능한지 확인하는 로직이 포함되어 있습니다.

```csharp
// Pathfinding.cs
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
```
