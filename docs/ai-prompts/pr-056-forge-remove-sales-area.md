# PR #56: refactor: 기획에서 빠진 판매 구역과 판매 코드 제거

## 개요

- 브랜치: `refactor/forge-remove-sales-area`
- 대상 브랜치: `dev`
- PR: #56
- 작성일: 2026-11-12
- 담당 영역: 게임플레이·제작
- 작업 목적: 기획에서 빠진 판매 구역과 판매 코드를 제거하고, 입구와 스폰 지점을 제작 구역 안으로 옮긴다.

## 주요 요청

1. 대장간 남은 작업(조립 작업대 이식, 맵 확장)을 작은 PR들로 나눠 진행한다.
2. 기획에서 빠진 판매 구역과 판매 코드를 제거한다.

## AI와 논의한 내용

### PR 나누는 순서

- 조립 규칙(`AssembleGame`, `AssembleRules`와 테스트) → 판매 코드 정리(이 PR) → 맵과 칸 통일 → 조립 입력·화면 순서로 나누기로 했다.
- 맵을 넓히기 전에 불필요한 판매·관리 구역을 먼저 지워 맵 변경의 범위를 줄인다.

### 판매 기준가

- `TierRules`의 판매 기준가는 의뢰·업그레이드에서 쓸 수 있어 남겼다.

## 주요 결정

- 판매 구역, 진열대, 손님, 판매 규칙·데이터, Customer 프리팹을 대체 기능 없이 제거한다.
- `ForgeRoom` 프리팹에서 판매·관리 구역과 중간 벽·문을 지우고, 입구와 스폰 지점을 제작 구역 안으로 옮긴다.
- `Game`·`ForgeTest` 씬의 Fusion 씬 오브젝트는 커밋 전에 다시 저장한다.
- 기획 변경은 없다. `docs/GAME_CONCEPT.md`는 #53에서 이미 반영되어 있고, 진행 문서와 네트워크 설계 문서만 정리한다.

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Sales/SalesRules.cs`, `Assets/02.Scripts/Forge/DisplayShelf*.cs`, `Customer.cs`, `ShopFlow.cs`, `ShopHud.cs`, `SalesPlayerActions.cs`, `SalesRulesData.cs`, `SalesRulesTests.cs`, `SalesRules.asset`(삭제)
- `Assets/04.Prefabs/Characters/Customer.prefab`(삭제), `Assets/04.Prefabs/Network/Player.prefab`, `Assets/04.Prefabs/Town/ForgeRoom.prefab`
- `Assets/01.Scenes/ForgeTest.unity`, `Assets/01.Scenes/Game.unity`
- `docs/PROJECT_PROGRESS.md`, `docs/NETWORK_DESIGN.md`

## 검증 내용

- EditMode 테스트 354개가 통과했다(판매 테스트 9개를 지워 363개에서 줄었다).
- `ForgeTest` 씬 플레이에서 스폰 위치와 제작 구역 화면을 확인했다.
- `Game` 씬의 스폰 위치 이동은 실제 플레이로 확인하지 못했다.

## AI 활용 범위

AI가 판매 코드·데이터 삭제, 프리팹과 씬 수정, Fusion 씬 재저장, 테스트 실행, 문서 정리를 수행했다. 제거 범위와 PR을 나누는 순서는 사용자가 정했다.

## 후속 작업

- 맵을 넓히고 작업대를 같은 크기 칸으로 통일하는 PR
- 조립 작업대의 입력과 화면 PR
