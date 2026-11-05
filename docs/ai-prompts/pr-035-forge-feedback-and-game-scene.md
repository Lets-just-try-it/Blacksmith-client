# PR #35: feat: 미니게임 연출 끄기(J 키)와 Game 씬 대장간 구성 추가

## 개요

- 브랜치: `feat/forge-feedback-toggle`
- 대상 브랜치: `dev`
- PR: #35
- 작성일: 2026-11-05
- 담당 영역: 게임플레이·제작(대장간)
- 작업 목적: 미니게임의 손맛 연출을 선택해서 끌 수 있게 하고, 대장간을 로비를 거쳐 들어오는 `Game` 씬으로 옮김

## 주요 요청

1. 미니게임 연출(사운드, 화면 흔들림 등 손맛이 느껴지는 부분)을 J 키로 껐다 켤 수 있게 할 것
2. 대장간 씬(`ForgeTest`)을 게임 씬(`Game`)으로 옮길 것
3. PR을 준비하되 두 작업을 한 PR에 묶고 커밋은 나눌 것

## AI와 논의한 내용

### 연출을 끄는 범위와 방식

연출이 네 미니게임의 `Feedback`·`View`에 흩어져 있어, 호출 지점마다 고치지 않고 공통 낮은 단계에서 스위치를 확인하게 했다.
- 효과음 도우미 4곳과 계속 나는 소리(`ForgeNoiseSynth`), 카메라 `Shake`·`Punch`, 화면 `Flash`, 입자 `Burst`
- 판정 글자, 규칙, 점수, 불꽃·조명 같은 상태 표시와 망치·숫돌의 떨림은 남김
- 스위치는 정적 클래스로 두었다. `Burst`가 정적 도우미이고 `ForgeNoiseSynth`가 오디오 스레드에서 읽어야 해서 참조 연결 없이 적용된다.
- 토글을 프리팹에 넣으려 했으나 `ForgeNotice`가 프리팹 밖(씬)에 있어 씬에 두었다.
- 에디터 포커스가 없어 키 입력을 흉내 낼 수 없어서, 토글 동작을 함수로 분리해 직접 불러 확인했다.

### Game 씬 이동

기존 `Game`은 카메라·조명·바닥·`GameState`·`PlayerSpawner`뿐이었고 첫 커밋 이후 바뀐 적이 없어 `ForgeTest` 구성으로 통째로 바꿨다.
- 호스트 시작은 로비가 하므로 `Bootstrap` 오브젝트에서 `NetworkBootstrap`을 빼고, `ForgeTestStarter`가 비어 있으면 호스트를 시작하지 않게 했다.
- 로비를 거쳐 들어가 동작을 확인하던 중 `ForgeTestStarter`의 `_roster`·`_generation` 참조가 #32 병합 때 빠졌음을 발견해 양쪽 씬에 다시 연결했다.

### PR 나누기

두 작업은 서로 다른 기능이라 규칙상 PR 둘이 맞지만, `Game` 씬이 토글 오브젝트를 포함해 순서 의존이 있어 사용자가 한 PR로 묶고 커밋만 나누기로 정했다.

## 주요 결정

- J 키로 껐다 켜며 값은 이 컴퓨터에 기억한다(사용자)
- 판정 글자와 상태 표시는 연출을 꺼도 남긴다(사용자 요청을 받아 AI가 범위 확정)
- `Game` 씬은 `ForgeTest` 구성으로 교체하고 `ForgeTest`는 시험 씬으로 유지한다
- 한 PR에 두 기능, 커밋은 둘로 나눈다(사용자)
- `GAME_CONCEPT.md` 미니게임 공통에 "J 키 연출 끄기"를 추가했다(기획 변경, 팀 공유 필요)

## 변경된 주요 파일

- `Assets/02.Scripts/Forge/MinigameFeedbackSwitch.cs`, `MinigameFeedbackToggle.cs`
- `Assets/02.Scripts/Forge/ForgeCameraFocus.cs`, `ForgeGreybox.cs`, `ForgeNoiseSynth.cs`, `Forging/Polish/Quench/SmeltingFeedback.cs`, `Forging/Polish/SmeltingView.cs`
- `Assets/02.Scripts/Forge/ForgeTestStarter.cs`
- `Assets/01.Scenes/Game.unity`, `Assets/01.Scenes/ForgeTest.unity`
- `docs/GAME_CONCEPT.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- EditMode 테스트 271개 통과, 컴파일 오류 없음
- 스위치를 끄면 카메라 흔들림·확대·입자가 0, 켜면 정상으로 돌아오는 것을 플레이 모드에서 확인
- 토글(상태 반전, 저장)은 함수로 직접 불러 확인, J 키 입력 연결(`/Keyboard/j`)은 확인
- `Bootstrap` → `Lobby` → 호스트 시작 → `Game` 진입, 플레이어 스폰, 작업대 4개, 카메라 추적, 낮 → 밤 전환과 밤 파견 창(모험가 3명) 확인
- 확인하지 못한 것: 실제 J 키 입력, 미니게임 안에서 소리가 꺼지는 것, 두 명 접속, 마우스 조작

## AI 활용 범위

AI가 한 일:
- 연출 차단 지점 조사와 스위치·토글 구현
- `Game` 씬 교체와 시작 스크립트 수정, 누락된 참조 발견과 재연결(Unity CLI)
- 플레이 모드 검증, 문서 갱신, 커밋 분리, PR 초안 작성

사용자가 정한 일:
- 연출을 J 키로 끄는 것, `ForgeTest`를 `Game` 씬으로 옮기는 것
- 한 PR로 묶고 커밋을 나누는 것, 커밋·PR 승인
- 플레이로 동작 확인

## 후속 작업

- `Game` 씬에 광장(영입)과 대화창 배치, 재료 수급 방식 확정
- 실제 키 입력과 두 명 접속 확인
