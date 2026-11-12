# PR #59: feat: 정제 정확도로 주괴 수를 정하고 주괴를 등급 없는 재료로 통일

## 개요

- 브랜치: `feat/forge-smelting-ingot-yield`
- 대상 브랜치: `dev`
- PR: #59
- 작성일: 2026-11-12
- 담당 영역: 게임플레이·제작
- 작업 목적: 정제 한 번의 정확도로 주괴 수를 정하고, 주괴를 등급 없는 재료로 통일한다.

## 주요 요청

1. 광석 하나로 주괴 1~3개 정도가 나오게 한다.
2. 정확도 구간 수치를 조정한다(3개는 85% 이상).
3. 이 수치가 임시이며 나중에 밸런스 테스트로 확정해야 한다는 내용을 문서에 넣는다.

## AI와 논의한 내용

### 광석 하나당 주괴 수

- 주괴 하나에 광석이 몇 개 필요한지는 게임마다 달라 표준이 없다(마인크래프트 1:1, 테라리아 3~4개, 스타듀밸리 5개 등).
- 이 프로젝트는 정제 한 번의 정확도가 주괴 수를 정하는 구조라서 "정제 한 번 = 광석 n개 → 주괴 k개(정확도별)"로 보는 편이 맞다고 정리했다.
- 기획의 소비량(대검 주괴 4개, 투구 2개, 갑옷 4개)을 근거로 계산했고, 사용자가 광석 1개로 1~3개를 제안해 그 방향으로 정했다.

### 정확도 구간

- 정확도 구간의 경계는 AI가 처음 가정했고, 사용자가 3개 구간을 85% 이상으로 조정했다. 가장 높은 정확도(95% 이상)에서는 4개가 드물게 나오게 했다.
- 구간 수치는 임시이며 밸런스 테스트 후 확정한다고 문서에 적었다.

## 주요 결정

- 광석 1개를 정제하면 주괴 1~4개가 나온다(0~39% 1개, 40~84% 2개, 85~94% 3개, 95% 이상 4개). 구간은 `SmeltingRules.asset`의 `YieldTiers`에 둔다.
- 구리·철 주괴를 중간 형태에서 재료로 바꿔 등급과 정확도 없이 쌓이게 한다.
- 정제 점수는 장비 등급에 더하지 않는다. 연마의 처음 날 상태와 판 성형의 띠 폭은 정제 점수 대신 앞 단계 값을 이어받는다.
- 첫날 지급 광석 수는 `[미정]`으로 남긴다.
- `docs/GAME_CONCEPT.md` 4.2 "정제"와 미정 표 16번을 `[추정]`으로 정리했다. 병합 전에 팀에 공유가 필요하다.

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Forge/SmeltingRules.cs`, `CraftingRules.cs`
- `Assets/02.Scripts/Forge/CraftingEntry.cs`, `CraftingMenuView.cs`, `ForgePlayerActions.cs`, `SmeltingRulesData.cs`
- `Assets/03.ScriptableObjects/Balance/SmeltingRules.asset`, `Items/Item_CopperIngot.asset`, `Item_IronIngot.asset`
- `Assets/04.Prefabs/Network/Player.prefab`, `Assets/01.Scenes/ForgeTest.unity`, `Game.unity`
- `Assets/02.Scripts/Tests/EditMode/SmeltingYieldTests.cs`, `CraftingRulesTests.cs`
- `docs/GAME_CONCEPT.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- 정제 수율 테스트와 제작 규칙 테스트를 추가했고 EditMode 테스트가 통과했다.
- 실제 정제 작업대에서 정확도별로 주괴가 나오는 것은 직접 확인하지 못했다.

## AI 활용 범위

AI가 주괴 수 방식 비교와 제안, 정확도 구간 구현, 주괴 재료 통일, 제작 처리, 테스트, 문서 정리를 수행했다. 광석 1개당 주괴 범위와 정확도 구간 조정, 임시 수치 표기 방침은 사용자가 정했다.

## 후속 작업

- 첫날 지급 광석 수 결정
- 정확도 구간의 밸런스 테스트와 확정
