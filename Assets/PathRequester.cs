using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections.Generic;

public class PathRequester : MonoBehaviour
{
    public Pathfinding pathfinding; // Pathfinding 스크립트 참조
    public Tilemap tilemap;         // 좌표 변환 등을 위한 타일맵 참조

    public Transform startTransform; // 시작 위치를 나타내는 오브젝트
    public Transform targetTransform; // 목표 위치를 나타내는 오브젝트

    private List<Vector3Int> currentPath;

    void Update()
    {
        // 예시: 마우스 클릭으로 목표 지점 설정 또는 특정 키 입력 시 경로 요청
        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (startTransform != null && targetTransform != null && pathfinding != null && tilemap != null)
            {
                Vector3Int startCellPos = tilemap.WorldToCell(startTransform.position);
                Vector3Int targetCellPos = tilemap.WorldToCell(targetTransform.position);

                Debug.Log($"Requesting path from {startCellPos} to {targetCellPos}");
                currentPath = pathfinding.FindPath(startCellPos, targetCellPos);

                if (currentPath != null && currentPath.Count > 0)
                {
                    Debug.Log("Path found! Nodes: " + currentPath.Count);
                    // 경로를 따라 이동하는 로직 또는 경로 시각화 로직 호출
                    // 예를 들어, 캐릭터 이동 스크립트에 경로를 전달
                    // MoveAlongPath(currentPath);
                }
                else
                {
                    Debug.LogWarning("Path not found.");
                }
            }
            else
            {
                Debug.LogError("Required components (Pathfinding, Tilemap, Start/Target Transform) are not assigned.");
            }
        }
    }

    // 경로 시각화를 위한 Gizmos (Scene 뷰에서만 보임)
    void OnDrawGizmos()
    {
        if (currentPath != null && tilemap != null)
        {
            Gizmos.color = Color.green;
            foreach (Vector3Int cellPos in currentPath)
            {
                // 셀의 월드 좌표 중심을 가져옴
                Vector3 worldPos = tilemap.GetCellCenterWorld(cellPos);
                Gizmos.DrawCube(worldPos, Vector3.one * 0.5f); // 셀 크기에 맞게 조절
            }
        }
    }

    // (선택 사항) 경로를 따라 이동하는 함수 예시
    /*
    void MoveAlongPath(List<Vector3Int> path)
    {
        // 이 부분은 캐릭터 이동 로직에 따라 매우 다양하게 구현될 수 있습니다.
        // Coroutine을 사용하여 순차적으로 이동하거나,
        // NavMeshAgent와 유사한 방식으로 다음 지점을 계속 업데이트할 수 있습니다.
        // 예: StartCoroutine(FollowPathCoroutine(path));
    }

    System.Collections.IEnumerator FollowPathCoroutine(List<Vector3Int> path)
    {
        foreach(Vector3Int cellPos in path)
        {
            Vector3 targetWorldPos = tilemap.GetCellCenterWorld(cellPos);
            // startTransform을 targetWorldPos로 이동시키는 로직 (예: Lerp, MoveTowards)
            while(Vector3.Distance(startTransform.position, targetWorldPos) > 0.1f)
            {
                startTransform.position = Vector3.MoveTowards(startTransform.position, targetWorldPos, Time.deltaTime * 5f); // 5f는 속도
                yield return null;
            }
        }
        Debug.Log("Path completed.");
    }
    */
}