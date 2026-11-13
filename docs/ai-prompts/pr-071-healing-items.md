# PR #71: feat: 약초 채집과 약초·포션 사용 아이템

## 개요

- 브랜치: `feat/healing-items`
- 대상 브랜치: `dev`
- PR: #71
- 작성일: 2026-11-14
- 담당 영역: 게임플레이·제작
- 작업 목적: 부상수치(PR #70)를 줄이는 약초·포션을 만들고, 약초를 던전에서 채집할 수 있게 한다.

## 주요 요청

1. 약초는 던전 바닥에서 채집하고, 포션은 상점에서 산다. 포션이 약초보다 많이 회복한다.
2. 약초·포션은 낮에 여관에서 모험가에게 건네며, 파견 창에서는 쓸 수 없다. 여관 건네기는 여관·친밀도 작업에서 다룬다.
3. 약초는 재료가 아니라 먹이는 사용 아이템이다.

## AI와 논의한 내용

### 이번 범위

상점과 여관이 아직 코드에 없다. 포션은 아이템만 만들고 상점은 나중에, 건네기는 호스트 처리와 RPC까지 만들고 화면은 여관 작업에서 붙이기로 했다.

### 약초 자리

약초를 기존 자원 다음에 놓아, 약초 수를 바꾸거나 새로 넣어도 같은 시드의 광맥·나무 자리가 바뀌지 않게 했다.

## 주요 결정

- 사용자: 포션은 아이템만 만들고 상점은 나중에 한다. 건네기는 규칙과 RPC까지 만든다.
- 사용자: 사용 아이템은 한 칸에 쌓인다. 약초는 재료가 아닌 사용 아이템이다.
- 구현: 사용 아이템 종류 `Consumable`을 추가하고 회복량은 아이템 데이터에 둔다(약초 30, 포션 100).
- 구현: 건네는 RPC는 장비 착용 요청과 같은 `InventoryPlayerActions`에 둔다.
- 구현: 약초 4개, 하나에서 1~2개를 [추정]으로 `GatheringRules`에 둔다.

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Battle/GatheringLayout.cs`
- `Assets/02.Scripts/Inventory/ItemData.cs`
- `Assets/02.Scripts/Inventory/AdventurerTreatment.cs`
- `Assets/02.Scripts/Inventory/InventoryPlayerActions.cs`
- `Assets/03.ScriptableObjects/Items/Item_Herb.asset`, `Item_Potion.asset`
- `Assets/04.Prefabs/Town/Gathering/GatheringHerb.prefab`
- `docs/GAME_CONCEPT.md`

## 검증 내용

- EditMode 테스트 474개 통과.
- 플레이 모드(`DispatchTest`, 호스트)에서 `RequestTreat`로 약초·포션 회복과 건강한 모험가 거절을 확인했다.
- 채집 맵에서 실제로 뜯는 것과 두 명 접속은 확인하지 않았다.

## AI 활용 범위

AI가 범위 선택지를 제시했고, 채집·아이템·건네기 처리와 테스트를 구현했다. Unity CLI로 아이템·프리팹·씬을 연결했고, 플레이 모드 확인과 기획·진행 문서 갱신을 했다. 범위, 약초를 사용 아이템으로 하는 것, 사용 아이템 쌓기는 사용자가 결정했다.

## 후속 작업

- 여관에서 약초·포션 건네기 화면
- 상점의 포션 구매
- 채집 맵에서 약초 뜯기와 두 명 접속 확인
