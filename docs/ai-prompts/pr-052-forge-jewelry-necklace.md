# PR #52: feat: 장신구대 목걸이 제작 미니게임과 목걸이 레시피 추가

## 개요

- 브랜치: `feat/forge-jewelry`
- 대상 브랜치: `dev`
- PR: #52
- 작성일: 2026-11-06
- 담당 영역: 게임플레이·제작
- 작업 목적: 프로토타입의 목걸이 제작 미니게임을 이식하고, 공용 제작 목록에 목걸이 레시피를 올려 장신구대에서 만들 수 있게 한다.

## 주요 요청

1. 판 성형에 이어 장신구 작업대도 진행한다.
2. 판 성형에서 만든 제작 목록·재료 예약 흐름을 그대로 쓴다.
3. 대장간 테스트 씬과 게임 씬에 같이 반영한다.

## AI와 논의한 내용

- 목걸이 미니게임을 사슬 잇기·자르기·보석 박기·광내기 4단계 시드 기반 규칙(Core)과 화면·입력(Forge)으로 나눠 이식했다.
- 보석 1/3/5개를 쓰는 1~3티어 레시피를 보석 4종 12개로 만들었다. 항목이 12개라 제작 목록 창을 스크롤로 바꿨다.
- 새 작업대를 프리팹에 넣으면 Game·ForgeTest 씬을 다시 저장해야 Fusion 오버라이드가 반영된다는 점을 확인했다.

## 주요 결정

- 점수 비율은 사슬 0.3·자르기 0.3·보석 박기 0.15·광내기 0.25([추정], 데이터로 조정 가능).
- 보석 종류를 섞은 목걸이는 이번에 미포함.
- 기획 변경: `GAME_CONCEPT.md`에 '장신구 제작' 절 추가.

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Forge/NecklaceGame.cs`, `NecklaceRules.cs`, `NecklaceShape.cs`, `MiniInput.cs`, `MiniEvent.cs`, `CraftingRules.cs`
- `Assets/02.Scripts/Forge/JewelryStation.cs`, `NecklaceBoardView.cs`, `NecklaceView.cs`, `NecklaceRulesData.cs`, `MinigameKeys.cs`, `CraftingMenuView.cs`
- 목걸이 레시피 12종, `NecklaceRules.asset`, `ForgeRoom.prefab`, `Game.unity`, `ForgeTest.unity`
- `docs/GAME_CONCEPT.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- EditMode 테스트 363개 통과(목걸이 29개 포함).
- `Game` 씬 호스트 플레이에서 열기 → 예약 → 4단계 미니게임 → 결과(정확도 0.95) → 재료 소모를 확인했다. 입력은 같은 호출을 스크립트로 불렀다.
- 확인하지 못한 것: 실제 마우스·키보드 조작, 2인 접속 점유 거절, 보석 종류별 실제 제작.

## AI 활용 범위

AI가 프로토타입 이식, 규칙·화면 구현, 테스트, 문서·씬 설정을 맡았다. 작업 범위와 기획 변경은 사용자가 정했고, 실제 조작 확인은 사용자가 한다.

## 후속 작업

- 보석 종류를 섞은 목걸이 규칙, 조립 작업대 제작 목록 적용, 2인 접속 확인.
