# PR #31: feat: ForgeTest 밤 파견 연결과 시계 오른쪽 위 이동, 파견·장비 창 UI 프리팹화

## 개요

- 브랜치: `feat/forge-night-dispatch-v2`
- 대상 브랜치: `dev`
- PR: #31
- 작성일: 2026-11-04
- 담당 영역: 대장간(ForgeTest), 전투(밤 파견 연결)
- 작업 목적: ForgeTest에서도 밤 파견까지 확인할 수 있게 연결하고, 미니게임 방법 창과 겹치는 시계를 옮김

## 주요 요청

1. 새 브랜치에서 ForgeTest 씬도 밤이 되면 파견 창이 뜨게 연결할 것
2. 대장간 미니게임의 진행 방식 UI(방법 창)가 시계와 겹치므로 시계를 오른쪽 위로 옮길 것
3. PR #30이 squash로 병합된 뒤 브랜치 정리를 위한 강제 푸시가 거부되자, 새 브랜치로 옮길 것

## AI와 논의한 내용

### UI를 두 씬에서 같이 쓰는 방법

ForgeTest에 파견 창·장비 착용 창·미리보기 무대를 새로 붙여야 했다. AI는 DispatchTest의 기존 UI를 그대로 복사하지 않고 프리팹으로 만들어 두 씬이 같이 쓰게 했다.
- 프리팹으로 만든 것: 파견 창, 장비 착용 창, 미리보기 무대, 시계, 밤 문구
- 프리팹 밖의 씬 오브젝트 참조(관리 목록, 파견 상태, 공용 창고, GameState)는 씬마다 연결한다.

### ForgeTest의 시험용 모험가

ForgeTest에는 관리 목록과 모험가가 없어 밤 파견 창이 비게 된다. AI는 `ForgeTestStarter`가 InventoryTest와 같은 방식으로 직업마다 1명씩 넣게 했다.

### 브랜치 정리

PR #30이 squash로 병합된 뒤에는 이 브랜치에 #30 커밋이 중복으로 남았다. AI는 지난번처럼 새 dev 위로 다시 쌓고 강제 푸시하려 했지만, 원격 이력을 덮어쓰는 동작이라 권한 단계에서 거부되었다. AI는 두 가지를 제시했다.
- 새 브랜치로 옮기기
- 강제 푸시 허용

사용자는 새 브랜치로 옮기기를 골랐다. AI는 새 dev에서 브랜치를 만들어 커밋 3개를 옮기고, 파일 내용이 원래 브랜치와 같은 것을 확인했다.

## 주요 결정

- ForgeTest도 DispatchTest와 같은 방식으로 밤 파견을 연결한다.
  - 관리 목록, 파견 상태, 밤 파견 연결, 파견·장비 창, 전투 중 낮 화면 끄기
  - 시험용 모험가는 직업마다 1명
- 파견 창·장비 착용 창·미리보기 무대·시계·밤 문구 UI는 프리팹으로 두 씬이 같이 쓴다.
- 시계는 오른쪽 위에 둔다. DispatchTest의 "파견 창" 버튼은 시계 왼쪽으로 옮긴다.
- PR은 강제 푸시 없이 새 브랜치(`feat/forge-night-dispatch-v2`)로 올린다.

## 변경된 주요 파일

- `Assets/04.Prefabs/UI/Battle/DispatchUI.prefab`
- `Assets/04.Prefabs/UI/Inventory/AdventurerEquipUI.prefab`, `AdventurerPreviewStage.prefab`
- `Assets/04.Prefabs/UI/DayCycleHud.prefab`, `NightTransitionUI.prefab`
- `Assets/01.Scenes/DispatchTest.unity`, `ForgeTest.unity`
- `Assets/02.Scripts/Forge/ForgeTestStarter.cs`
- `docs/GAME_CONCEPT.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- `ForgeTest` 플레이 모드에서 다음을 확인했다. 콘솔 오류 없음.
  - 시계가 오른쪽 위에 보이는 것
  - 낮이 끝나면 밤 파견 창이 시험용 모험가 3명과 함께 뜨는 것
  - 파견하면 전투 씬이 올라오고, 결과를 확인하면 2일째 낮이 시작되는 것
- `DispatchTest` 플레이 모드에서 시계와 "파견 창" 버튼이 겹치지 않는 것을 확인했다.
- 화면 캡처로 두 씬의 시계 위치, ForgeTest의 밤 파견 창과 전투 장면을 확인했다.
- 작업 중 확인하고 고친 것: 스크립트로 넣은 전투 채널과 관리 목록 데이터 참조가 씬에 저장되지 않아 Unity CLI로 다시 지정했다.
- 새 브랜치로 옮긴 뒤 파일 내용이 확인한 상태와 같은 것을 확인했다.
- 버튼은 함수를 직접 불러 확인했다. 실제 마우스와 두 명 접속은 확인하지 못했다.

## AI 활용 범위

AI가 한 일:
- ForgeTest·DispatchTest 구성과 UI 참조 조사
- 프리팹화와 씬 연결 구성 (Unity CLI)
- 시험용 모험가 코드 작성
- 플레이 모드 확인과 화면 캡처
- 문서 갱신, 커밋 분리, 새 브랜치로 옮기기, PR 초안 작성

사용자가 정한 일:
- ForgeTest 밤 파견 연결과 시계 오른쪽 위 이동
- 새 브랜치에서 작업하기
- 강제 푸시 대신 새 브랜치로 옮기기
- 커밋·푸시·PR 승인

## 후속 작업

- `Game` 씬에 프리팹과 관리 목록·파견·낮·밤 연결 배치
- 실제 미니게임 도중 낮이 끝나는 경우, 실제 마우스와 두 명 접속 확인
- 쓰지 않게 된 `feat/forge-night-dispatch` 브랜치 정리
