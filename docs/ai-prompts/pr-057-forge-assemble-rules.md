# PR #57: feat: 조립 미니게임 규칙과 밸런스 데이터 추가

## 개요

- 브랜치: `feat/forge-assemble-rules`
- 대상 브랜치: `dev`
- PR: #57
- 작성일: 2026-11-12
- 담당 영역: 게임플레이·제작
- 작업 목적: 방어구 조립 미니게임의 규칙과 밸런스 데이터를 프로토타입에서 옮긴다.

## 주요 요청

1. 조립 작업대 미니게임을 프로토타입에서 이식한다. 규칙과 정확도 계산은 `Core/`의 순수 C#(시드 `System.Random`)으로 쓰고, 수치는 ScriptableObject로 둔다.
2. 규칙, 입력, 화면을 작은 PR로 나눠 올린다. 이 PR은 규칙만 다룬다.

## AI와 논의한 내용

### 프로토타입 기준

- 프로토타입 기준은 오래된 `Scripts.zip`이 아니라 최신 폴더와 스크린샷으로 정했다.

### 프로토타입에서 바꾼 점 (판정은 같고 구조만)

- 이음새 수는 코드에 들어 있던 광석 등급 조건 대신 난이도 필드(`HoleBonus`, `SideHoles`, `OptionalSeams`)로 표현했다. 값은 프로토타입과 같다(투구 2/3/4, 갑옷 3/4/5 이음새).
- 판을 놓는 시점은 프로토타입의 `PointerReleased` 대신 `PointerHeld`가 꺼질 때로 판단한다. 이 프로젝트의 `MiniInput`에 릴리스 필드가 없기 때문이다.

## 주요 결정

- 규칙만 이 PR에 두고, 작업대와 화면은 다음 PR로 나눈다. 그래서 이 PR은 플레이 모드로 확인할 수 없다.
- 프로토타입에 코드 상수로 박혀 있던 값(허용 오차, 링 속도, 판정 폭, 구멍·이음새 수)은 모두 `AssembleRules` 데이터로 뺐다.
- 정확도는 구멍 수로 가중한 이음새 평균이고, 판 성형 점수가 낮으면 최대 8%까지 깎는다.
- `docs/GAME_CONCEPT.md`에 조립 절을 `[추정]`으로 추가했다. 병합 전에 팀에 공유가 필요하다.

## 변경된 주요 파일

- `Assets/02.Scripts/Core/Forge/AssembleGame.cs`, `AssembleRules.cs`, `MiniEvent.cs`
- `Assets/02.Scripts/Forge/AssembleRulesData.cs`
- `Assets/03.ScriptableObjects/Balance/AssembleRules.asset`
- `Assets/02.Scripts/Tests/EditMode/AssembleGameTests.cs`
- `docs/GAME_CONCEPT.md`, `docs/PROJECT_PROGRESS.md`

## 검증 내용

- EditMode 테스트 384개가 통과했다(조립 규칙 테스트 21개 포함). 같은 시드와 같은 입력이면 같은 결과가 나오는 것도 테스트로 확인했다.
- 규칙만 있어서 실제 플레이로는 확인하지 못했다.

## AI 활용 범위

AI가 프로토타입 규칙 이식, 데이터 분리, 테스트 작성, 문서 갱신을 수행했다. PR을 규칙·입력·화면으로 나누는 방침과 `[추정]` 항목 처리는 사용자가 정했다.

## 후속 작업

- 조립 작업대와 화면(입력, 연출) PR
