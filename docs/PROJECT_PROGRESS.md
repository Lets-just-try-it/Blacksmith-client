# Tempered Heroes (가제) 프로젝트 진행 현황

> 기준일: 2026-11-12
>
> 이 문서는 자신이 들어 있는 브랜치의 상태를 설명한다. 시스템별 상태, 핵심 진입점, 확인하지 않은 것, 남은 작업만 적는다. 구현 방법과 확인 기록은 PR 본문과 `docs/ai-prompts/`에 적는다. 개발 환경 설정은 `docs/SETUP.md`를 따른다.

## 요약

Unity 6 URP 2.5D 쿼터뷰 대장간 경영·모험가 파견 RPG다. 낮에는 던전에서 재료를 채집해 장비를 제작하고 의뢰·상점·모험가에게 쓰며, 밤에는 모험가를 파견해 자동 전투 결과를 확인한다. 게임 흐름과 데모 1 범위는 `docs/GAME_CONCEPT.md`, 네트워크 규칙은 `CLAUDE.md`의 "네트워크 구조"와 `docs/NETWORK_DESIGN.md`를 따른다.

- 씬: `Bootstrap` → `Lobby` → `Game`이 실제 흐름이다. `PlazaTest`, `ForgeTest`, `BattleTest`, `InventoryTest`, `DispatchTest`는 로비 없이 호스트로 시작하는 개발용 씬이다. `Battle` 씬은 밤 파견 때 Additive로 불러온다.
- `Game` 씬: 대장간(`ForgeRoom` 프리팹), 공용 창고, 낮·밤, 밤 파견과 던전 지도, 광장(x = 500 근처), 대화, 전투 보상 반영이 배치되어 있다. 시험용 재료와 모험가는 `StartSupply`가 넣는다.

## 시스템별 상태

상태: 구현 / 진행 중 / 미구현. 진입점은 코드를 찾기 시작할 곳이다.

### 장비 제작 (진행 중)

- 미니게임: 정제(`SmeltingGame`), 단조(`ForgingGame`), 담금질(`QuenchGame`), 연마(`PolishGame`), 판 성형(`PlateGame`), 조립(`AssembleGame`), 활 깎기(`BowSawGame`), 시위 걸기(`BowStringGame`), 활 몸체 휘기(`BowBendGame`), 시험 사격(`BowRangeGame`, 3D 사격장 `BowRangeStage`), 장신구(`NecklaceGame`). 규칙은 `Core/Forge`, 수치는 `03.ScriptableObjects/Balance/*Rules.asset`. 조립 작업대는 `Forge/AssembleStation`(+ `AssembleView`, `AssembleBoardView`, `AssembleStage`)이고, 아직 `ForgeTest`에서 직접 끝까지 해 보지 않았다.
- 제작 흐름: 작업대 사용권 `ForgeStationState`(호스트) + `ForgeStationLink`, 결과 RPC `ForgePlayerActions`. 공용 제작 목록 창 `CraftingMenuView`, 재료 예약 `Core/Forge/CraftingReservation`, 재료 판정 `Core/Forge/CraftingRequirements`, 정확도 누적·등급 환산 `Core/Forge/CraftingRules`, 레시피 `CraftingRecipeData`.
- 정제: 광석 1개 → 주괴 1~4개(정확도별, `SmeltingRules.YieldTiers`, 구간은 임시이며 밸런스 테스트 후 확정). 주괴는 등급·정확도 없이 쌓이고(`ItemKind.Material`), 정제 점수는 장비 등급에 들어가지 않는다(`CraftingEntry.ScoredStepsDone`). 레시피: 구리 대검, 철 대검, 구리·철 투구/갑옷(판 성형 → 조립), 구리·철 활(정제 → 활 몸체 휘기 → 활 깎기 → 시위 걸기 → 시험 사격, 주괴·참나무 목재·힘줄 사용), 목걸이 12개. 레시피는 아직 재료 1개 → 장비 1개이고 부재료를 받지 않는다.
- 등급: `Core/GradeRule` + `GradeThresholds` (구현).
- 시험: `ForgeTest`와 `Game`의 공용 창고에 시험용 재료가 들어 있다(광석, 주괴, 참나무 목재, 힘줄, 보석. 작업 중인 중간 형태 아이템은 넣지 않는다). `ForgeTestStarter`의 지급 목록에서 바꾼다. 티어별 난이도·판매 기준가 `TierRules`.
- 연출 끄기: `J` 키(`MinigameFeedbackToggle`, `MinigameFeedbackSwitch`).
- 작업 자세: Player 프리팹의 `Forge/ForgeWorkPose`가 `ForgeStationRegistry`에서 사용 중인 작업대를 찾아 망치질 자세를 켠다.

