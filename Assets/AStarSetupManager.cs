using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

[RequireComponent(typeof(Pathfinding))]
public class AStarSetupManager : MonoBehaviour
{
    [Header("UI Elements")] public TMP_InputField widthInputField;
    public TMP_InputField heightInputField;
    public Button generateMapButton;
    public Button setStartButton;
    public Button setEndButton;
    public Button setObstacleButton;
    public Button runAStarButton;
    public Button clearPathButton; // 선택 사항

    [Header("Tilemap & Tiles")] public Tilemap tilemap;
    public TileBase defaultTile;
    public TileBase startTile;
    public TileBase endTile;
    public TileBase obstacleTile;
    public TileBase pathTile; // 경로 표시용 타일 (선택 사항)

    [Header("Path Visualization")] public bool useLineRenderer = true; // 타일 또는 LineRenderer 선택 옵션
    public LineRenderer pathLineRenderer; // Inspector에서 할당
    public float lineHeight = 0.1f; // 라인이 타일 위에 그려지도록 높이 설정
    [Header("Pathfinding Options")] public Toggle allowDiagonalsToggle; // UI에 추가할 토글


    private Pathfinding pathfinding; // A* 알고리즘 스크립트 참조

    private enum PlacementMode
    {
        None,
        PlacingStart,
        PlacingEnd,
        PlacingObstacle
    }

    private PlacementMode currentMode = PlacementMode.None;

    private Vector3Int startPosition;
    private Vector3Int endPosition;
    private HashSet<Vector3Int> obstaclePositions = new HashSet<Vector3Int>();

    private bool startPointSet = false;
    private bool endPointSet = false;
    private List<Vector3Int> currentFoundPath = null;


    void Start()
    {
        pathfinding = GetComponent<Pathfinding>();
        if (pathfinding == null)
        {
            Debug.LogError("Pathfinding component not found on this GameObject!");
            enabled = false; // 스크립트 비활성화
            return;
        }

        // Pathfinding 스크립트에 타일맵 참조 직접 설정 (만약 Pathfinding 스크립트가 직접 참조를 받는다면)
        // pathfinding.tilemap = this.tilemap;
        if (allowDiagonalsToggle != null)
        {
            allowDiagonalsToggle.isOn = pathfinding.allowDiagonalMovement;
            allowDiagonalsToggle.onValueChanged.AddListener((value) =>
            {
                pathfinding.allowDiagonalMovement = value;
                Debug.Log($"대각선 이동 {(value ? "허용" : "금지")}");
            });
        }

        // 버튼 이벤트 리스너 할당
        generateMapButton.onClick.AddListener(GenerateMap);
        setStartButton.onClick.AddListener(() => SetMode(PlacementMode.PlacingStart));
        setEndButton.onClick.AddListener(() => SetMode(PlacementMode.PlacingEnd));
        setObstacleButton.onClick.AddListener(() => SetMode(PlacementMode.PlacingObstacle));
        runAStarButton.onClick.AddListener(RunAStar);
        if (clearPathButton != null)
            clearPathButton.onClick.AddListener(ClearDisplayedPath);

        // 초기값 (예시)
        if (widthInputField) widthInputField.text = "20";
        if (heightInputField) heightInputField.text = "10";

        if (pathLineRenderer != null)
        {
            pathLineRenderer.startWidth = 0.2f;
            pathLineRenderer.endWidth = 0.2f;
            pathLineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            pathLineRenderer.startColor = Color.blue;
            pathLineRenderer.endColor = Color.blue;
            pathLineRenderer.positionCount = 0;
            pathLineRenderer.sortingOrder = 10; // 정렬 순서 높게 설정
            pathLineRenderer.sortingLayerName = "Default"; // 적절한 정렬 레이어 설정
            pathLineRenderer.receiveShadows = false;
            pathLineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }

    void SetMode(PlacementMode mode)
    {
        currentMode = mode;
        Debug.Log("Current Mode: " + currentMode);
    }

    void GenerateMap()
    {
        if (!int.TryParse(widthInputField.text, out int width) || !int.TryParse(heightInputField.text, out int height))
        {
            Debug.LogError("Invalid width or height input.");
            return;
        }

        if (width <= 0 || height <= 0)
        {
            Debug.LogError("Width and Height must be positive integers.");
            return;
        }

        // 기존 타일맵 클리어
        tilemap.ClearAllTiles();
        obstaclePositions.Clear();
        startPointSet = false;
        endPointSet = false;
        ClearDisplayedPath(); // 이전 경로 지우기

        // 타일맵 채우기
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                tilemap.SetTile(new Vector3Int(x, y, 0), defaultTile);
            }
        }

