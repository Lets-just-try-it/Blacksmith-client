# PR #18: feat: MVP 던전 2단계 몬스터 데이터와 보스 패턴·체력바 추가

## 개요

- 브랜치: `feat/battle-monsters`
- 대상 브랜치: `dev`
- PR: #18
- 작성일: 2026-11-03
- 담당 영역: 전투 (`Battle/`, `Core/Battle/`)
- 작업 목적: MVP 던전 2단계의 몬스터와 보스 패턴을 데이터로 만들고, 몬스터 체력을 화면에 보여 준다.

## 주요 요청

1. 남은 작업의 진행 순서와 전투 영역의 남은 작업 목록을 정리할 것
2. 팀에서 정한 담당자(플레이어 캐릭터는 광장 담당, 낮·밤 사이클은 대장간 담당)를 다음 기능 PR 문서에 반영할 것
3. 몬스터 데이터부터 진행할 것. 정확한 수치는 추후 밸런싱한다
4. 잡몹은 프리팹 하단에 체력바를, 보스는 UI로 체력바를 표시할 것
5. 모든 바의 끝이 번지지 않도록 Fill 오른쪽 앵커 방식으로 바꿀 것

## AI와 논의한 내용

### 작업 순서

AI가 진행 문서와 기획 문서의 미정 항목을 바탕으로, 의존 관계에 따라 전투 작업 순서를 제시했다(몬스터 데이터 → 부상·사망 → Fusion 연동 → 파견 창 → 결과 반영 → 밤 흐름 → 보스 클리어 → 장비 보정). 사용자는 다른 영역과 엮이지 않는 몬스터 데이터부터 하기로 했다.

### 몬스터 구성과 보스 패턴

기존 구조는 몬스터마다 스킬이 하나뿐이라 보스가 잡몹과 다르지 않았다. AI가 선택지를 제시했고 사용자가 정했다.

- 몬스터 구성: 고블린·언데드 테마(단계마다 잡몹 3종 + 보스) / 단계마다 잡몹 2종 / 직접 지정 중 고블린·언데드 테마
- 보스 패턴: 스킬 순환 + 분노 / 스킬 순환만 / 지금 구조 유지 / 소환까지 중 스킬 순환 + 분노
- 외형: 임시 모델에 색만 구분 / 기존 Placeholder 공용 중 색만 구분

### 수치 확인

AI가 에디터에서 파티 인원과 레벨을 바꿔 가며 두 던전의 시뮬레이션을 돌려 단계별 난이도 차이를 확인했다. 사용자는 정확한 수치는 추후 밸런싱하기로 했다.

### 바 표시 방식

사용자가 보스 체력바 왼쪽 끝이 번져 보이는 것을 발견했다. 긴 바에 둥근 스프라이트를 Filled로 늘린 탓이었다. 스프라이트를 빼니 Filled가 동작하지 않아서, AI가 Fill의 오른쪽 앵커를 체력 비율로 줄이는 방식으로 바꿨다. 사용자가 같은 문제가 다른 바에도 생길 수 있다고 보고 모든 바를 앵커 방식으로 바꾸게 했다. AI는 같은 칸의 마나 바도 함께 바꾸고 공용 함수 `BarFill`로 묶었다.

## 주요 결정

- 몬스터 데이터부터 진행한다. (사용자 결정)
- 1단계 고블린 소굴(고블린 전사·궁수·주술사, 보스 고블린 대장), 2단계 버려진 묘지(해골 병사·궁수, 망령, 보스 해골 기사)로 정한다. (사용자 결정)
- 보스는 스킬을 순서대로 쓰고, 체력이 정한 비율 이하로 처음 떨어지면 한 번 분노한다. (사용자 결정)
- 몬스터 외형은 임시 모델에 색과 크기만 바꾼다. (사용자 결정)
- 몬스터 수치, 배치, 분노 비율과 효과는 [추정]으로 데이터에 두고 추후 밸런싱한다. (사용자 결정)
- 잡몹은 발밑 월드 체력바, 보스는 화면 상단 UI 체력바로 표시한다. (사용자 결정)
- 전투의 모든 바는 Fill 오른쪽 앵커 방식으로 표시한다. (사용자 결정)
- 보스 여부는 화면에서만 쓰므로 `Core`가 아닌 `MonsterData`의 `_isBoss`에 둔다. (AI 제안)
- 테스트용 몬스터 3종과 `Dungeon_Test`를 지우고 `BattleTest` 씬은 `Dungeon_Tier1`을 쓴다. (AI 제안)
- 플레이어 캐릭터는 광장 담당, 낮·밤 사이클은 대장간 담당으로 한다. (팀 결정, 사용자 전달)

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Battle/BattleSetup.cs`, `BattleUnit.cs`, `BattleSimulation.cs`, `BattleFormulas.cs`, `BattleEvent.cs`
- `Assets/02.Scripts/Battle/MonsterData.cs`, `BattleUnitView.cs`, `BattlePlayer.cs`, `BattleTestStarter.cs`, `BattleUnitHpBar.cs`, `BattleBossHpBar.cs`, `BattleHudSlot.cs`, `BarFill.cs`
- `Assets/02.Scripts/Tests/EditMode/BattleSimulationTests.cs`
- `Assets/03.ScriptableObjects/Monsters/`, `Assets/03.ScriptableObjects/Dungeons/`
- `Assets/04.Prefabs/Characters/Monsters/`, `Assets/04.Prefabs/Characters/BattleTest/Monster_Placeholder.prefab`, `Assets/04.Prefabs/UI/Battle/BattleHudSlot.prefab`
- `Assets/05.Art/Materials/Monsters/`, `Assets/01.Scenes/BattleTest.unity`
- `docs/GAME_CONCEPT.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- 에디터 컴파일 오류 없음, EditMode 테스트 92개 통과 (새 테스트 3개 포함)
- AI가 Unity CLI로 `BattleTest` 플레이 모드를 실행하고 화면을 캡처해 1단계 승리와 결과창, 2단계 보스 스테이지의 몬스터, 잡몹·보스 체력바, HUD와 몬스터 바 끝이 번지지 않는 것을 확인했다. 콘솔 오류 없음.

## AI 활용 범위

AI는 작업 순서 정리, 선택지 제시, 코드·테스트 작성, Unity CLI를 통한 몬스터·던전 데이터와 프리팹·씬 구성, 시뮬레이션 수치 확인, 플레이 모드와 화면 검증, 문서 갱신을 맡았다. 작업 순서, 몬스터 구성, 보스 패턴 수준, 외형 방식, 체력바 표시 방식, 바를 앵커 방식으로 통일하는 것, 수치를 추후 밸런싱한다는 방침, 커밋은 사용자가 정했다.

## 후속 작업

- 몬스터 수치 밸런싱, 몬스터 아트
- 부상·사망 결과를 관리 목록에 반영, Fusion 연동, 파견 창