### 대장간 맵 (진행 중)

- `04.Prefabs/Town/ForgeRoom` 그레이박스(제작 구역 하나 34m × 26m, 작업 순서대로 배치(가운데 줄 정제·단조·담금질·연마, 앞줄 창고·판 성형·조립·장신구대, 뒷줄 활 몸체 휘기·활 깎기·시위 걸기·시험 사격), 같은 4m × 4m 칸 12개: 작업대 11개(조립, 활 몸체 휘기, 활 깎기, 시위 걸기, 시험 사격 포함), 공용 창고, 칸 바닥 표시 `SlotFloor`, 작업대·소품 그레이박스 모양, 입구 통로와 광장 포털 `PlazaExitPortal`, `ForgeDoor`). `ForgeTest`에서 F1~F6 시점, V 1인칭(`ForgeTestCameraFollow`).
- 실제 에셋 적용은 남았다. `ForgeTest`는 대장간 담당 영역이다.

### 인벤토리 (진행 중)

- 규칙 `Core/Inventory`: `ItemStack`, `InventoryGrid`, `EquipmentRules`(부위·직업 무기, 무기 벗기기 금지), `EquipmentStatRules`.
- 네트워크: `NetworkInventory`, 공용 창고 `SharedInventoryState`(5칸 × 10줄 → 최대 40줄), 플레이어 슬롯 `PlayerInventoryState`(8칸), RPC `InventoryPlayerActions`, 화면 연결 `InventoryChannel`.
- 화면: `InventoryView`, `PlayerInventoryHud`, `InventorySplitPopup`(Ctrl 나누기), 손에 든 슬롯 `HeldSlotSelector`·`HeldItemView`.
- 장비 착용 창: `AdventurerEquipView`, `AdventurerEquipping`, 미리보기 `AdventurerPreview`.
- 아이템: `03.ScriptableObjects/Items` 번호 1~45, 표 `ItemCatalog`. 1티어 데이터 이름은 아직 "구리"다(기획은 청동).

### 낮·밤 사이클 (진행 중)

- `Network/GameState`(`Day`, `DayPhase`, 낮 `TickTimer`, `Gold`), 규칙 `Core/DayCycleRules`, 수치 `DayCycle` 데이터(600초, 10시~20시).
- 연출: `UI/NightTransitionView`, 조명 `Forge/DayNightLighting`, 시계 `UI/DayCycleHud`.
- 연결: `ForgeDayCycleLink`(마감), `NightDispatchLink`(밤 파견).
- `Game` 씬의 "밤으로 넘기기 (테스트)" 버튼(`UI/SkipToNightButton`)은 출시 전에 뺀다.

### 던전·파견·자동 전투 (진행 중)

