# PR #10: feat: 모험가 데이터·랜덤 생성과 광장 영입, 관리 목록 UI 추가

## 개요

- 브랜치: `feat/adventurer-recruit`
- 대상 브랜치: `dev`
- PR: #10
- 작성일: 2026-11-01
- 담당 영역: 광장 (`Plaza/`, `Core/Adventurers/`)
- 작업 목적: 다른 영역(특히 자동 전투)이 기준으로 삼을 모험가 정보 형식과 생성·영입·관리 목록의 기틀을 만든다.

## 주요 요청

1. 전투 유닛, 광장 모험가 NPC, 상점 손님 NPC, 플레이어 캐릭터가 공통 인간형 인터페이스를 상속할지 검토할 것
2. 모험가 정보(이름, 직업, 레벨, 외형 번호, 보정 전 체력·공격력·방어력, 착용 장비 번호, 몸 상태)를 정의할 것
3. 개발용 씬에서 랜덤 모험가를 생성하고, 플레이어가 다가가 E를 누르면 영입되어 관리 목록에 들어가게 할 것
4. 관리 목록 UI를 만들고 화면 버튼으로 열고 닫을 것
5. 다른 작업자의 코드는 수정하지 않고, PR에 전투 담당자가 이 기틀을 기준으로 수정하도록 안내를 남길 것
6. 스킬과 초상화는 MVP 범위가 아니므로 뼈대만 둘 것
7. 직업은 전사만 만들되 확장할 수 있게 하고, 직업별 능력치 범위와 모험가별 성장값을 둘 것
8. 작업 전부터 있던 `.gitignore` 변경(`.idea/`, `.mcp.json` 제외)도 이 PR에 포함할 것

## AI와 논의한 내용

### 공통 인간형 인터페이스

