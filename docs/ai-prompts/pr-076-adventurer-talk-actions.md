# PR #76: feat: 여관 대화로 장비 건네기와 파티 넣고 빼기

## 개요

- 브랜치: `feat/adventurer-talk-actions`
- 대상 브랜치: `dev`
- PR: #76
- 작성일: 2026-11-14
- 담당 영역: 게임플레이·제작
- 작업 목적: 여관 모험가와 대화해 장비를 건네고 파견 파티에 넣고 뺄 수 있게 하고, 실제로 했을 때만 친밀도가 바뀌게 한다.

## 주요 요청

1. 여관 모험가와의 대화 선택지로 장비 건네기와 파티 꾸리기를 할 수 있게 한다.
2. "장비 건네 주기"를 고르면 대화한 플레이어의 슬롯·인벤토리와 모험가 장비 착용 창이 파견 때처럼 열리고 실제로 장비를 건넬 수 있다.
3. "파티에 넣기"를 고르면 파견 파티에 넣는다. 가득 찼으면 오늘 밤은 쉬겠다고 답하고, 목록 창에서 한 명을 빼고 다시 대화해 넣을 수 있다.
4. 창을 여는 것만으로는 친밀도가 오르지 않고, 실제로 장비를 건네거나 파티에 넣었을 때만 오른다.
5. 이미 파티에 든 모험가에게는 "파티에서 빼기"를 띄우고, 빼면 친밀도가 아주 조금 줄어든다.
6. PR #74에서 빠진 AI 협업 기록을 이 PR에 함께 넣는다.

## AI와 논의한 내용

### 씬 UI를 여는 방법

여관 모험가는 호스트가 스폰하는 프리팹이라 씬의 장비 착용 창을 직접 참조할 수 없다. AI가 다음 구조를 제안했다.

- 호스트가 대화한 플레이어에게만 RPC를 보낸다.
- RPC가 ScriptableObject 채널(`InnChannel`)로 창을 연다.
- 장비를 입히거나 파티에서 빼는 요청은 영역별 행동 컴포넌트 규칙에 따라 Player 프리팹의 새 `InnPlayerActions`가 보낸다.

### 파티에 넣어도 대답이 뜨지 않던 문제

사용자가 파티에 넣어도 대사가 뜨지 않는다고 알렸다. AI가 찾은 원인은 이렇다. 호스트에서는 선택 RPC가 바로 실행되어, 결과 대답이 아직 닫히지 않은 원래 대화창에 막혔다. 공통 `DialogueDirector`에서 창을 먼저 닫고 선택을 알리도록 순서를 바꿨고, 사용자가 플레이 모드에서 대답이 뜨는 것을 확인했다.

### 파티에 든 모험가의 선택지

대화 데이터의 조건은 게임 전체 값만 보므로 모험가별 상태를 볼 수 없다. AI는 다음과 같이 구현했다.

- `DialogueSeat`이 상태에 따라 대화를 고르는 `IDialogueSource`를 추가했다.
- 여관 모험가는 파티에 들어 있으면 "파티에서 빼기"가 있는 대화를 쓴다.
- 파티 여부는 호스트가 아무도 대화하지 않을 때만 갱신한다. 대화 중에 바뀌면 화면과 호스트의 선택지 번호가 어긋나기 때문이다.

### 친밀도 증감

사용자가 파티에 넣으면 친밀도가 얼마나 오르는지 물었다. AI는 하루 1회 +3이라고 답했다. 사용자가 빼면 아주 조금 줄라고 해 -1로 했다.

## 주요 결정

- 사용자 결정
  - 대화 선택지로 장비 건네기와 파티 넣기를 하고, 가득 찼을 때는 대답만 하고 넣지 않는다.
  - 실제로 건네거나 넣었을 때만 친밀도가 오른다.
  - 파티에 든 모험가는 "파티에서 빼기"가 뜨고, 빼면 친밀도가 조금 준다.
- 구현 중 AI가 정한 것(팀 확인 필요)
  - 참가자 누구나 여관에서 장비를 건네고 파티를 넣고 뺄 수 있다(9절 [미정] 해소).
  - 장비 건네기 친밀도는 여관 대화로 건넸을 때만 준다. 밤 파견 창에서 입히면 오르지 않는다.
  - 대화로 빼면 뺄 때마다 -1이고, 목록 창으로 빼면 바뀌지 않는다.
  - 건네기 창은 그 모험가 한 명, 공용 창고와 대화한 플레이어 개인 슬롯만 보여 준다.
  - 대답 대사는 임시 문구다.

## 변경된 주요 파일

- `Assets/02.Scripts/Plaza/InnResident.cs`, `InnResidents.cs`, `InnPlayerActions.cs`, `InnChannel.cs`
- `Assets/02.Scripts/Plaza/AdventurerRosterState.cs`, `AdventurerRosterView.cs`, `AdventurerRosterRow.cs`
- `Assets/02.Scripts/Inventory/AdventurerEquipView.cs`
- `Assets/02.Scripts/Battle/DispatchState.cs`
- `Assets/02.Scripts/Dialogue/DialogueDirector.cs`, `DialogueSeat.cs`, `IDialogueSource.cs`
- `Assets/02.Scripts/Core/Adventurers/AffinityRules.cs`
- `Assets/03.ScriptableObjects/Dialogues/Inn_Resident_InParty.asset`, `Inn_Party_*.asset`
- `Assets/04.Prefabs/Characters/InnResident.prefab`, `Network/Player.prefab`, `UI/Plaza/AdventurerRosterRow.prefab`, `Assets/01.Scenes/Game.unity`
- `docs/GAME_CONCEPT.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- 컴파일 오류 없음. EditMode 테스트 전체 533개 통과.
- 사용자가 플레이 모드(호스트)에서 "파티에 넣기" 대답이 뜨는 것을 확인했다.

## AI 활용 범위

- AI가 한 일
  - 코드 분석과 구조 제안.
  - 구현과 테스트 작성.
  - Unity CLI로 에셋·프리팹·씬 연결.
  - 대답이 뜨지 않던 원인 분석, 기획·진행 문서 갱신.
- 사용자가 한 일
  - 기능 범위와 친밀도 규칙을 정했다.
  - 플레이 모드에서 대답 표시를 확인했다.
  - 커밋과 PR 내용을 승인했다.

## 후속 작업

- 플레이 모드에서 장비 건네기, 파티에서 빼기, 목록 창 빼기 버튼 확인.
- 두 명 접속에서 클라이언트 동작 확인.
- 대답 대사 확정.
- 약초·포션 건네기와 그 친밀도.
