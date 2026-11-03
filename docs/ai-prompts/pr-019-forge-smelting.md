# PR #19: feat: 정제 미니게임 3막 규칙과 작업대 화면 추가

## 개요

- 브랜치: `feat/forge-smelting`
- 대상 브랜치: `dev`
- PR: #19
- 작성일: 2026-11-03
- 담당 영역: 게임플레이·제작
- 작업 목적: 프로토타입의 정제(원석 쪼개기 → 풀무질 → 쇳물 붓기)를 규칙과 작업대 화면까지 옮긴다.

## 주요 요청

1. 정제를 프로토타입 기준으로 구현(규칙, 데이터, 테스트, 작업대 화면, 소리·타격감 포함)
2. 확인하지 못한 것은 사용자 확인 후 PR 작성
3. 화면 다듬기: UI 겹침, 균열 표시 가림·모양, 카메라 거리, 곡괭이·쇳물 모양, 쇳물 기준 낮추기, 플레이어 솟구침 수정(플레이어 프리팹 수정 없이)

## AI와 논의한 내용

### 기준 버전

처음에 옛 `Scripts.zip` 기준으로 만든 정제(불과 찌꺼기 걷기 동시)는 사용자가 프로토타입과 다르다고 확인해 폐기했고, 프로토타입 폴더의 최신 3막 구조로 다시 옮겼다.

### 플레이어 솟구침

원인은 플레이어 `CharacterController`의 계단 오름 높이였다. 네트워크 영역 프리팹이라 사용자 요청으로 프리팹은 바꾸지 않고, 정제 작업대 충돌체(둥근 모서리, 높은 충돌체)로 해결했다. 다른 작업대 모서리에서도 같은 현상이 있을 수 있다.

### 해상도 느낌

프로젝트 MSAA가 꺼져 있어, 정제 중에만 카메라 SMAA를 켰다. 프로젝트 전체 MSAA는 팀과 정할 일이다.

## 주요 결정

- 정제 방식: 프로토타입 최신 3막(원석 쪼개기, 풀무질, 쇳물 붓기). 광석 등급 0, 1, 2의 수치를 데이터로 둔다.
- 화면: 작업대 위 0.3배로 줄인 임시 그레이박스 무대, 단계마다 카메라 이동, UI에 가리지 않도록 시야를 넓히고 무대를 위로 올림.
- 쇳물 눈금은 시각 높이를 20% 낮췄다(채점 규칙 불변).
- 기획: `GAME_CONCEPT.md`에 정제 3막 세부를 추가했다. 팀 공유 필요.

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Forge/SmeltingGame.cs`, `SmeltingRules.cs`, `MiniInput.cs`, `MiniEvent.cs`
- `Assets/02.Scripts/Forge/Smelting*.cs`(Station, Stage, View, Feedback, Boulder/Furnace/PourVisual, CrackMarkers, Greybox, Layout, RulesData), `ForgeCameraFocus.cs`, `ForgeProceduralSfx.cs`
- `Assets/03.ScriptableObjects/Balance/SmeltingRules.asset`
- `Assets/04.Prefabs/Town/ForgeRoom.prefab`
- `Assets/02.Scripts/Tests/EditMode/SmeltingGameTests.cs`
- `docs/GAME_CONCEPT.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- EditMode 테스트 141개 통과, 컴파일 오류 없음
- 시드 결정성, 봇으로 한 판 진행(정확도 0.99), 화면 좌표와 균열 좌표 변환 왕복 검사
- 사용자가 `ForgeTest`에서 정제 한 판을 직접 확인

## AI 활용 범위

규칙, 화면, 테스트 작성과 수정은 AI가 했다. 방식 선택, 화면 판단(겹침, 모양), 플레이 확인은 사용자가 했다.

## 후속 작업

- 실제 모델·효과음, 작업대 사용권·결과 전송, 나머지 미니게임 4종
- 담금질·단조를 최신 프로토타입과 대조
- 프로젝트 전체 MSAA 결정
