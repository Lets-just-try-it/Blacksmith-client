# PR #6: feat: Photon Fusion 2 연동과 Host 모드 네트워크 뼈대 추가

## 개요

- 브랜치: `chore/photon-setup`
- 대상 브랜치: `dev`
- PR: #6
- 작성일: 2026-10-31
- 담당 영역: 네트워크·플랫폼 (`Network/`)
- 작업 목적: 역할별 기능 구현 전에 Photon Fusion 2 SDK를 연동하고, 모든 영역이 올라갈 Host 모드 멀티플레이 뼈대를 만든다.

## 주요 요청

1. 사용자가 직접 임포트한 Photon Fusion 2 SDK를 커밋하고, 데모·샘플 파일은 정리할 것
2. PR을 올리기 전에 Host·Client 멀티플레이 구조 뼈대까지 만들어 한 PR로 올릴 것
3. 씬은 `CLAUDE.md`에 적힌 대로 Bootstrap, Lobby, Game으로 구성할 것
4. 참가는 세션명 직접 입력으로 할 것. 세션 목록과 빠른 참가는 만들지 않고, 스팀·스토브 초대는 나중에 붙인다.
5. 입력 처리 구조 설명과 RPC 배치 원칙을 문서에 남길 것

## AI와 논의한 내용

### App ID 공유 방식

사용자가 App ID를 그대로 커밋해 공유해도 되는지 물었다. AI가 저장소가 공개 상태인 것을 확인하고 세 방안을 비교했다.
- 저장소를 비공개로 바꾸고 커밋
- `PhotonAppSettings.asset`을 `.gitignore`로 빼고 App ID는 비공개 채널로 공유
- 공개 저장소에 그대로 커밋 (CCU 도용, 모르는 사람의 세션 접속 위험)

AI는 Fusion 에디터 코드를 읽고, 파일이 없으면 에디터를 열 때 빈 설정 파일이 다시 만들어지는 것을 확인했다. 사용자는 두 번째 방안을 골랐다. App ID 입력은 AI가 대신하지 않고 사용자가 직접 했다.

### Photon 데모 파일 정리

`FusionDemos`, `FusionMenu` 폴더에는 데모 `.unitypackage`만 있고 프로젝트에 풀린 데모 씬은 없었다. `FusionMenuSceneInfo.cs`, 통계 도구 등은 SDK 본체라서 남겼다. 사용자가 두 폴더를 직접 삭제했다.

### 입력 처리 구조

사용자가 `PlayerNetworkInput`과 `PlayerInputCollector`의 차이, `GetInput`의 역할을 물었다. AI가 예시 코드로 설명했다.
- 러너 프리팹의 수집기가 로컬 입력 장치를 읽어 `input.Set()`으로 넘긴다.
- 기능 클래스는 `GetInput()`으로 꺼내 쓴다.
- `GetInput()`은 호스트와 입력한 본인 PC에서만 값을 준다.

사용자가 이해한 내용을 확인한 뒤 문서에 남기기로 했다.

### GameState의 소유와 복제

사용자가 클라이언트도 `GameState` 값을 가져야 하지 않는지 물었다. AI는 다음과 같이 설명했다.
- 호스트 소유는 쓰기 권한(State Authority)을 뜻한다.
- `[Networked]` 값은 모든 클라이언트에 복제된다.
- 클라이언트는 값을 읽고, 바꾸고 싶으면 RPC로 요청한다.

### PlayerMovement의 구조와 위치

**폴더:** AI는 `Network/`의 범위가 인프라라는 점을 들어 플레이어 코드를 `Player/` 폴더로 옮기자고 제안했다. 사용자는 지금은 옮기지 않기로 했다.

**생명주기:** 사용자가 `PlayerMovement`가 네트워크 생명주기를 갖지 않는 구조가 가능한지 물었다. AI는 두 구조를 비교했다.
- 이동 규칙과 틱 실행을 나누는 구조는 테스트하기 좋다.
- 다만 예측 재계산 때문에 규칙에 상태를 두면 안 되고, 지금은 분리할 규칙이 없다.

사용자는 현재 구조를 유지하기로 했다.

### RPC 배치

AI가 두 방식을 비교했다.
- A: `NetworkPlayer` 한 클래스에 모든 RPC를 모으는 방식
- B: Player 프리팹에 영역별 행동 컴포넌트를 붙이는 방식

비교 기준은 갓 오브젝트, 병합 충돌, 역할 경계, 의존 방향, 프리팹 충돌이었다.

사용자는 B를 골랐다.
- 근거: 세 명이 대장간·광장·전투로 영역을 나눠 동시에 개발하고, 프로젝트 규칙도 영역 경계를 중요하게 여긴다.
- A로 하면 세 명이 같은 파일을 고쳐 병합 충돌이 잦아진다. 공용 `Network/`가 각 영역 로직을 알게 된다.
- B로 하면 각 영역의 RPC가 자기 폴더 안에서 끝난다.

### 커밋에 넣을 범위

작업 중 에디터가 URP·Graphics·Project·UnityConnect 설정 파일을 자동으로 바꿨다. 사용자가 직접 바꾼 적이 없는 값이라 이번 PR에서 빼기로 했다.

## 주요 결정