- 규칙 `Core/Battle`: `BattleSimulation`(시드 기반, 맵 위 무리 전투, 대열, 스킬, 분노), `StagePathField`(8방향 격자 경로), `BattleGroupRules`, `BattleRewardRules`, `BattleDropRules`, `DungeonProgressRules`, `DispatchRules`, `AccessoryEffect`.
- 데이터: `DungeonData`(`Dungeon_Tier1`, `Dungeon_Tier2`만 있음), `03.ScriptableObjects/Monsters`, `AdventurerBattle`(직업별 전투 수치·스킬 표).
- 흐름: 던전 지도 `DungeonMapView` → 파견 창 `DispatchView`/`DispatchState` → `BattleSceneLoader`가 `Battle` 씬 Additive 로드 → `BattleSceneFlow` 재생, `BattleState` 동기화 → 결과창 확인 → `DungeonProgressState` 반영, `BattleRewardApplier`(`Game` 씬만)가 골드·재료·경험치 반영. 전투 중 낮 화면은 `DaySceneBattleCover`가 끈다.
- 지도는 씬마다 `DispatchUI`의 `DungeonMap`에 `_state`·`_progress`를 연결해야 켜진다. `DispatchTest`와 `Game`에 연결했다.
- 스테이지 맵: `Core/Battle/StageMapGenerator`가 전투 시드로 타원 덩어리를 이어 붙인 넓은 맵을 만들고, `StageMapValidator`가 한 덩어리·구멍 없음·최소 폭을 확인한다. `Battle` 씬의 `StageMap`(`StageMapView`)이 시작 지점을 원점에 두고 바닥 메시를 깐다. 수치와 바닥 머티리얼(던전마다 하나)은 `StageMap_Tier1`·`StageMap_Tier2`(던전 데이터가 참조), 바닥은 `ThirdParty/FloorTiles01`이다. `Core/Battle/StageLayoutGenerator`가 같은 시드로 몬스터 무리·보스 스폰 지점을 정한다. 맵과 스폰 지점은 `DungeonData.AddGroupsTo`가 `BattleSetup`에 넣는다.
- 전투: 잡몹 무리는 처음부터 스폰 지점에 서 있고, 모험가는 처음에만 대열로 서고, 그 뒤로는 남은 무리 중 걸어서 가장 가까운 무리로 각자 격자 경로를 따라 걸으며 서로 비켜 선다(NavMesh 안 씀, 걷는 속도는 `BattleRules`의 `_marchSpeed`, 원거리가 앞서면 `_lineUpSpeedPercent`만큼 속도를 바꿔 근접이 앞에 서게 함). 인간형 유닛은 화면 이동 속도를 애니메이터 `Speed`에 넣어 대기·걷기·달리기 블렌드를 쓴다(`BattleUnitView`). 모험가가 무리 시야(`MonsterData` 시야 거리)에 들면 무리가 다가와 싸운다. 잡몹 무리를 모두 토벌하면 보스가 보스 자리에 나타난다. 결과는 토벌 순서(`BattleResult.ClearedGroups`)로 보상·경험치를 계산한다. 화면에 토벌 진행도(`BattleProgressLabel`)를 보여 준다.
- 시체: 몬스터가 쓰러지면 잠시 뒤 그 자리의 시체(`BattleCorpses`, 그레이박스 `04.Prefabs/Characters/Monsters/MonsterCorpse`, 몬스터 데이터의 시체 프리팹이 있으면 그것)로 바뀌어 남는다. 같은 무리 시체끼리 겹치면 옆으로 비켜 놓아 시체 무리로 보인다. 자리는 `BattleResult.Corpses`에 남는다.
- 채집할 맵: 낮 씬 `DispatchSession`의 `GatheringSiteState`(호스트 소유)가 이긴 전투의 던전·스테이지·시드·시체 자리를 담고, 다음 밤에 파견하지 않거나 져서 결과를 반영하면 비운다. 맵은 `DungeonData.GenerateMap(seed)`로 다시 만든다. `Game`·`DispatchTest`·`ForgeTest`에 연결했다.
- 채집 맵 입구: `Game` 씬 광장 북쪽의 `DungeonGate`(문)에 닿으면 채집할 곳이 있으면 채집 맵 시작 지점으로 옮기고, 없으면 "던전에 몬스터가 잔뜩 있습니다. 위험합니다."를 띄운다(`ForgeNotice` 재사용). 채집 맵은 x = 2000의 `GatheringSite`(`GatheringSiteView`)가 같은 시드로 바닥만 깔고(`StageMapView` 충돌 켬: 바닥과 가장자리 벽), 시작 지점 옆 `PlazaReturnPortal`로 광장에 돌아온다. 맵은 참가자마다 들어갈 때 화면이 어두워진 사이에 만들고(호스트는 다른 플레이어가 문에 닿을 때), 채집할 맵이 바뀌면 안에 플레이어가 없을 때 버린다. 시체·자원·채집 상호작용은 아직 없다.
- 몬스터 재료 드랍이 아직 있다. 2026-11-10 기획과 달라 바꿀 대상이다(남은 작업 3).

### 모험가 영입 (진행 중)

- 규칙 `Core/Adventurers`: `AdventurerData`, `AdventurerGenerator`(후보 1~4명), `AdventurerStatRules`, 레벨 `AdventurerLevelRules`(필요 경험치 데이터 `Battle/AdventurerLevelData`).
- `Plaza/`: 관리 목록 `AdventurerRosterState`(최대 8), `PlazaRecruitment`, `RecruitCandidate`(대화에서 "영입하기"로 영입).
- 매일 교체: `PlazaRecruitment`가 `GameState.PhaseChanged`로 날짜가 바뀐 낮에 남은 후보를 디스폰하고 새로 뽑는다. `GameState`가 없는 `PlazaTest`에서는 스폰 때 한 번만 뽑는다.
- 배회: `CandidateWander`(호스트가 움직이고 `NetworkTransform`이 복제)가 스폰 자리 반경 1m를 걷다 멈춘다. 말을 걸면 멈춰 플레이어를 바라본다. 플레이어도 대화 중 상대 쪽으로 돈다(`DialoguePlayerActions`). 후보가 플레이어를 찾도록 `PlayerSpawner`가 `Runner.SetPlayerObject`로 등록한다. 이름표 등은 `Utils/KeepWorldRotation`으로 방향을 유지한다.
- 능력치 창: 영입 대화 중 말을 건 플레이어 화면 왼쪽에 뜬다(`RecruitCandidate` → `RecruitStatsChannel` → `RecruitStatsPanel`, 프리팹 `04.Prefabs/UI/Plaza/RecruitStatsUI`를 `Game`·`PlazaTest`에 배치). 문구는 관리 목록과 같은 `AdventurerLabels.FormatStats`.
- 초상화는 임시 이미지다(`AdventurerPortraitProvider`).

