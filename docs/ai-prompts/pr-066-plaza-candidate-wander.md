# PR #66: feat: 광장 영입 후보 배회·매일 교체와 영입 대화 능력치 창 추가

## 개요

- 브랜치: `feat/plaza-candidate-wander`
- 대상 브랜치: `dev`
- PR: #66
- 작성일: 2026-11-12
- 담당 영역: 게임플레이·제작
- 작업 목적: 광장 영입 후보를 매일 낮에 새로 뽑고, 후보가 살아 있는 NPC처럼 배회하며 대화 상대를 바라보게 하고, 영입 대화 중 후보의 능력치를 보여 준다(기획 6.1).

## 주요 요청

1. 영입 후보를 매일 낮이 시작될 때 새로 뽑는다(기획 6.1 `[미정]` 해결).
2. 후보가 제자리에 서 있지 않고 주변을 걷다 멈추며, 대화하는 동안 플레이어와 서로 마주 본다.
3. 영입 대화 중 화면에 후보의 능력치를 보여 준다.
4. 구현 뒤 PR을 올리지 않은 사이에 `dev`가 많이 바뀌어, 최신 `dev`를 받을 때 생기는 충돌을 해결하고 새 로컬 브랜치에서 PR을 올린다.

## AI와 논의한 내용

### 최신 dev와의 충돌 해결

로컬 변경 중 `dev`와 겹치는 파일은 `Game.unity`, `GAME_CONCEPT.md`, `PROJECT_PROGRESS.md` 세 개였다. 임시 폴더에서 3-way 병합을 먼저 시험해 보니 `Game.unity`는 자동 병합되었고, `GAME_CONCEPT.md`는 작업 폴더 파일이 CRLF라 전체 충돌처럼 보였을 뿐 줄 끝을 맞추면 충돌이 없었다. `PROJECT_PROGRESS.md`는 `dev`(#54)에서 간소화된 형식으로 바뀌어, `dev` 버전을 기준으로 로컬 내용을 새 형식에 맞게 다시 썼다. 상세 확인 기록은 새 문서 방침에 따라 PR 본문으로 옮겼다.

방법은 (A) stash → pull → 새 브랜치에서 apply, (B) 기능 브랜치에 먼저 커밋한 뒤 rebase, (C) `pull --autostash`를 비교했고, 백업이 남고 최신 `dev`에서 깨끗하게 브랜치를 딸 수 있는 A를 택했다.

## 주요 결정

- 후보는 매일 낮이 시작될 때 새로 뽑고, 영입하지 않은 후보는 다음 날 사라진다(사용자 결정, 기획 6.1 `[미정]` 정리).
- 능력치 창은 대화하는 플레이어 화면에만 보인다(사용자 결정).
- 충돌은 A 방식으로 해결하고, PR은 최신 `dev`에서 만든 새 브랜치로 올린다(사용자 결정).
- Unity가 자동으로 바꾼 폰트·머티리얼·URP 설정·`ProjectSettings` 변경은 기능과 관계없어 커밋에서 뺐다(사용자 결정).

## 변경된 주요 파일

- `Assets/02.Scripts/Plaza/CandidateWander.cs`, `RecruitStatsChannel.cs`, `RecruitStatsPanel.cs`, `PlazaRecruitment.cs`, `RecruitCandidate.cs`, `AdventurerLabels.cs`, `AdventurerRosterRow.cs`
- `Assets/02.Scripts/Dialogue/DialoguePlayerActions.cs`, `Assets/02.Scripts/Network/PlayerSpawner.cs`, `Assets/02.Scripts/Utils/KeepWorldRotation.cs`
- `Assets/04.Prefabs/UI/Plaza/RecruitStatsUI.prefab`, `Assets/04.Prefabs/Characters/RecruitCandidate.prefab`, `Assets/03.ScriptableObjects/Runtime/RecruitStatsChannel.asset`
- `Assets/01.Scenes/Game.unity`, `PlazaTest.unity`
- `docs/GAME_CONCEPT.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- 최신 `dev`와 병합한 상태에서 Unity 컴파일 오류 없음, EditMode 테스트 451개 통과.
- `PlazaTest` 플레이 모드는 사용자가 직접 확인했다.
- 두 명 접속, 실제 결과창으로 다음 날을 넘기는 경우, 빌드는 확인하지 않았다.

## AI 활용 범위

AI는 후보 배회·매일 교체·능력치 창 구현과 Unity CLI로 씬·프리팹 연결, 충돌 분석과 병합, 진행 문서를 새 형식으로 다시 쓰기, 컴파일과 테스트 실행, 커밋·PR 문안 작성을 했다. 기획 결정(후보 교체 시점, 능력치 창 노출 범위), 충돌 해결 방식과 브랜치 위치, 커밋 대상 파일, 플레이 모드 확인은 사용자가 정하거나 승인했다.

## 후속 작업

- 두 명 접속에서 후보 배회·방향과 능력치 창 확인.
- 전투 결과의 부상·사망을 관리 목록에 반영하는 API.
