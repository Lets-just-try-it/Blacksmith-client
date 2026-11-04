# PR #29: feat: 파견 창과 전투 씬 Additive 불러오기, 장비를 입은 모험가로 전투

## 개요

- 브랜치: `feat/dispatch-window`
- 대상 브랜치: `dev`
- PR: #29
- 작성일: 2026-11-04
- 담당 영역: 전투
- 작업 목적: 파견 창에서 모험가에게 입힌 장비를 기반으로 자동 전투 유닛을 초기화

## 주요 요청

1. 파견 창에서 모험가에게 입힌 장비를 기반으로 자동 전투 유닛을 초기화할 것
2. 작업 전에 최신 dev 브랜치 내용을 읽을 것
3. 전투 씬을 Additive로 불러오는 것까지 포함할 것
4. 결과창이 뜨면 파견 창으로 돌아가지 않고 바로 낮 씬으로 돌아갈 것
5. 장비 착용 창에서 보이는 장비 모습이 전투에서도 보일 것

## AI와 논의한 내용

### 파견 창이 없는 상태

AI가 dev를 확인해 보니 장비 보정은 이미 전투 유닛 만들기에 들어가 있었다. 하지만 파견 창(모험가 선택 창)은 없었고, 전투는 장비 없는 테스트 파티를 쓰고 있었다. AI가 이를 설명하자, 사용자는 장비 창이 아니라 모험가 선택 창이 없다는 뜻인지 확인한 뒤 파견 창을 만들기로 했다.

### 작업 범위

AI는 두 가지 선택지를 제시했다.
- 한 씬에 파견 창과 전투를 함께 두는 테스트 씬
- 기획대로 Additive 불러오기

사용자는 Additive 불러오기를 골랐다. AI가 범위가 크고 낮 씬 화면 끄기도 임시로 넣어야 한다고 하자, 사용자는 Additive로 띄우면 화면 끄기는 당연히 해야 하는 일이라고 짚었다. 그래서 문서상 다른 담당이던 화면 끄기를 이번에 함께 구현했다.

### 던전 진행 상황 위치

전투 씬을 내리면 `BattleState`의 진행 상황이 사라진다. 파견 창도 스테이지를 고를 때 이 값이 필요하다. AI는 공용 `GameState`를 바꾸지 않고, 전투 영역의 낮 씬 오브젝트(`DungeonProgressState`)로 옮기는 안을 제시했다.

### 기본 규칙

AI가 정해지지 않은 항목의 기본안을 제시했고, 사용자는 1·2안을 확정하고 3안은 다음 작업으로 미뤘다.
1. 참가자 화면: 호스트의 선택을 그대로 보여 주고, 참가자는 보기만 한다 (기획 13절 11번)
2. 파견 뒤 장비: 모험가가 계속 입고 있는다
3. 결과 반영: 관리 목록·골드에 반영하는 일은 다음 작업

### 전투에 보이는 장비 모습

장비 상자가 장비 창 미리보기 무대에만 있어서 전투 모델에는 보이지 않았다. AI는 상자를 전투 모델 프리팹으로 옮기고, 미리보기와 전투가 같은 컴포넌트로 표시하게 바꿨다.

## 주요 결정

