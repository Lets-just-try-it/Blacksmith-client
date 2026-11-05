# PR #32: feat: 진열대 판매와 배회하는 손님, 공용 골드, 정제 붓기 눈금 화면 추가

## 개요

- 브랜치: `feat/forge-sales`
- 대상 브랜치: `dev`
- PR: #32
- 작성일: 2026-11-05
- 담당 영역: 게임플레이·제작(대장간), 시스템·데이터(공용 골드)
- 작업 목적: 제작한 장비를 진열대에서 팔아 골드를 벌게 하고, 정제 붓기 단계의 진행을 눈금으로 보여 줌

## 주요 요청

1. 장비를 진열대에 올려 파는 흐름과 공용 골드를 만들 것
2. 손님이 진열 여부와 상관없이 판매 구역을 돌아다니고, 장비가 있는 진열대 앞에서 일정 확률로만 살 것
3. 정제 붓기 단계에 틀에 찬 양을 보여 주는 눈금 화면과 정확도 미리보기를 둘 것
4. 최신 dev를 받아 충돌을 정리하되, 인벤토리 담당자의 작업을 우선할 것
5. 골드 표시가 시계와 겹치지 않게 다시 배치하고, 플레이 전에 켜져 있는 UI를 숨길 것

## AI와 논의한 내용

### 손님이 오가는 방식

처음에는 진열대가 있어야 손님이 왔다. 사용자가 손님이 판매 구역을 돌아다니다 확률로 사 가는 쪽을 원해서 바꿨다.
- 들를 곳(진열대 앞과 빈 자리 4곳)을 3~5곳 무작위로 정해 차례로 돈다. 같은 곳을 연달아 들르지 않는다.
- 장비가 있는 진열대 앞에서만 확률(기본 50%)로 산다. 사면 입구로 나가고, 안 사면 다음 곳으로 간다.
- 규칙(`PlanRoute`, `WillBuy`)은 시드 기반 순수 C#이고, 수치는 `SalesRules.asset`에 둔다.

### 정제 붓기 화면

붓기 단계에서 얼마나 채웠는지 알기 어려워 눈금 막대를 더했다. "지금 끝내면 정제 N%" 미리보기는 마무리 계산과 같은 함수를 써서 화면과 결과가 어긋나지 않게 했다.

### dev의 낮·밤 사이클과 겹침

최신 dev에 낮·밤 사이클(#30)이 들어와 우리 하루 흐름 구현(`DayCycleState` 등)과 같은 기능이 두 벌이 되었다. AI는 충돌 내용과 차이를 비교해 보여 주었고, 사용자는 dev 것으로 통일하기로 정했다.
- 버린 것: `DayCycleState`, `DayChannel`, `DayCycleRulesData`와 관련 에셋
- 남긴 것: 골드(dev에 없음). `ShopFlow`는 dev의 `GameState.Phase`를 보도록 바꿨다.
- 씬은 글자 단위로 합칠 수 없어 dev 씬을 기준으로 두고, 우리 오브젝트(`ShopFlow`, 골드 표시, 배회 지점)는 유니티 스크립트로 다시 연결했다.

### 인벤토리 PR 이후 합치기

인벤토리 담당자의 #25~#28을 받으며 충돌한 두 파일(`Player.prefab`, `ForgeTest.unity`)은 인벤토리 쪽 변경을 그대로 두고 우리 컴포넌트만 얹었다.

### UI 배치와 시작 상태

- 골드 텍스트가 시계(`DayCycleHud`)와 겹쳐 시계 바로 아래로 옮겼다.
- 플레이 전에 게임창에 켜져 있던 미니게임·공용 창고 등의 패널을 꺼진 상태로 저장했다. 각 화면 스크립트가 시작할 때 스스로 끄므로 실제 시작 상태와 같다. `Flash`는 코드가 색만 바꾸므로 켜 둔다.

## 주요 결정

- 손님은 진열 여부와 상관없이 오고, 진열대 앞에서만 확률로 산다. [추정] 진열대마다 장비 한 칸이다.
- 하루 흐름(낮·마감·밤)은 dev의 `GameState`·`DayCycleRules`로 통일한다.
- 충돌 정리는 인벤토리 담당자의 변경을 우선한다.
- 골드는 `GameState`에 둔다. 호스트만 바꾸고, 화면에는 시계 아래에 보인다.
- `docs/GAME_CONCEPT.md`에 진열대 판매 [추정], 손님 배회와 구매 확률, 골드 표시 위치, 붓기 눈금 화면을 추가했다(기획 변경, 팀 공유 필요).

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Sales/SalesRules.cs`, `Assets/02.Scripts/Forge/SalesRulesData.cs`
- `Assets/02.Scripts/Forge/ShopFlow.cs`, `Customer.cs`, `DisplayShelfState.cs`, `DisplayShelfStation.cs`, `SalesPlayerActions.cs`, `ShopHud.cs`
- `Assets/02.Scripts/Network/GameState.cs`
- `Assets/02.Scripts/Core/Forge/SmeltingGame.cs`, `Assets/02.Scripts/Forge/SmeltingView.cs`
- `Assets/04.Prefabs/Town/ForgeRoom.prefab`, `Characters/Customer.prefab`, `Network/Player.prefab`
- `Assets/01.Scenes/ForgeTest.unity`
- `Assets/02.Scripts/Tests/EditMode/SalesRulesTests.cs`, `SmeltingGameTests.cs`
- `docs/GAME_CONCEPT.md`, `docs/NETWORK_DESIGN.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- EditMode 테스트 263개 통과(판매 규칙 9개, 정제 미리보기 2개 추가).
- `ForgeTest` 플레이 모드 진입, 콘솔 오류 없음.
- dev를 합치기 전에 사용자가 플레이로 판매·배회 손님·붓기 화면이 동작하는 것을 확인했다.
- 프리팹과 씬: 깨진 컴포넌트와 스크립트 없음.
- 확인하지 못한 것: dev와 합친 뒤 낮 → 마감 → 밤 → 다음 날 동안의 손님 동작, 골드 표시가 시계와 겹치지 않는 것, 실제 두 명 접속.

## AI 활용 범위

AI가 한 일:
- 판매·손님·골드·붓기 화면 구현과 테스트 작성
- 충돌 분석과 하루 흐름 통일 작업, 씬·프리팹 재구성(Unity CLI)
- UI 위치 조정과 시작 패널 정리
- 문서 갱신, 커밋 분리, PR 초안 작성

사용자가 정한 일:
- 손님의 배회·확률 구매 방식
- dev의 낮·밤 사이클로 통일하고 인벤토리 담당자 변경을 우선하는 것
- 판매 작업의 PR 범위, 커밋·푸시·PR 승인
- 플레이로 동작 확인

## 후속 작업

- 합친 뒤 하루 흐름 전체와 두 명 접속에서 진열·판매 확인
- 판 성형·조립 등 나머지 제작 미니게임과 방어구 제작
- 판매 가격과 손님 수 등 수치 조정
