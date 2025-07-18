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