- 파견 창은 전투 영역(`Battle/`)에 두고, 광장의 관리 목록은 읽기만 한다.
- 파티는 최대 5명이고 부상·사망 모험가는 고를 수 없다.
- 파견 창의 능력치는 전투 유닛과 같은 계산(`AdventurerBattleData.ToSetup`)을 쓴다.
- 파견 창은 모든 참가자에게 열리고 고르기는 호스트만 한다. 기획 13절 11번을 확정했다.
- 파견 창에서는 열린 단계 중 보스까지 클리어하지 않은 단계만 고를 수 있고, 처음에는 보스 스테이지까지 골라 둔다.
- 두 씬은 Runtime 에셋 `BattleChannel`로 잇는다.
- 전투 씬은 Additive로 불러오고, 결과 확인 뒤 내려 바로 낮 씬으로 돌아간다.
- 전투 중에는 낮 씬 카메라·UI·라이트를 끄고 조작을 잠근다. 낮·밤 사이클이 생기면 합칠 임시 구현이다.
- 던전 진행 상황은 낮 씬의 `DungeonProgressState`에 둔다.
- `BattleTest`는 테스트 파티로 `Battle` 씬을 불러오는 테스트 전용 씬으로 정리하고, `DispatchTest` 씬을 추가한다.
- 장비 모습은 전투 모델의 `AdventurerEquipmentLook`이 표시하고, 장비 창 미리보기와 전투가 같이 쓴다.
- 파견 뒤 장비는 모험가가 계속 입는다. 전투 결과를 관리 목록·골드에 반영하는 일은 다음 작업으로 둔다.

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Battle/DispatchRules.cs`, `Assets/02.Scripts/Tests/EditMode/DispatchRulesTests.cs`
- `Assets/02.Scripts/Battle/DispatchState.cs`, `DispatchView.cs`, `DispatchAdventurerRow.cs`
- `Assets/02.Scripts/Battle/BattleChannel.cs`, `BattleSceneLoader.cs`, `BattleSceneFlow.cs`, `DaySceneBattleCover.cs`, `DungeonProgressState.cs`, `AdventurerEquipmentLook.cs`
- `Assets/02.Scripts/Battle/BattleState.cs`, `BattleTestStarter.cs`
- `Assets/02.Scripts/Inventory/AdventurerEquipView.cs`, `AdventurerPreview.cs`
- `Assets/01.Scenes/Battle.unity`, `BattleTest.unity`, `DispatchTest.unity`, `InventoryTest.unity`
- `Assets/04.Prefabs/UI/Battle/DispatchAdventurerRow.prefab`, `Assets/04.Prefabs/Characters/BattleTest/Adventurer_Placeholder.prefab`
- `Assets/03.ScriptableObjects/Runtime/BattleChannel.asset`, `ProjectSettings/EditorBuildSettings.asset`
- `docs/GAME_CONCEPT.md`, `docs/NETWORK_DESIGN.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- EditMode 테스트 242개 통과. 파견 규칙 테스트 7개를 추가했다.
- `DispatchTest` 플레이 모드에서 다음을 확인했다. 콘솔 오류 없음.
  - 파견 창 표시와 파티 선택
  - 장비를 입히면 파견 창과 전투 유닛 능력치가 같게 바뀌는 것 (체력 476→540, 공격력 34→49, 방어력 7→16)
  - 장비 창 미리보기와 전투 유닛에 같은 장비 상자가 보이는 것
  - Additive로 불러오기, 낮 화면 끄기와 조작 잠금
  - 결과 확인 뒤 진행 상황 반영, 씬 내리기, 낮 화면 복귀
- `BattleTest`에서 전투, 결과 반영, 씬 내리기, 다시 파견이 반복되는 것을 확인했다.
- 화면 캡처로 파견 창, 장비 창 미리보기, 전투 장면을 확인했다.
- 작업 중 확인하고 고친 것
  - 새로 만든 채널 에셋의 참조가 씬에 저장되지 않아 Unity CLI로 다시 지정했다.
  - 장비 창을 연 채 세션을 끝내면 나던 관리 목록 접근 오류를 고쳤다.
- 버튼은 화면 함수를 직접 불러 확인했다. 실제 마우스와 두 명 접속은 확인하지 못했다.

## AI 활용 범위

AI가 한 일:
- dev 최신 내용과 전투·광장·인벤토리 코드 조사
- 구조 제안, 코드·테스트 작성
- Unity CLI로 씬·프리팹·에셋 구성
- 플레이 모드 확인과 화면 캡처
- 문서 갱신, 커밋·PR 초안 작성

사용자가 정한 일:
- 파견 창을 만들기로 한 것과 작업 범위 (Additive 불러오기까지)
- 낮 씬 화면 끄기를 함께 구현할 것
- 결과창 뒤 바로 낮 씬으로 돌아가는 흐름
- 참가자 화면과 파견 뒤 장비 규칙 확정, 결과 반영은 다음 작업으로 미룸
- 전투에서도 장비 모습이 보이게 할 것
- 커밋·푸시·PR 승인

## 후속 작업

- 전투 결과를 관리 목록(경험치·부상·사망)과 골드·재료에 반영
- 실제 마우스와 두 명 접속 확인
- 낮·밤 사이클에서 밤에 파견 창 열기, 낮 화면 끄기를 화면 전환과 합치기
- `Game` 씬에 파견 창과 관련 오브젝트 배치
- 파견 뒤 장비 반환 규칙, 실제 장비 모델
- 기획 변경 팀 공유
