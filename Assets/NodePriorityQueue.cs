using System.Collections.Generic;

/// <summary>
/// A* 경로탐색 알고리즘을 위한 최소 힙(Min Heap) 기반 우선순위 큐
/// - 시간 복잡도: 삽입/제거/업데이트 모두 O(log n)
/// - 노드 비교: FCost(우선) 및 hCost(동일 FCost 시) 기준으로 정렬
/// - 일반적인 리스트 정렬 방식(O(n log n))보다 효율적인 구현
/// </summary>
public class NodePriorityQueue
{
    // 바이너리 힙으로 구현된 우선순위 큐의 내부 저장소
    private List<Pathfinding.Node> heap = new List<Pathfinding.Node>();
    // 노드의 인덱스를 빠르게 조회하기 위한 딕셔너리 (O(1) 접근 시간)
    private Dictionary<Pathfinding.Node, int> nodeIndices = new Dictionary<Pathfinding.Node, int>();

    /// <summary>
    /// 우선순위 큐에 있는 노드의 수
    /// </summary>
    public int Count => heap.Count;
    
    /// <summary>
    /// 특정 노드가 우선순위 큐에 포함되어 있는지 확인
    /// </summary>
    public bool Contains(Pathfinding.Node node) => nodeIndices.ContainsKey(node);

    /// <summary>
    /// 우선순위 큐에 노드 추가 - O(log n)
    /// 노드를 힙의 마지막에 추가하고 올바른 위치로 상향 이동
    /// </summary>
    public void Add(Pathfinding.Node node)
    {
        heap.Add(node);
        nodeIndices[node] = heap.Count - 1;
        SortUp(heap.Count - 1); // 힙 속성 유지를 위해 상향 이동
    }

    /// <summary>
    /// 우선순위가 가장 높은(FCost가 가장 낮은) 노드 제거 및 반환 - O(log n)
    /// 루트 노드 제거 후 마지막 노드를 루트로 이동시키고 하향 정렬
    /// </summary>
    public Pathfinding.Node RemoveFirst()
    {
        Pathfinding.Node firstNode = heap[0];

        // 마지막 노드를 첫 위치로 이동 (루트로)
        int lastIdx = heap.Count - 1;
        heap[0] = heap[lastIdx];
        nodeIndices[heap[0]] = 0;

        // 첫 노드 제거
        heap.RemoveAt(lastIdx);
        nodeIndices.Remove(firstNode);

        if (heap.Count > 0)
            SortDown(0); // 힙 속성 유지를 위해 하향 이동

        return firstNode;
    }

    /// <summary>
    /// 노드 우선순위(FCost, hCost)가 변경되었을 때 힙 위치 갱신 - O(log n)
    /// A* 알고리즘에서 더 좋은 경로를 찾았을 때 호출됨
    /// </summary>
    public void UpdateItem(Pathfinding.Node node)
    {
        if (nodeIndices.TryGetValue(node, out int index))
            SortUp(index); // 비용이 감소했으므로 상향 이동만 필요
    }

    /// <summary>
    /// 노드를 힙 내에서 상향 이동 (더 작은 FCost를 가진 노드를 위로) - O(log n)
    /// 부모 노드보다 우선순위가 높으면 교환하는 과정을 반복
    /// </summary>
    private void SortUp(int childIdx)
    {
        while (childIdx > 0)
        {
            int parentIdx = (childIdx - 1) / 2; // 부모 노드 인덱스 계산
            if (Compare(heap[childIdx], heap[parentIdx]) >= 0)
                break; // 부모가 더 작거나 같으면 정렬 완료

            Swap(childIdx, parentIdx); // 부모와 자식 위치 교환
            childIdx = parentIdx; // 다음 비교를 위해 부모로 이동
        }
    }

    /// <summary>
    /// 노드를 힙 내에서 하향 이동 (더 큰 FCost를 가진 노드를 아래로) - O(log n)
    /// 자식 노드 중 더 작은 값과 교환하는 과정을 반복
    /// </summary>
    private void SortDown(int parentIdx)
    {
        while (true)
        {
            int leftChildIdx = parentIdx * 2 + 1;
            int rightChildIdx = parentIdx * 2 + 2;
            int swapIdx = parentIdx;

            // 왼쪽 자식이 있고 부모보다 작으면 교환 대상으로 선택
            if (leftChildIdx < heap.Count && Compare(heap[leftChildIdx], heap[swapIdx]) < 0)
                swapIdx = leftChildIdx;

            // 오른쪽 자식이 있고 현재 교환 대상보다 작으면 오른쪽을 교환 대상으로 선택
            if (rightChildIdx < heap.Count && Compare(heap[rightChildIdx], heap[swapIdx]) < 0)
                swapIdx = rightChildIdx;

            // 교환할 필요가 없으면 종료
            if (swapIdx == parentIdx)
                break;

            Swap(parentIdx, swapIdx);
            parentIdx = swapIdx; // 다음 비교를 위해 이동
        }
    }

    /// <summary>
    /// 두 노드의 우선순위 비교
    /// 1. FCost 비교 (낮을수록 우선)
    /// 2. FCost가 같으면 hCost 비교 (낮을수록 우선, 목표에 더 가까운 노드 선호)
    /// </summary>
    private int Compare(Pathfinding.Node a, Pathfinding.Node b)
    {
        int compare = a.FCost.CompareTo(b.FCost);
        if (compare == 0)
            compare = a.hCost.CompareTo(b.hCost);
        return compare;
    }

    /// <summary>
    /// 힙 내의 두 노드 위치 교환 및 인덱스 딕셔너리 업데이트
    /// </summary>
    private void Swap(int idxA, int idxB)
    {
        Pathfinding.Node nodeA = heap[idxA];
        Pathfinding.Node nodeB = heap[idxB];

        heap[idxA] = nodeB;
        heap[idxB] = nodeA;

        nodeIndices[nodeA] = idxB;
        nodeIndices[nodeB] = idxA;
    }
}