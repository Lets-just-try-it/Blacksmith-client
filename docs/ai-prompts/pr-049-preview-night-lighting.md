# PR #49: fix: 밤에도 장비 착용 창 모험가 미리보기를 낮 밝기로 표시

## 개요

- 브랜치: `fix/preview-night-lighting`
- 대상 브랜치: `dev`
- PR: #49
- 작성일: 2026-11-06
- 담당 영역: 게임플레이·파견
- 작업 목적: 밤 조명 때문에 파견 창·장비 착용 창의 모험가 미리보기가 어둡게 보이는 버그를 고친다.

## 주요 요청

1. 밤이 되어 조명이 어두워지면 파견 창에서 보는 모험가 미리보기도 어두워지는 문제를 고친다.
2. 원인을 먼저 확인하고, 미리보기는 밤에도 낮과 같은 밝기로 보이게 한다. 낮 씬의 밤 조명 연출은 그대로 둔다.
3. 영역 밖 파일을 고쳐야 하면 고치기 전에 확인한다.

## AI와 논의한 내용

### 원인

모험가 툰 셰이더(`HumanoidPaletteToon.hlsl`)가 씬의 주 광원(`GetMainLight`)과 전역 환경광(`SampleSH`)을 곱한다. 밤에 `Forge/DayNightLighting`이 해 세기·색과 환경광을 낮추므로 같은 씬의 화면 밖 미리보기 무대도 어두워진다. 무대의 `PreviewLight`는 포인트 광이라 이 셰이더에 영향을 주지 않았다.

### 고치는 방법

- 레이어·광원 컬링 마스크로 나누는 방법은 셰이더가 주 광원 값을 그대로 읽어 효과가 없어 택하지 않았다.
- `DayNightLighting`(대장간 영역)을 고치지 않고, 미리보기 카메라가 그리는 동안만(`RenderPipelineManager.beginCameraRendering`/`endCameraRendering`) 무대 방향광을 `RenderSettings.sun`으로, 환경광을 낮 색으로 바꿨다가 되돌리는 방법을 택했다.

## 주요 결정

- 사용자가 인벤토리 영역 파일(`Inventory/AdventurerPreview.cs`, `AdventurerPreviewStage` 프리팹) 수정을 승인했다.
- 미리보기 방향광은 낮 해와 같은 색·세기·각도(50/-30)로, 환경광은 낮 씬의 Flat 환경광(0.45, 0.42, 0.4)으로 맞췄다. 기본은 꺼 두고 미리보기를 그릴 때만 켠다.
- 프리팹이 아닌 자체 무대를 가진 `InventoryTest` 씬에도 같은 변경을 적용했다.
- 기획 변경은 없다.

## 변경된 주요 파일

- `Assets/02.Scripts/Inventory/AdventurerPreview.cs`
- `Assets/04.Prefabs/UI/Inventory/AdventurerPreviewStage.prefab`
- `Assets/01.Scenes/InventoryTest.unity`
- `docs/PROJECT_PROGRESS.md`

## 검증 내용

- `ForgeTest` 플레이 모드에서 낮을 끝내 밤(20:00)이 된 뒤, 낮 씬은 어두운 채로 장비 착용 창 미리보기가 낮과 같은 밝기로 보이는 것을 화면으로 확인했다.
- 미리보기 화면의 모델 평균 밝기: 낮 0.43, 밤 0.37~0.44, 고치기 전 밤 0.13.
- 미리보기를 그린 뒤 `RenderSettings.sun`과 환경광이 원래 값으로 돌아오고 미리보기 방향광이 꺼져 있는 것을 확인했다. 콘솔 오류 없음.

## AI 활용 범위

AI가 원인 분석, 스크립트 수정, Unity CLI로 프리팹·씬 수정, 플레이 모드 검증, 진행 문서 갱신, 커밋·PR 작성을 했다. 사용자는 영역 밖 파일 수정, 커밋, PR 생성을 확인하고 승인했다.

## 후속 작업

- 두 명 접속 상태와 `Game` 씬에서의 밤 미리보기는 화면으로 확인하지 않았다.
- 인벤토리 담당자에게 미리보기 조명 변경을 공유한다.
