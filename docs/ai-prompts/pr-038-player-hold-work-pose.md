# PR #38: feat: 플레이어 아이템 들기 자세와 미니게임 망치질 동작 추가

## 개요

- 브랜치: `feat/player-hold-work-pose`
- 대상 브랜치: `dev`
- PR: #38
- 작성일: 2026-11-05
- 담당 영역: 게임플레이·제작(대장간, 캐릭터 애니메이션)
- 작업 목적: 플레이어가 아이템을 들 때와 미니게임을 할 때 몸동작을 보여 줌

## 주요 요청

1. Blender 익스포트에 추가된 HoldItem 애니메이션을 임포트할 것
2. 인벤토리의 아이템을 손에 들면 상반신만 들기 자세, 들지 않거나 빈 칸이면 자세를 취하지 않을 것. 하반신은 대기·걷기·달리기를 그대로 할 것
3. 아이템별 들고 있는 모습은 나중에 하고, 지금은 몸동작만 할 것
4. 대장간 미니게임 중에는 오른손에 망치를 들고 망치질 동작을 하고, 끝나면 망치를 숨길 것. 나중에 미니게임별 동작을 다르게 적용할 수 있게 할 것
5. 미니게임 도중에는 들고 있는 아이템이 보이지 않게 할 것(테스트 후 추가 요청)

## AI와 논의한 내용

### 상반신만 덮어쓰기

Blender 문서의 지침대로 팔 뼈만 체크한 Avatar Mask로 Override 레이어를 만들었다. 베이스 레이어의 대기·걷기·달리기는 그대로 둔다. 레이어 가중치는 스크립트가 바꾸고 기본값을 0으로 두어 모험가·손님에는 영향이 없게 했다.

### 미니게임 중인지 판단하는 방법

AI는 이미 복제되는 작업대 사용권(`ForgeStationState`의 사용 중 여부와 사용자)을 읽는 안을 냈다. 다른 안으로 플레이어에 따로 동기화하는 자세 값을 두는 방법과, 공용 `Characters`에 통합 컨트롤러를 두는 방법을 비교했다. 사용자는 설계안대로 진행하기로 했다.
- 새 네트워크 상태 없이 `Render()`에서 읽기만 한다.
- Player 프리팹이 씬의 작업대를 찾을 수 있게, 작업대가 스폰될 때 등록하는 런타임 에셋(`ForgeStationRegistry`)을 두었다.
- 작업대 종류별 동작 번호와 도구 파츠를 목록으로 두어, 미니게임별 동작이 생기면 애니메이터 상태와 목록만 추가하게 했다.

### 미니게임 중 든 아이템 표시

첫 테스트에서 망치질 도중에도 든 아이템 표시가 보였다. 사용자 요청에 따라 `HeldItemView`에 숨기기를 두고, 작업대를 쓰는 동안 숨기게 했다. 든 칸 자체는 바꾸지 않는다.

### 브랜치와 PR 순서

이 작업은 #34의 모델·애니메이터에 의존한다. #34 병합 뒤에 올린 달리기 교체 커밋은 사용자 결정에 따라 별도 PR(#37)로 먼저 올렸다. 이 작업은 그 동안 보관(stash)했다가 최신 dev에서 이어 갔다.

## 주요 결정

- 들기 자세는 팔만 덮어쓰고, 판단은 인벤토리의 손에 든 아이템 표시가 한다.
- 미니게임 중인지는 작업대 사용권으로 판단하고, 새 네트워크 상태는 두지 않는다.
- 미니게임 중에는 망치질이 들기 자세보다 우선이고, 든 아이템 표시는 숨긴다.
- 지금은 네 작업대 모두 망치질과 망치다. 아이템별 들기 모습과 들기 소켓은 나중에 만든다.

## 변경된 주요 파일

- `Assets/02.Scripts/Characters/HumanoidPose.cs`, `HumanoidAppearanceView.cs`, `FixedHumanoidAppearance.cs`, `HumanoidLookData.cs`
- `Assets/02.Scripts/Forge/ForgeWorkPose.cs`, `ForgeStationRegistry.cs`, `ForgeStationState.cs`
- `Assets/02.Scripts/Inventory/HeldItemView.cs`
- `Assets/05.Art/Animations/Hero/Hero@Common_HoldItem.fbx`, `HumanoidArms.mask`, `Humanoid.controller`
- `Assets/03.ScriptableObjects/Runtime/ForgeStationRegistry.asset`
- `Assets/04.Prefabs/Network/Player.prefab`, `Assets/04.Prefabs/Town/ForgeRoom.prefab`
- `docs/PROJECT_PROGRESS.md`

## 검증 내용

- EditMode 테스트 271개 통과.
- `ForgeTest` 플레이 모드에서 들기 자세 켜짐·풀림, 사용권을 받았을 때 망치질·망치 표시·든 아이템 숨김, 사용권이 풀린 뒤 원래대로 돌아오는 것을 레이어 가중치·애니메이터 상태와 화면 캡처로 확인했다. 사용권은 스크립트로 직접 주고 풀었다.
- 사용자가 첫 테스트에서 망치질 중 든 아이템이 보이는 문제를 찾아 알려 주었다.
- 확인하지 못한 것: 실제 미니게임 시작·종료 흐름, 아이템을 든 채 걷기·달리기, 두 명 접속.

## AI 활용 범위

AI가 한 일:
- 설계안과 대안 제시, 애니메이터·마스크 구성과 코드 작성(Unity CLI)
- 프리팹·씬 연결 확인, 플레이 모드 검증, 브랜치 정리와 문서 갱신
- 에디터가 스스로 바꾼 설정 파일이 커밋에 섞인 것을 찾아 빼고 다시 커밋

사용자가 한 일:
- Blender에서 HoldItem 애니메이션을 만들어 익스포트
- 들기 자세·망치질 규칙과 미니게임 중 든 아이템 숨김을 정함
- 별도 PR 순서와 설계안을 승인

## 후속 작업

- 아이템별로 손에 얹는 모습과 들기 소켓(`ItemSocket_Hold`)
- 미니게임별 작업 동작
