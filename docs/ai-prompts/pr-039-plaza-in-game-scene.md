# PR #39: feat: Game 씬에 광장 배치와 구역 이동·화면 페이드 추가

## 개요

- 브랜치: `feat/plaza-in-game-scene`
- 대상 브랜치: `dev`
- PR: #39
- 작성일: 2026-11-05
- 담당 영역: 게임플레이·제작(광장 배치), 네트워크·플랫폼(구역 이동, 화면 전환)
- 작업 목적: 광장을 Game 씬에 넣고, 대장간과 광장을 오가는 이동과 씬 전환에 화면 페이드를 적용함

## 주요 요청

1. PlazaTest 씬의 내용을 Game 씬에 추가할 것. 로스터 패널은 옮기지 않을 것
2. 대장간의 `Entrance_Block`에 닿으면 광장으로, 광장의 대장간 입구 큐브에 닿으면 대장간으로 이동할 것. 광장에 나오면 큐브 앞에 설 것
3. 이동할 때 화면이 어두워진 사이 플레이어를 옮기고 다시 밝힐 것
4. 같은 페이드를 로비 → 게임, 게임 → 자동 전투, 자동 전투 → 게임 전환에도 쓸 것
5. `ForgeRoom` 프리팹은 고치지 말고, `Entrance_Block` 자리에 광장과 같은 이동 큐브를 둘 것(계획 검토 후 요청)
6. 페이드를 별도 PR로 나누지 말 것

## AI와 논의한 내용

### 작업 계획과 광장 위치

AI가 작업 계획을 먼저 설명했다. 사용자가 "Game 씬의 x = 1000에도 전투 공간이 있느냐"고 물었다. AI는 전투 공간이 `Battle` 씬에 있고, 이 씬을 Game 씬 위에 Additive로 불러오기 때문에 겹치지 않게 x = 1000에 둔 것이라고 설명했다. 같은 이유로 광장은 Game 씬 안의 x = 500 근처에 두었다.

### 이동을 누가 판정하고 실행할지

다음 세 가지 안을 비교했다.
- 클라이언트가 닿았는지 판정하고 RPC로 호스트에 요청한다.
- 호스트가 닿았는지 판정하고, 네트워크 상태로 페이드를 시작하게 한다.
- 페이드 없이 바로 이동한다.

"클라이언트 행동은 RPC로 요청한다"는 프로젝트 규칙과 맞고, 화면이 완전히 어두워진 뒤에 이동하는 순서를 로컬에서 보장할 수 있어서 첫 번째 안으로 진행했다.
- 클라이언트는 조작을 잠그고 화면을 어둡게 한 뒤 RPC를 보낸다.
- 호스트는 틱 안에서 `Teleport`로 옮기고 `TravelCount`를 늘린다.
- 클라이언트는 `TravelCount`가 바뀐 것을 받으면 잠금을 풀고 화면을 밝힌다.

### 페이더 구조

Player 프리팹과 Additive로 불러오는 전투 씬은 씬의 페이더를 인스펙터로 참조할 수 없다. 그래서 페이더는 씬마다 하나 두고, 다른 쪽은 Runtime 에셋 `ScreenFadeChannel`로 요청하게 했다. 기존 `BattleChannel`과 같은 방식이다. 게임 ↔ 전투 전환은 호스트가 네트워크 값으로 모든 참가자의 화면을 어둡게 하고, 자기 화면이 다 어두워지면 씬을 바꾸게 했다.

### 테스트 중 발견한 문제

처음 플레이 테스트에서 도착한 뒤에도 캐릭터가 이전 속도로 밀려, 반대편 큐브에 다시 닿아 대장간과 광장을 계속 오갔다. 순간이동할 때 속도를 0으로 만들고, 도착 지점을 큐브에서 더 떨어뜨려 해결했다.

### 다음 날 아침 위치

사용자가 자동 전투 후 다음 날 아침에 플레이어가 어디에 있는지 물었다. AI는 코드를 확인해 전날 밤 위치에서 그대로 이어진다고 답했다. 그리고 지금처럼 두기, 아침에 대장간으로 옮기기, 저녁에 대장간으로 모으기 세 가지 안을 제시했다. 사용자는 기획이 정해지지 않았으니 지금처럼 두고 나중에 정하기로 했다.

## 주요 결정

- 광장은 Game 씬 x = 500 근처에 둔다. 관리 목록은 Game 씬의 기존 `AdventurerRoster`를 쓰고, 로스터 패널과 테스트용 대화 트리거는 옮기지 않는다.
- `ForgeRoom` 프리팹은 고치지 않고, `Entrance_Block` 자리에 이동 큐브를 Game 씬 오브젝트로 둔다.
- 포털 판정과 페이드는 이동하는 클라이언트가 하고, 순간이동은 호스트가 틱 안에서 한다.
- 화면 페이드는 같은 PR에서 구역 이동과 씬 전환(로비 → 게임, 게임 ↔ 전투)에 모두 적용한다.
- 다음 날 아침 시작 위치는 정하지 않고 지금처럼 전날 위치를 유지한다. 진행 문서의 남은 작업에 기록했다.

## 변경된 주요 파일

- `Assets/02.Scripts/Network/AreaPortal.cs`, `PlayerAreaTravel.cs`, `NetworkPlayer.cs`
- `Assets/02.Scripts/UI/ScreenFader.cs`, `ScreenFadeChannel.cs`, `LobbyView.cs`
- `Assets/02.Scripts/Battle/BattleSceneLoader.cs`, `DaySceneBattleCover.cs`
- `Assets/02.Scripts/Forge/ForgeTestCameraFollow.cs`
- `Assets/04.Prefabs/Town/AreaPortal.prefab`, `Assets/04.Prefabs/UI/ScreenFader.prefab`, `Assets/04.Prefabs/Network/Player.prefab`
- `Assets/03.ScriptableObjects/Runtime/ScreenFadeChannel.asset`
- `Assets/01.Scenes/Game.unity`, `Lobby.unity`, `BattleTest.unity`, `DispatchTest.unity`, `ForgeTest.unity`
- `docs/NETWORK_DESIGN.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- EditMode 테스트 271개 통과
- AI가 에디터 플레이 모드에서 확인한 내용은 다음과 같다. 큐브 접촉과 파견·결과 확정은 키 입력 대신 스크립트로 실행했다.
  - 로비 → Game 진입 후 화면이 밝아짐
  - 대장간 ↔ 광장 양방향 이동 후 도착 지점에 멈추고, 카메라가 따라오며 조작 잠금이 풀림
  - 전투 씬 열기·내리기 후 화면이 밝아짐
- 사용자가 두 명 접속에서 페이드와 구역 이동이 동작하는 것을 확인했다.

## AI 활용 범위

- AI: 기존 코드와 씬 구조 분석, 작업 계획과 대안 비교, 스크립트 작성, Unity CLI로 프리팹·씬 구성, 플레이 모드 검증과 문제 수정, 문서 갱신, 커밋·PR 작성
- 사용자: 작업 범위 결정(로스터 패널 제외, `ForgeRoom` 프리팹 유지, 페이드 같은 PR), 다음 날 시작 위치 보류 결정, 두 명 접속 확인, 커밋·PR 승인

## 후속 작업

- 다음 날 아침 플레이어 시작 위치 결정
- 이동 큐브 실제 아트(대장간 쪽 큐브가 입구를 가려 보일 수 있음)