        Debug.Log($"Map generated with Width: {width}, Height: {height}");
        currentMode = PlacementMode.None; // 생성 후 모드 초기화
        AdjustCamera();
    }

    void Update()
    {
        if (currentMode == PlacementMode.None) return;
        if (!Input.GetMouseButtonDown(0)) return; // 왼쪽 마우스 클릭

        // UI 위에 마우스 포인터가 있는지 확인 (UI 클릭 방지)
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            return;
        }

        // 마우스 클릭 좌표 변환 개선
        Vector3 mousePos = Input.mousePosition;
        // z 값은 카메라에서 타일맵까지의 거리
        float distanceToTilemap = Vector3.Distance(
            new Vector3(0, 0, 0), // 타일맵 z 위치
            new Vector3(0, 0, Camera.main.transform.position.z) // 카메라 z 위치
        );
        // 수정된 ScreenToWorldPoint 호출
        Vector3 worldPos = Camera.main.ScreenToWorldPoint(
            new Vector3(mousePos.x, mousePos.y, distanceToTilemap)
        );
        worldPos.z = 0; // 타일맵 평면 z 좌표로 고정
        Vector3Int cellPos = tilemap.WorldToCell(worldPos);
        // Update() 함수에 추가
        if (Input.GetMouseButtonDown(0))
        {
            Vector3 clickedWorldPos = worldPos;
            Debug.DrawRay(clickedWorldPos, Vector3.up * 0.5f, Color.red, 3f);
            Debug.DrawRay(clickedWorldPos, Vector3.right * 0.5f, Color.red, 3f);
        }

        // 디버깅용 로그
        Debug.Log($"마우스: {mousePos}, 월드(수정됨): {worldPos}, 셀: {cellPos}, 셀 중심: {tilemap.GetCellCenterWorld(cellPos)}");

        Debug.DrawLine(worldPos, worldPos + Vector3.up * 0.1f, Color.red, 1.0f);
        Debug.Log($"마우스: {mousePos}, 월드: {worldPos}, 셀: {cellPos}, 셀 중심: {tilemap.GetCellCenterWorld(cellPos)}");

        // 디버그 출력 추가
        Debug.Log($"마우스 화면 좌표: {Input.mousePosition}, 월드 좌표: {worldPos}, 셀 좌표: {cellPos}");

        // 타일맵 범위 확인 (생성된 맵 범위 내인지)
        BoundsInt bounds = tilemap.cellBounds; // 생성 후 업데이트된 bounds 사용
        if (!bounds.Contains(cellPos) && bounds.size.x > 0 && bounds.size.y > 0) // 맵이 생성되었을 때만 범위 체크
        {
            Debug.LogWarning("Clicked outside of the generated map bounds.");
            return;
        }


        switch (currentMode)
        {
            case PlacementMode.PlacingStart:
                if (startPointSet) // 이미 시작점이 있다면 이전 타일 복원
                {
                    tilemap.SetTile(startPosition, defaultTile);
                }

                startPosition = cellPos;
                tilemap.SetTile(startPosition, startTile);
                startPointSet = true;
                Debug.Log("Start point set at: " + startPosition);
                Debug.Log(
                    $"타일맵 정보 - 위치: {tilemap.transform.position}, 회전: {tilemap.transform.rotation}, 스케일: {tilemap.transform.localScale}");
                Debug.Log($"카메라 정보 - 위치: {Camera.main.transform.position}, 회전: {Camera.main.transform.rotation}");
                break;

            case PlacementMode.PlacingEnd:
                if (endPointSet) // 이미 도착점이 있다면 이전 타일 복원
                {
                    tilemap.SetTile(endPosition, defaultTile);
                }

                endPosition = cellPos;
                tilemap.SetTile(endPosition, endTile);
                endPointSet = true;
                Debug.Log("End point set at: " + endPosition);
                break;

            case PlacementMode.PlacingObstacle:
                if (cellPos == startPosition || cellPos == endPosition) // 시작/도착점은 장애물로 못 만듦
                {
                    Debug.LogWarning("Cannot place obstacle on start or end point.");
                    return;
                }

                if (obstaclePositions.Contains(cellPos))
                {
                    obstaclePositions.Remove(cellPos);
                    tilemap.SetTile(cellPos, defaultTile); // 장애물 제거
                }
                else
                {
                    obstaclePositions.Add(cellPos);
                    tilemap.SetTile(cellPos, obstacleTile); // 장애물 설치
                }

                Debug.Log("Obstacle toggled at: " + cellPos);
                break;
        }
    }

    void RunAStar()
    {
        if (!startPointSet || !endPointSet)
        {
            Debug.LogError("Please set both start and end points before running A*.");
            return;
        }

        if (allowDiagonalsToggle != null)
        {
            pathfinding.allowDiagonalMovement = allowDiagonalsToggle.isOn;
        }

        if (pathfinding.tilemap == null) pathfinding.tilemap = this.tilemap; // 만약을 위해 한번 더 설정
        pathfinding.UpdateWalkableNodes(obstaclePositions, startPosition,
            endPosition); // Pathfinding 스크립트에 이런 함수가 있다고 가정

        ClearDisplayedPath(); // 이전 경로 지우기
        currentFoundPath = pathfinding.FindPath(startPosition, endPosition);

        if (currentFoundPath != null && currentFoundPath.Count > 0)
        {
            Debug.Log("Path found! Length: " + currentFoundPath.Count);
            DisplayPath(currentFoundPath);
        }
        else
        {
            Debug.LogWarning("Path not found.");
        }

        currentMode = PlacementMode.None; // 실행 후 모드 초기화
    }


    // 경로 표시 함수 수정
    void DisplayPath(List<Vector3Int> path)
    {
        if (useLineRenderer && pathLineRenderer != null)
        {
            // LineRenderer로 경로 표시
            DisplayPathWithLineRenderer(path);
        }
        else if (pathTile != null)
        {
            // 기존 타일 방식으로 경로 표시
            foreach (Vector3Int cellPos in path)
            {
                if (cellPos != startPosition && cellPos != endPosition)
                {
                    tilemap.SetTile(cellPos, pathTile);
                }
            }
        }
    }

    // LineRenderer를 사용한 경로 표시 함수
    void DisplayPathWithLineRenderer(List<Vector3Int> path)
    {
        if (path == null || path.Count == 0 || pathLineRenderer == null)
        {
            if (pathLineRenderer != null) pathLineRenderer.positionCount = 0;
            return;
        }

        // 라인 렌더러 기본 설정 확인
        pathLineRenderer.sortingOrder = 10; // 다른 요소보다 앞에 보이도록 정렬 순서 설정

        List<Vector3> pathPoints = new List<Vector3>();

        // 시작 타일 중앙점 추가
        Vector3 startCenterPos = tilemap.GetCellCenterWorld(startPosition);
        // 카메라에서 보이는 z 위치로 조정 (앞쪽으로)
        startCenterPos.z = -0.1f; // 카메라 near 클리핑 평면보다 앞쪽 값으로 설정
        pathPoints.Add(startCenterPos);

        // 중간 경로 포인트 추가
        foreach (Vector3Int cellPos in path)
        {
            if (cellPos.Equals(startPosition) || cellPos.Equals(endPosition))
                continue;

            Vector3 centerPos = tilemap.GetCellCenterWorld(cellPos);
            centerPos.z = -0.1f;
            pathPoints.Add(centerPos);
        }

        // 도착 타일 중앙점 추가
        Vector3 endCenterPos = tilemap.GetCellCenterWorld(endPosition);
        endCenterPos.z = -0.1f;
        pathPoints.Add(endCenterPos);

        // LineRenderer 설정
        pathLineRenderer.positionCount = pathPoints.Count;
        for (int i = 0; i < pathPoints.Count; i++)
        {
            pathLineRenderer.SetPosition(i, pathPoints[i]);
        }

        // 디버그 로그 추가
        Debug.Log(
            $"라인 렌더러 포인트 개수: {pathPoints.Count}, 첫 포인트: {pathPoints[0]}, 마지막 포인트: {pathPoints[pathPoints.Count - 1]}");
    }

    // 경로 지우기 함수 수정
    void ClearDisplayedPath()
    {
        if (useLineRenderer && pathLineRenderer != null)
        {
            // LineRenderer 초기화
            pathLineRenderer.positionCount = 0;
        }
        else if (currentFoundPath != null && pathTile != null)
        {
            // 기존 방식으로 타일 복원
            foreach (Vector3Int cellPos in currentFoundPath)
            {
                if (cellPos != startPosition && cellPos != endPosition && !obstaclePositions.Contains(cellPos))
                {
                    tilemap.SetTile(cellPos, defaultTile);
                }
            }
        }

        currentFoundPath = null;
    }

    // 카메라 위치를 타일맵 중앙에 맞추기
    void AdjustCamera()
    {
        // 타일맵 크기 가져오기
        BoundsInt bounds = tilemap.cellBounds;

        // 카메라 위치를 타일맵 중앙으로 조정
        Camera.main.transform.position = new Vector3(
            bounds.center.x,
            bounds.center.y,
            -10 // 카메라 z 위치 유지
        );

        // 타일맵 전체가 보이도록 카메라 크기 조정 (직교 카메라 가정)
        if (Camera.main.orthographic)
        {
            float padding = 0.5f;
            Camera.main.orthographicSize = Mathf.Max(bounds.size.y / 2f + padding,
                bounds.size.x / 2f / Camera.main.aspect + padding);
        }

        Debug.Log($"카메라 위치 조정됨: {Camera.main.transform.position}, 크기: {Camera.main.orthographicSize}");
    }

    // 디버깅을 위한 시각화 코드
    void OnDrawGizmos()
    {
        if (tilemap == null) return;

        // 마우스 위치에 Gizmo 표시
        if (currentMode != PlacementMode.None)
        {
            Vector3 mousePos = Input.mousePosition;
            float distanceToTilemap = Vector3.Distance(
                new Vector3(0, 0, 0),
                new Vector3(0, 0, Camera.main.transform.position.z)
            );
            Vector3 worldPos = Camera.main.ScreenToWorldPoint(
                new Vector3(mousePos.x, mousePos.y, distanceToTilemap)
            );
            worldPos.z = 0;

            Gizmos.color = Color.yellow;
            Gizmos.DrawSphere(worldPos, 0.1f);

            Vector3Int cellPos = tilemap.WorldToCell(worldPos);
            Vector3 cellCenter = tilemap.GetCellCenterWorld(cellPos);

            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(cellCenter, Vector3.one);
        }
    }
}