### 인간형 캐릭터 (구현)

- 모델 `05.Art/Models/Characters/Hero/Hero.fbx`, 프리팹 `04.Prefabs/Characters/Humanoid`, 외형 목록 `HumanoidLookData`, 적용 `HumanoidAppearanceView`, 모험가 외형·장비 `AdventurerLook`, 자세 `HumanoidPose`.
- 셰이더 `05.Art/Shaders/HumanoidPaletteToon`(팔레트 텍스처 `Hero_Palette.png`).
- 주의: 외형 목록은 번호가 위치라 끝에만 추가한다. 애니메이션 FBX는 `Preserve Hierarchy`를 켜고, 다시 익스포트할 때 `.meta`를 유지한다. 팔레트는 Blender 원본과 함께 고친다.

### 대화 (진행 중)

- 규칙 `Core/Dialogue`(`DialogueCondition`, `DialogueChoiceCursor`), `Dialogue/`의 `DialogueDirector`, `DialogueView`, `DialogueChannel`, `SharedDialogue`(함께 보는 대화), RPC `DialoguePlayerActions`.
- 데이터 `03.ScriptableObjects/Dialogues`. 튜토리얼·컷씬 연결과 명성 등 조건 값이 남았다.

### 구역 이동·화면 페이드 (구현)

- `04.Prefabs/Town/AreaPortal`, `Game` 씬의 `ForgePlazaLink`, `ScreenFader`/`ScreenFadeChannel`(`docs/NETWORK_DESIGN.md` 3.2절). 전투 결과를 반영하거나 파견을 건너뛰면 `PlayerSpawner.ReturnAllToSpawnPoints`로 모두 대장간으로 옮긴다(채집 맵에 있던 플레이어 포함).

### Photon Fusion 2 (진행 중)

- 로비의 Host 시작·Client 참가, 플레이어 스폰, `NetworkInput` 예측 이동, 입력 잠금 `PlayerNetworkInputLock`.
- 남음: 스팀·스토브 초대, 재접속 처리.

### 미구현

- 대장간 업그레이드·해금, 저장/불러오기, 방송 연동.
- 데모 1 기능(채집, 상점, 의뢰, 여관, 친밀도, 내구도 등)은 남은 작업 3.

### 자동화 테스트 (일부)

- `02.Scripts/Tests/EditMode`에 `Core` 규칙의 EditMode 테스트가 있다. 실행: 에디터를 연 채 `unity command test run editor`.
- 가격 규칙 등 일부 `Core` 규칙은 결정성 테스트가 없다. 빌드 검증은 하지 않았다.

## 확인하지 않은 것

대부분의 화면 입력은 화면 함수나 RPC를 코드로 불러 확인했다. 아래는 아직 실제로 확인하지 않았다.

- 실제 마우스 조작: 제작 목록 끌어다 놓기, 장신구 미니게임, 장비 착용 창, 파견 창·지도 버튼.
- 두 명 접속: 작업대 사용권 거절, 인벤토리·장비 착용, 파견 창·지도(참가자 보기 전용), 낮·밤 전환, `Game` 씬 전체 흐름. (Host 이동, 영입, 대화, 전투 재생은 두 명으로 확인했다.)
- 광장 후보: 두 명 접속에서 클라이언트 화면의 배회·방향, 클라이언트 플레이어가 대화 상대 쪽으로 도는 것, 클라이언트의 능력치 창. 실제 결과창으로 다음 날을 넘겼을 때의 후보 교체(스크립트로만 넘겨 봄).
- 실제 미니게임 도중 낮이 끝나는 경우.
- 보스 등장 연출, 바닥 드랍, 토벌 진행도의 플레이 모드 화면(무리 이동·교전은 `BattleTest`로 확인함). 두 명 접속에서 무리 전투가 같게 재생되는지.
- 스테이지 맵: 두 명 접속에서 같은 맵이 나오는지, 전투 카메라로 본 맵 가장자리.
- 채집할 맵(`GatheringSiteState`): 플레이 모드에서 승리·패배·파견 건너뛰기 뒤 값이 바뀌는지, 참가자에게 같은 값이 보이는지.
- 채집 맵 입구: 혼자(호스트) 막힘 안내·들어가기·돌아오기, 들어갈 때 생성(화면이 검을 때)·지난 맵을 안에 있는 동안 유지하고 나오면 버리기만 확인했다. 맵 생성 동안 호스트 화면이 멈칫하는지(다른 플레이어가 들어갈 때 호스트 화면은 가려지지 않는다). 클라이언트가 들어가는지, 실제 전투 승리 뒤 들어가는지, 맵 가장자리를 걸어서 막히는지.
- 빌드한 실행 파일에서의 전체 흐름.

