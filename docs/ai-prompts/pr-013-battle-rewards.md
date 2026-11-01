# PR #13: feat: 전투 보상·경험치 규칙과 레벨업 추가

## 개요

- 브랜치: `feat/battle-rewards`
- 대상 브랜치: `dev`
- PR: #13
- 작성일: 2026-11-02
- 담당 영역: 전투 (`Battle/`, `Core/Battle/`), 일부 `Core/Adventurers/`, `Plaza/`
- 작업 목적: 전투가 끝난 뒤 보상과 모험가별 경험치·레벨업을 계산해 결과창에 보여 준다.

## 주요 요청

1. 처음 제시한 작업 순서대로 전투 결과 처리(보상·경험치)를 진행할 것
2. 임시 한글 폰트를 써서 결과창이 뜨는 것까지 확인할 것
3. 폰트 적용은 따로 커밋할 것

## AI와 논의한 내용

### 보상과 경험치 규칙

AI가 기획의 [담당자 결정]·[미정] 항목에 대해 선택지를 제시했고 사용자가 정했다.

- 보상 종류: 골드만 / 골드 + 재료 중 골드 + 재료
- 패배 시: 클리어한 만큼 지급 / 승리해야만 지급 중 클리어한 만큼
- 경험치 대상: 사망자 제외 전원 / 끝까지 살아남은 모험가만 / 쓰러진 시점까지 중 쓰러진 시점까지
- 레벨업과 저장: 공식 + 필드 추가 / 레벨별 표 / 저장 없이 계산만 중 공식 + 필드 추가

"쓰러진 시점까지"를 위해 AI는 시뮬레이션이 스테이지를 클리어할 때 쓰러지지 않은 모험가의 클리어 수를 세게 했다. 무작위 값을 쓰지 않아 기존 시드의 결과는 바뀌지 않는다.

### 테스트 시나리오 확인

AI가 처음 고른 테스트 시드에서는 두 모험가가 같은 시점에 쓰러져, 쓰러진 시점에 따라 경험치가 달라지는지 확인되지 않았다. 여러 시드를 돌려 한 모험가만 중간에 쓰러지는 조합으로 바꾸고, 두 값이 다른지도 검사하게 했다.

### 결과창 레이아웃과 폰트

화면 캡처에서 보상 줄이 행과 겹치고 행 글자가 두 줄로 넘치는 것을 발견했다. AI가 결과창 위치와 행 프리팹의 칸 너비, 글자 자동 크기를 조정했다. 사용자 요청으로 광장 UI가 쓰는 임시 폰트 주아를 결과창에 적용해 한글이 보이는 것을 확인했다.

## 주요 결정

- 결과 처리(보상·경험치)를 먼저 진행한다. (사용자 결정)
- 보상은 골드와 재료이고, 패배해도 클리어한 스테이지만큼 지급한다. (사용자 결정)
- 경험치는 쓰러지기 전까지 클리어한 스테이지만큼 받는다. (사용자 결정)
- 사망한 모험가는 경험치를 받지 않는다. (AI 제안)
- 레벨업 필요 경험치는 기본값 + 증가분 × (현재 레벨 - 1)이고 [추정]으로 둔다. `AdventurerData`·`NetworkAdventurer`에 경험치 필드를 추가한다. (사용자 결정, 광장 담당 동의 필요)
- 최대 레벨과 재료 종류는 [미정]으로 남긴다. (AI 제안)
- 골드·재료를 실제 게임 상태에 반영하는 것은 대장간 데이터가 생긴 뒤로 미룬다. (AI 제안, 사용자 승인)
- 결과창에 임시 폰트 주아를 적용하고, 폰트는 따로 커밋한다. (사용자 결정)

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Battle/BattleRewardRules.cs`, `BattleSimulation.cs`, `BattleResult.cs`, `BattleUnit.cs`
- `Assets/02.Scripts/Core/Adventurers/AdventurerLevelRules.cs`, `AdventurerData.cs`
- `Assets/02.Scripts/Plaza/NetworkAdventurer.cs`
- `Assets/02.Scripts/Battle/AdventurerLevelData.cs`, `DungeonData.cs`, `BattleResultView.cs`, `BattleResultRow.cs`, `BattleTestStarter.cs`, `AdventurerUnitInfo.cs`
- `Assets/02.Scripts/Tests/EditMode/BattleRewardRulesTests.cs`, `BattleSimulationTests.cs`
- `Assets/03.ScriptableObjects/Balance/AdventurerLevel.asset`, `Assets/03.ScriptableObjects/Dungeons/Dungeon_Test.asset`
- `Assets/04.Prefabs/UI/Battle/BattleResultRow.prefab`, `Assets/01.Scenes/BattleTest.unity`
- `docs/GAME_CONCEPT.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- 에디터 컴파일 오류 없음, EditMode 테스트 68개 통과 (새 테스트 9개 포함)
- AI가 Unity CLI로 `BattleTest` 플레이 모드를 실행하고 화면을 캡처해 결과창의 보상, 경험치·레벨업, 한글 표시를 확인했다. 다시 전투해 오른 레벨과 남은 경험치가 이어지는 것도 확인했다. 콘솔 오류 없음.

## AI 활용 범위

AI는 선택지 정리, 코드·테스트 작성, Unity CLI를 통한 데이터·씬·프리팹 구성, 플레이 모드와 화면 검증, 문서 갱신, 커밋 분리를 맡았다. 보상 종류, 패배 처리, 경험치 대상, 레벨업 방식, 작업 순서, 폰트 적용과 커밋 분리는 사용자가 정했다.

## 후속 작업

- 골드·재료를 게임 상태에 반영
- 전투 결과(레벨·경험치, 부상·사망)를 관리 목록에 반영 (광장 담당 API)
- Fusion 연동
- 전투 HUD 등 나머지 UI에 한글 폰트 적용
