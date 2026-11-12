# PR #68: feat: 채집 맵 자원 채집과 전투 화면 표시, 파견 건너뛰기 페이드

## 개요

- 브랜치: `feat/battle-gathering-resources`
- 대상 브랜치: `dev`
- PR: #68
- 작성일: 2026-11-13
- 담당 영역: 게임플레이·제작
- 작업 목적: PR #67의 후속 작업으로 채집 맵에 시체 더미·자원을 놓고 채집할 수 있게 하고, 채집 중 영업 마감 대기와 파견 건너뛰기 화면 페이드를 넣는다.

## 주요 요청

1. 채집 맵에 시체 더미·자원 배치와 채집 상호작용, 채집 중 "영업 마감 중" 대기.
2. 파견을 건너뛸 때 대장간으로 옮기는 동안 화면 페이드.
3. (작업 중 발견) 채집할 곳이 없을 때 던전 문에 붙어 있으면 안내가 사라지지 않고, 그대로 밤을 넘기면 다음 날 바로 던전에 들어가는 버그 수정.
4. 채집할 자원을 자동 전투 화면에서도 보여 주어, 플레이어가 전투를 보며 다음 날 캘 곳을 알 수 있게 한다.

## AI와 논의한 내용

### 전투 재료 보상과 채집의 관계

지금 구현은 전투가 끝나면 광석·부산물·보석을 공용 창고에 넣고 있어 기획 7.1(전투 보상은 골드·경험치)과 달랐다. 시체 더미로 옮길지, 전투 보상을 유지할지를 물었다.

### 채집 도구, 배치할 자원, GameState 변경

도구 아이템이 아직 없어 도구 확인을 할지, 이번에 어떤 자원을 놓을지, 낮 끝 대기를 위해 `GameState`의 진행 중 작업 등록을 여러 곳에서 받도록 바꿀지(Network 공개 API)를 물었다.

### 던전 문 버그 원인과 해결

문에 닿는 이벤트가 붙어 있는 동안 여러 번 들어와, 안내가 계속 다시 뜨고 밤을 넘기면 채집할 곳이 생긴 직후 들어가 버리는 것으로 보았다. AI는 문이 떨어질 때까지 한 번만 반응하도록 고치는 안을 냈고, 사용자는 파견할 때 플레이어를 바로 대장간으로 옮기는 방법을 제안했다. 두 가지를 함께 적용했다.

### 전투 화면에 자원 보이기

자원 자리는 이미 전투 시드와 시체 자리만으로 정해지므로, 전투 화면과 채집 맵이 같은 함수로 자리를 만들면 같은 자리에 같은 자원이 보인다고 정리했다.

## 주요 결정

- 사용자 결정: 광석은 광맥 채집 오브젝트로만 얻고, 가죽·힘줄·마석·보석은 시체 갈무리로 얻는다. 전투 보상에서 재료를 뺀다.
- 사용자 결정: 채집 도구는 이번에 확인하지 않는다.
- 사용자 결정: 시체 더미, 광맥, 나무, 바닥의 광석·목재를 모두 놓는다.
- 사용자 결정: `GameState`의 진행 중 작업 등록을 여러 곳에서 받도록 바꾼다.
- 사용자 결정: 파견하면 플레이어를 바로 대장간으로 옮긴다. 채집 자원을 자동 전투 화면에도 보여 준다.
- 기획 7.4: 놓는 수·채집량·채집 시간을 [미정]에서 [추정](데이터 `GatheringRules`)으로 바꿨다. 시체 더미는 무리마다 하나이고 몬스터별 갈무리 확률을 쓴다. 채집 중 캐릭터가 멈추고, 인벤토리에 자리가 없으면 시작하지 않는다.
- 구현 중 결정: 자원 상태(캐는 중·다 캠)만 호스트가 동기화하고, 자리와 채집량은 시드로 참가자마다 만든다. 인벤토리에 넣는 것은 하나라도 안 들어가면 아무것도 넣지 않는다(`NetworkInventory.TryAddAll`).

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Battle/GatheringLayout.cs`
- `Assets/02.Scripts/Battle/GatheringNodesState.cs`, `GatheringPlayerActions.cs`, `GatheringNodeView.cs`, `GatheringSiteLayout.cs`, `GatheringSiteView.cs`, `GatheringRulesData.cs`
- `Assets/02.Scripts/Battle/BattleGatheringPreview.cs`, `DispatchSkipFade.cs`, `DispatchState.cs`, `NightDispatchLink.cs`, `DungeonGate.cs`
- `Assets/02.Scripts/Core/Battle/BattleRewardRules.cs`, `BattleDropRules.cs`, `Battle/BattleRewardApplier.cs`, `BattleResultView.cs` (전투 재료 보상 제거)
- `Assets/02.Scripts/Network/GameState.cs`, `Inventory/NetworkInventory.cs`, `Forge/ForgeDayCycleLink.cs`
- `Assets/01.Scenes/Game.unity`, `Battle.unity`, `Assets/04.Prefabs/Town/Gathering/`, `Assets/04.Prefabs/Network/Player.prefab`
- `docs/GAME_CONCEPT.md`, `docs/NETWORK_DESIGN.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- EditMode 테스트 455개 통과(채집 배치·채집량 테스트 6개 추가, 전투 보상 테스트 갱신).
- AI가 Unity CLI로 호스트 혼자 플레이 모드 확인: 실제 승리한 전투 화면의 자원 23개와 다음 날 채집 맵의 23개가 모두 같은 자리, 캐기와 개인 인벤토리 반영, 캐는 중 다른 요청 거절, 영업 마감 대기와 마감 중 거절, 파견 건너뛰기 페이드 순서, 던전 문 한 번만 반응, 파견 시 대장간으로 이동.
- 사용자가 직접 확인: E 키로 캐기와 조작 잠금, 채집 맵·전투 화면의 그레이박스 모습, 걸어서 던전 문에 부딪혔을 때.

## AI 활용 범위

AI는 코드 구조 조사, 설계 제안, 코드·테스트 작성, Unity CLI로 씬·프리팹·데이터 연결, 플레이 모드 확인, 문서 갱신, 커밋·PR 초안을 맡았다. 전투 재료를 채집으로 옮기는 방식, 도구 확인 여부, 놓을 자원, Network 공개 API 변경, 파견 시 대장간 이동, 전투 화면 자원 표시는 사용자가 정했다. 던전 문 버그는 사용자가 발견했고, 직접 플레이 확인과 커밋·PR 승인도 사용자가 했다.

## 후속 작업

- 인벤토리가 찼을 때와 클라이언트 접속(클라이언트 채집, 동시 채집, 클라이언트 페이드) 확인.
- 채집 도구 확인과 제작, 묻힌 보물 상자, 약초·뼈, 어두운 던전 시야.
- 1단계 몬스터 갈무리 확률 조정(시체 더미가 자주 빔), 에메랄드 효과 적용 대상.