- 사용자 결정
  - SDK 임포트와 Host 모드 뼈대를 한 PR로 올린다. 브랜치 이름은 `chore/photon-setup`이다.
  - `PhotonAppSettings.asset`은 git에서 제외한다. App ID와 지역은 각 로컬에서 최초 1회 직접 입력하고, 값은 팀 비공개 채널로 공유한다.
  - 씬은 Bootstrap → Lobby → Game으로 구성한다. Bootstrap은 지금은 바로 로비로 넘어간다.
  - 참가는 세션명 직접 입력으로 한다. `SampleScene`은 지우지 않는다. 로비 문구는 한글 폰트가 생길 때까지 영어로 둔다.
  - 두 명 테스트는 사용자가 빌드와 에디터로 직접 한다.
  - `PlayerMovement`는 `NetworkBehaviour` 구조를 유지하고 `Network/`에 둔다.
  - 영역별 RPC는 Player 프리팹의 영역별 행동 컴포넌트(예: `ForgePlayerActions`)에 두고, `NetworkPlayer`에는 플레이어 자체에 관한 요청만 둔다. 세 명이 영역을 나눠 동시에 개발하고 규칙도 영역 경계를 중요하게 여기기 때문이다.
  - 입력 처리 설명은 `docs/NETWORK_DESIGN.md`에 두고, `CLAUDE.md` 7번 규칙에서 참조한다.
  - 에디터가 자동으로 바꾼 설정 파일은 이번 PR에서 뺀다.
- 구현 과정에서 정한 것
  - 세션 종료 시 로비 복귀는 씬을 넘어 살아남는 러너 프리팹의 `SessionShutdownHandler`가 맡는다.
  - 호스트는 클라이언트의 카메라를 모르므로, 수집기가 카메라 기준 입력을 월드 방향으로 바꿔 보낸다.
  - 호스트 본인의 `PlayerJoined`가 Game 씬 로드 전에 지나가므로, `PlayerSpawner.Spawned()`에서 이미 접속한 플레이어도 스폰한다.
  - 구식 처리된 `OnUserSimulationMessage` 콜백은 구현하지 않는다.

## 변경된 주요 파일

- `Assets/Photon/` (Fusion 2.1.3 SDK), `Packages/manifest.json`, `ProjectSettings/ProjectSettings.asset`
- `Assets/02.Scripts/Network/NetworkBootstrap.cs`
- `Assets/02.Scripts/Network/PlayerInputCollector.cs`
- `Assets/02.Scripts/Network/PlayerNetworkInput.cs`
- `Assets/02.Scripts/Network/PlayerSpawner.cs`
- `Assets/02.Scripts/Network/PlayerMovement.cs`
- `Assets/02.Scripts/Network/NetworkPlayer.cs`
- `Assets/02.Scripts/Network/SessionShutdownHandler.cs`
- `Assets/02.Scripts/Network/GameState.cs`
- `Assets/02.Scripts/UI/LobbyView.cs`
- `Assets/02.Scripts/Utils/BootstrapLoader.cs`
- `Assets/04.Prefabs/Network/` (`NetworkRunner`, `Player`, `GameState`)
- `Assets/01.Scenes/` (`Bootstrap`, `Lobby`, `Game`), `ProjectSettings/EditorBuildSettings.asset`
- `.gitignore`, `CLAUDE.md`, `docs/NETWORK_DESIGN.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- 컴파일: 경고·오류 없음. EditMode 테스트 15개 통과 (에디터 테스트 실행).
- 플레이 모드 (에디터, 호스트 단독)
  - Bootstrap → Lobby → Host 시작 → Game 씬 로드, 플레이어 스폰을 확인했다.
  - 키 입력에 따른 쿼터뷰 기준 이동, 세션 종료 후 로비 복귀를 확인했다.
  - 키 입력은 Input System에 키 상태를 직접 넣는 방식으로 흉내 냈다.
- 실패 처리: 세션명이 비었거나 App ID가 없어 인증에 실패하면 로비에 실패 문구가 뜨고 다시 시도할 수 있다.
- 작업 중 발견해서 고친 문제
  - CLI로 만든 Game 씬의 `NetworkObject`가 베이크되지 않았다. `Bake Scene Objects`로 고쳤다.
  - 새 Player 프리팹이 Fusion 프리팹 테이블에 없었다. `Rebuild Prefab Table`로 고쳤다.
  - 두 가지 모두 진행 현황 문서에 안내를 남겼다.
- 씬과 프리팹은 YAML을 직접 고치지 않고 Unity CLI로 만들었다.
- 두 명 접속: 사용자가 빌드한 실행 파일과 에디터로 직접 접속해 확인했다. 이상 없음.

## AI 활용 범위

- AI가 한 작업
  - stage 내용 검토: App ID 노출 여부, 커밋되지 않는 폴더의 `.meta` 확인
  - App ID 공유 방안 비교, Fusion 코드를 읽어 설정 파일 재생성 동작 확인
  - 뼈대 설계안과 대안 제시, 스크립트 작성, Unity CLI로 프리팹·씬·빌드 설정 구성
  - 플레이 모드 검증과 문제 수정
  - 입력 처리, GameState 복제, 이동 구조, RPC 배치에 대한 설명과 비교
  - 문서 갱신 초안 작성과 커밋 분할 제안
  - GitHub CLI 설치와 PR 생성
- 사용자가 직접 한 것
  - SDK 임포트와 stage, 데모 폴더 삭제
  - App ID와 지역 입력, GitHub CLI 로그인
  - 빌드와 에디터로 두 명 접속 검증
  - 브랜치 이름, App ID 공유 방식, 씬 구성, 참가 방식, 테스트 방식, 폴더 유지, 이동 구조 유지, RPC 배치 결정
  - 커밋·푸시·PR 승인

## 후속 작업

- 플레이어 캐릭터 담당 결정
- 영역별 행동 컴포넌트 구현 (예: `ForgePlayerActions`, `PlazaPlayerActions`)
- 스팀·스토브 초대로 참가하는 기능
- 한글 폰트 에셋 추가 후 로비 문구 한글화
- 에디터가 자동으로 바꾼 URP·프로젝트 설정 파일의 반영 여부 팀 확인
- `.gitignore`의 `.idea/`, `.mcp.json` 항목 반영
