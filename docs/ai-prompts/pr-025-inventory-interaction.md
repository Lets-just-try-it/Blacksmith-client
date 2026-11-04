# PR #25: feat: 인벤토리 스택 나누기·빠른 이동과 손에 든 물건 표시 추가

## 개요

- 브랜치: `feat/inventory-interaction`
- 대상 브랜치: `dev`
- PR: #25
- 작성일: 2026-11-04
- 담당 영역: 인벤토리
- 작업 목적: 인벤토리 조작 편의 기능과 손에 든 물건 표시 추가

## 주요 요청

1. 드래그 중인 아이템이 인벤토리 UI 뒤로 가는 문제를 고칠 것
2. 쌓인 물건을 원하는 개수만큼 나눌 수 있게 할 것
3. 손에 든 물건이 있는 칸을 강조하고, 캐릭터가 손에 든 물건 자체도 강조할 것
4. 왼쪽 Shift나 더블 클릭만으로 내 인벤토리 ↔ 공용 인벤토리 사이를 옮길 수 있게 할 것
5. 대화 중에는 숫자키 슬롯 선택을 무시할 것

## AI와 논의한 내용

### 손에 든 슬롯의 위치

처음에는 기능 브랜치(`feat/shared-inventory`)에서 확인해서, 손에 든 슬롯이 아직 없다고 안내했다. 사용자가 `dev`로 바꾼 뒤 다시 확인하니 PR #24에 숫자키 선택(`HeldSlotSelector`)이 있었다. 다만 고른 슬롯을 화면에 표시하는 코드는 없었다.

### "손에 든 아이템 자체의 강조"의 뜻

AI가 세 가지 해석을 물었다.
- 칸 안 아이콘 강조
- 화면 위에 든 물건 따로 표시
- 캐릭터 손의 월드 물건

사용자는 세 번째를 골랐다. 캐릭터가 물건을 든 모습이 아직 없으므로 표시 기능부터 만들기로 했다.

### 다른 참가자에게 보이기

캐릭터가 든 물건을 다른 참가자도 보려면 손에 든 슬롯 번호가 호스트에 있어야 한다. 그래서 다음처럼 나누었다.
- 로컬 값(`InventoryChannel.HeldSlot`)은 그대로 둔다.
- 고를 때마다 RPC로 호스트의 `[Networked]` 값에 알린다.
- 내 화면은 로컬 값을 써서 지연이 없다.
- 제작 요청 흐름은 바꾸지 않는다.

### 드래그 아이콘이 가려지는 원인

씬을 조사해 보니 하단 슬롯 캔버스의 정렬 순서가 -1, 공용 창고 창이 0이었다. 그래서 하단 슬롯에서 끈 아이콘이 창고 창 뒤에 그려졌다. 아이콘에 정렬 순서를 덮어쓰는 캔버스를 두어 고쳤다.

## 주요 결정

- 스택 나누기는 Ctrl+드래그로 하고, 놓으면 개수 입력 창이 뜬다(기본값 절반). AI가 세 가지 방식을 제안했고 사용자가 추천안을 골랐다.
- 빠른 이동은 공용 창고가 열려 있을 때만 동작한다. 들어가는 만큼만 옮기고 나머지는 원래 칸에 남긴다.
- 숫자키 슬롯 선택은 대화뿐 아니라 캐릭터 조작이 잠긴 동안 모두 무시한다(`GAME_CONCEPT.md` 13절 15번 확정).
- 캐릭터가 든 물건은 처음에 카메라 기준 위치로 구현했다. 사용자가 "플레이어 앞쪽에 두고, 뒤돌았을 때 가려지는 것은 상관없음"으로 정해 캐릭터 기준 앞쪽으로 바꿨다.
- 손에 든 슬롯 번호를 호스트에 동기화한다. PR #24의 "내 PC에서만 정함"을 바꾸는 기획 변경이다.

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Inventory/InventoryGrid.cs`
- `Assets/02.Scripts/Inventory/InventoryChannel.cs`
- `Assets/02.Scripts/Inventory/InventoryPlayerActions.cs`
- `Assets/02.Scripts/Inventory/InventoryView.cs`
- `Assets/02.Scripts/Inventory/InventorySlotView.cs`
- `Assets/02.Scripts/Inventory/InventorySplitPopup.cs`
- `Assets/02.Scripts/Inventory/HeldItemView.cs`
- `Assets/02.Scripts/Inventory/HeldSlotSelector.cs`
- `Assets/02.Scripts/Inventory/PlayerInventoryState.cs`
- `Assets/04.Prefabs/Network/Player.prefab`
- `Assets/04.Prefabs/UI/Inventory/InventorySplitPopup.prefab`
- `docs/GAME_CONCEPT.md`, `docs/NETWORK_DESIGN.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- EditMode 테스트 219개 통과. 나누기·빠른 이동 규칙 테스트 6개를 추가했다.
- `InventoryTest` 플레이 모드에서 화면 함수와 채널을 직접 불러 다음을 확인했다. 콘솔 오류 없음.
  - 빠른 이동
  - 나누기 창의 기본값과 입력 잠금
  - 하단 슬롯 강조와 호스트 `HeldSlot` 동기화
  - 캐릭터 앞 물건 표시
  - 끄는 아이콘이 창고 창 위에 그려지는 것
- 화면 캡처로 결과를 확인했다. 카메라 기준 위치일 때 물건이 몸에 가려지는 문제를 발견해 위치를 바꿨다.
- 실제 마우스·키보드 조작, 두 명 접속, 캐릭터가 카메라를 향한 모습은 확인하지 못했다.

## AI 활용 범위

AI가 한 일:
- 기존 인벤토리·손에 든 슬롯 코드와 씬 구조 조사
- 구조 제안, 코드·테스트 작성
- Unity CLI로 프리팹·씬·머티리얼 구성
- 플레이 모드 확인, 문서 갱신

사용자가 정한 일:
- 스택 나누기 방식
- 손에 든 물건 강조의 범위(월드 표시)와 위치(캐릭터 앞쪽)
- 잠금 중 슬롯 선택 무시 반영

## 후속 작업

- 실제 마우스로 나누기·빠른 이동 확인, 두 명 접속에서 다른 참가자가 든 물건 확인
- 손에 든 물건의 실제 모델·아이콘
- 대장간 담당자와 손에 든 슬롯 동기화 방식 공유