## 남은 작업

1. `main`과 `dev`에 브랜치 보호를 설정한다. `main`은 첫 출시 준비 때 `release/` 브랜치를 병합해 채운다.
2. 역할별 남은 기능
   - 대장간: 조립·활 몸체 휘기·활 깎기·시위 걸기·시험 사격 작업대 직접 확인(미니게임 끝까지, 장비 완성), 단조·담금질을 최신 프로토타입과 대조, 장신구에 다른 보석 섞는 규칙(처음 넣은 보석이 효과), 미스릴 장비 아이템, 대장간 업그레이드와 해금, 실제 효과음.
   - 전투: 시체 아트, 교전 중 유닛끼리 겹침, 몬스터 아트와 수치, 보스 등장 때 카메라, 장비별 이펙트, 티어별 장비 능력치, 전투 도중 들어온 참가자 재생 맞추기, 부상·사망과 부서진 목걸이를 관리 목록에 반영, 사망 시 장비 제거, 공용 창고가 가득 찼을 때 보상 처리.
   - 광장: 부상·사망 반영 API, 초상화([고도화]).
   - 인간형 모델: 손에 든 물건을 `ItemSocket_Hold`에 얹기, 미니게임별 작업 동작, 갑옷·장신구 모델, 걷기·공격·쓰러짐 화면 확인.
   - 인벤토리: 재접속한 플레이어의 슬롯 이어 주기, 손에 든 물건 모델·아이콘, 투구·갑옷·장신구 벗기기 기능 제거(교체만), 슬롯이 가득 찼을 때 제작 결과 처리, 파견 뒤 장비 반환.
   - 대화: 튜토리얼·클리어 컷씬에서 `SharedDialogue` 사용, 명성 등 조건 값, `Game` 씬 배치 확인.
   - 공통: `ForgeTest`에 던전 지도 연결(대장간 담당), 밤 연출과 `DaySceneBattleCover`(임시)를 합칠지, 저장 구조(`DungeonProgressState` 포함)와 `GameState` 스폰 시점, 장비 보정과 직업별 공격 방식(`AdventurerSetupRules`, `AdventurerBattle`).
3. 데모 1을 위해 2026-11-10 기획(`docs/GAME_CONCEPT.md`)과 지금 구현이 다른 부분을 맞춘다. 담당자는 팀에서 정한다(기획 13절).
   - 제거: 전투 보상의 재료·보석 드랍(`BattleDropRules`, 결과창 재료 표시, 드랍 연출), 치료비 회복.
   - 변경: 재료 한 칸 최대 32개(지금 99), 1티어 광석 이름 청동(데이터는 구리), 부산물 이름(짐승 가죽 → 가죽, 질긴 힘줄 → 힘줄, 마석 조각 → 마석)과 뼈·목재 추가, 부상 상태를 부상수치로, 에메랄드 효과 대상.
   - 추가: 던전 채집(절차적 맵, 자원 스폰, 묻힌 보물 상자, 어두운 던전 시야), 채집 도구 제작, 상점(가방, 등불, 보물 탐지기, 포션, 재료 거래), 제작 의뢰 게시판, 여관, 친밀도, 장비 내구도와 녹이기, 개인 인벤토리 창(`I` 키), 방어구 판금·사슬·가죽과 공격 속도 배율.
4. 가격 등 다른 `Core` 규칙에도 시드 결정성 EditMode 테스트를 추가한다.
5. UI용 한글 폰트를 확정하고 기존 UI에 적용한다. 주아(`Jua`)와 고운바탕(`GowunBatang`)은 임시 폰트다(SIL OFL 1.1, `05.Art/Fonts`).
6. Unity CLI 사용이 안정되면 예비로 둔 Unity MCP 연동을 정리한다.
7. CI/CD를 구성한다.