AI가 네 유닛은 상태의 주인(호스트 `[Networked]`, 클라이언트 예측, `Core` 순수 C# 시뮬레이션)과 구동 방식이 달라 상속으로 묶으면 갓 오브젝트가 된다고 설명했다. 겉모습과 이동 애니메이션만 작은 컴포넌트로 나눠 조합하고, 인터페이스는 같은 방식으로 다뤄야 하는 쪽이 생길 때(예: `IInteractable`) 작게 만들자고 제안했다. 사용자가 받아들였다.

### 저장 형식과 네트워크 구조

- 관리 목록은 공용 `GameState`에 필드를 추가하지 않고 `Plaza/AdventurerRosterState`에 둔다. `Network/` 파일을 고치지 않기 위해서다.
- 레벨·장비·상태가 바뀌므로 시드만 저장하지 않고 정보 전체를 저장한다.
- 후보 NPC는 각자 NetworkObject로 둔다. 나중에 돌아다니게 할 때도 동기화가 자연스럽다.
- 영입은 내 클라이언트가 가장 가까운 후보 한 명을 골라 RPC로 요청하고, 호스트가 정합성(후보 존재, 중복, 빈 슬롯)만 확인한다.
- 테스트 씬은 기존 `NetworkBootstrap`을 재사용해 로비 없이 호스트로 시작한다.

### 스킬과 초상화

AI가 스킬은 수치 대신 `SkillId` 번호만 저장하자고 제안했다. 수치를 조정하면 이미 영입한 모험가에도 반영되고, 직업별·모험가별 어느 쪽으로 정해져도 저장 구조를 바꾸지 않는다. 초상화는 외형에서 나오는 결과물이라 저장하지 않고 `AdventurerPortraitProvider` 뼈대만 둔다. 사용자가 받아들였다.

### 직업 확장과 성장 방식

사용자가 직업별 공격 방식과 능력치 경향, 모험가별 성장값을 설명했다. AI는 직업을 enum 대신 `AdventurerJobData` 에셋으로 만들어 코드 수정 없이 늘릴 수 있게 하고, 직업별 공격 방식은 전투 영역이라 [미정]으로 남겼다. 보정 전 능력치는 기본값과 성장값을 저장하고 `AdventurerStatRules` 한 함수로 계산해 화면과 전투가 같은 값을 쓰게 했다.

### 입력 경로와 동시 영입

사용자가 두 후보가 맞닿아 있을 때 둘 다 영입되는지, E 입력이 `PlayerInputCollector`를 거치는지 물었다. AI가 가장 가까운 한 명만 고르고 요청도 한 번만 보내며, E는 기존 `PlayerNetworkInput`의 Interact 버튼으로 받고 `Network/` 입력 코드는 고치지 않았다고 확인했다. 공유 상태를 바꾸는 요청이라 CLAUDE.md 규칙대로 RPC를 쓴다.

### 두 명 접속 테스트

AI가 처음 만든 테스트 시작 방식은 무작위 세션명이라 두 창이 같은 방에 들어갈 수 없었다. 사용자가 로비가 `PlazaTest`를 불러오도록 임시로 바꾸자고 제안했고, 로컬에서만 바꿔 테스트한 뒤 되돌렸다. 테스트에서 클라이언트 화면의 후보가 원점에 겹쳐 보이는 문제가 나왔고, AI가 후보 프리팹에 `NetworkTransform`이 없는 것을 원인으로 찾아 추가했다.

## 주요 결정

- 공통 인간형 인터페이스 대신 컴포지션을 쓴다. (AI 제안, 사용자 승인)
- 직업은 전사 1종이고 `AdventurerJobData` 에셋으로 늘린다. (사용자 결정)
- 직업별 능력치·성장값 범위가 다르고, 성장값은 모험가마다 타고난다. (사용자 결정)
- 레벨 성장은 `기본값 + 성장값 × (레벨 - 1)`이다. 팀 상의 전이므로 [추정]으로 둔다. (사용자 결정)
- 광장 후보 수는 1~4명 무작위이고 [추정]이다. (사용자 결정)
- 영입 비용은 없고 빈 슬롯이 있으면 항상 성공한다. 명성 등에 따른 성공·실패는 [미정]이다. (사용자 결정)
- 테스트 씬 슬롯은 8칸, 기본 시작 슬롯은 1칸이다. (AI 제안, 사용자 승인)
- 스킬은 `SkillId`만 두고 `SkillId` → `SkillData` 표는 전투 담당이 만든다. 스킬 내용과 초상화는 [고도화]이다. (사용자 결정)
- UI 문구는 한글, 임시 폰트 주아를 쓴다. (사용자 결정)
- 전투 담당 코드는 수정하지 않고 PR 본문에 안내를 남긴다. (사용자 결정)
- `.gitignore` 정리를 같은 PR에 별도 커밋으로 넣는다. (사용자 결정)

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Adventurers/`
- `Assets/02.Scripts/Plaza/`
- `Assets/02.Scripts/Tests/EditMode/AdventurerGeneratorTests.cs`, `AdventurerStatRulesTests.cs`
- `Assets/03.ScriptableObjects/Adventurers/Job_Warrior.asset`
- `Assets/03.ScriptableObjects/Balance/AdventurerGeneration.asset`, `AdventurerRoster.asset`, `AdventurerRoster_Test.asset`
- `Assets/04.Prefabs/Characters/RecruitCandidate.prefab`, `Assets/04.Prefabs/UI/Plaza/AdventurerRosterRow.prefab`
- `Assets/04.Prefabs/Network/Player.prefab`
- `Assets/01.Scenes/PlazaTest.unity`, `ProjectSettings/EditorBuildSettings.asset`
- `docs/GAME_CONCEPT.md`, `docs/PROJECT_PROGRESS.md`
- `.gitignore`

## 검증 내용

- 에디터 컴파일 오류 없음, EditMode 테스트 54개 통과
- AI가 Unity CLI로 `PlazaTest` 플레이 모드를 실행해 후보 생성, 영입, 슬롯 초과 시 실패, 목록 갱신, 창 열고 닫기를 확인했다. 키 입력은 Input System 이벤트로 넣었다.
- 사용자가 빌드한 실행 파일과 에디터로 두 명 접속 동기화와 클라이언트 화면의 후보 위치를 확인했다.

## AI 활용 범위

AI는 설계 비교와 제안, 코드·테스트 작성, Unity CLI를 통한 에셋·프리팹·씬 구성, 플레이 모드 검증, 문서 갱신, 버그 원인 분석을 맡았다. 기획 내용(정보 항목, 직업과 성장 방식, 후보 수, 영입 비용, 스킬·초상 범위)은 사용자가 정했다. 두 명 접속 테스트와 창 모드 설정은 사용자가 직접 했다.

## 후속 작업

- 파견 창, 전투 결과(부상·사망·경험치) 반영 API, 사망한 모험가 목록 제외
- 외형 프리셋을 모델과 초상에 적용
- 낮이 시작될 때 후보 재생성, `Game` 씬 배치
- 레벨 성장 방식과 직업별 공격 방식 팀 논의
