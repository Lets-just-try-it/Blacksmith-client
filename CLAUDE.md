# CLAUDE.md

# Tempered Heroes (가제)

이 프로젝트에서 작업하는 AI 코딩 에이전트를 위한 공통 지침이다.

## 프로젝트

- Unity 6 (6000.3.24f1, 팀 전원 같은 버전), URP, Input System(새 입력 시스템), Photon Fusion 2 Host 모드
- 2.5D 쿼터뷰 경영/육성 시뮬레이션 RPG. 내가 제작한 장비로 모험가를 파견시켜 마왕을 무찌른다.
- 출시를 목적으로 한 LAUNCH:ER 프로젝트다. 우선순위: 동작하는 코드 > 가독성 > 유지보수성 > 단순성. 과도하게 설계하지 않는다.

## 용어

- "모험가"로 통일한다("용사" 금지). "대장간"으로 통일한다("건물" 금지). 문서, 코드, 주석, UI 문자열 모두 해당한다.
- 루프 단계 이름(낮, 밤 등)은 `docs/GAME_CONCEPT.md` 2절의 표기를 쓴다.

## 문서 읽기

작업마다 필요한 문서의 필요한 부분만 읽는다. 문서를 처음부터 끝까지 읽지 않는다.

| 문서 | 언제 | 읽는 법 |
| --- | --- | --- |
| `docs/GAME_CONCEPT.md` | 기획을 따르는 작업 | 맨 위 절 목록과 "영역 연결"을 보고 해당 절만 읽는다 |
| `docs/PROJECT_PROGRESS.md` | 작업 시작 전 상태 확인, PR 전 갱신 | 맡은 시스템의 절과 "남은 작업"만 읽는다 |
| `docs/NETWORK_DESIGN.md` | 공유 상태, RPC, 입력, 씬 전환을 건드릴 때 | 해당 절만 읽는다 |
| `docs/SETUP.md` | 환경 설정, Unity CLI 연결 문제 | 필요할 때만 |
| `GIT_WORKFLOW.md`, `docs/ai-prompts/README.md` | 커밋·PR을 만들 때 | 전체 |

## 폴더 구조

```text
Assets/
  01.Scenes/            # Bootstrap, Lobby, Game (+ 개발용 테스트 씬)
  02.Scripts/
    Core/               # 네트워크와 Unity에 의존하지 않는 순수 C# 규칙
    Network/            # NetworkRunner 시작, 플레이어 스폰, 공용 GameState
    Forge/              # 대장간: 제작, 업그레이드
    Plaza/              # 광장: 모험가 영입, 생성, 파견
    Battle/             # 자동 전투, 몬스터, 던전
    Inventory/          # 개인·공용 인벤토리, 손에 든 슬롯, 아이템 정의
    Dialogue/           # 영역 공통 대화
    Characters/         # 영역 공통 인간형 캐릭터(외형, 걷기 동작)
    UI/ Utils/
    Editor/             # 에디터 전용 (빌드 제외)
  03.ScriptableObjects/ # Balance(밸런스 수치), Equipment, Items, Adventurers, Monsters, Dungeons, Dialogues, Characters, Runtime(런타임 연결 에셋, 밸런스 아님)
  04.Prefabs/           # Network, Characters, Equipment, Town, UI, Effects
  05.Art/               # Models(FBX), Sprites, Animations, Materials, Shaders, Fonts(라이선스 포함)
  06.Audio/             # BGM, SFX
  Settings/
  ThirdParty/           # 외부 에셋. 패키지마다 폴더와 출처·라이선스 README.md
```

- 새 파일은 용도에 맞는 폴더에 둔다. 계산 규칙은 `Core/`, 도메인 폴더에는 상태를 가진 `NetworkBehaviour`와 화면·연출 `MonoBehaviour`를 둔다.

## 코딩 스타일

- C#. 클래스·메서드·프로퍼티·공개 멤버는 PascalCase, 비공개 필드는 `_camelCase`.
- 공개 필드보다 `[SerializeField]`. 메서드는 작고 한 가지 목적. `MonoBehaviour` 하나에 책임 하나. 기존 스타일을 따른다.
- 주석은 구현 이유나 명확하지 않은 동작에만 쓴다.
- 최적화는 필요성이 확인된 경우에만 하고, 가독성을 희생하지 않는다.

## Unity 지침

- 우선: 직렬화 필드, 프리팹, 적절한 ScriptableObject, 폴링보다 이벤트, 자주 생성되는 오브젝트의 풀링, 인스펙터 참조, 컴포넌트 참조 캐싱, Input Action.
- 피함: `FindObjectOfType`, `GameObject.Find`, `Resources.Load`, 구 입력 API(`Input.GetKey` 등), 큰 `Update()`, 갓 오브젝트, 매 프레임 불필요한 할당.
- 에디터 작업(씬, GameObject, 컴포넌트, 프리팹)은 Unity CLI(`unity command <그룹> <명령>`)로 한다. `.unity`, `.prefab` YAML을 직접 편집하지 않는다. CLI로 연결되지 않으면 사용자에게 알리고(`docs/SETUP.md`), Unity MCP는 예비로만 쓴다.

## 아키텍처

결합도를 낮게 유지하고, 요청받지 않으면 복잡한 디자인 패턴을 들이지 않는다.

### 게임 루프

- 코어 루프 (낮 동안 반복): 전날 밤 토벌한 던전에서 재료를 채집하고, 미니게임으로 장비를 제작하며, 정확도에 따라 장비 등급이 정해진다. 만든 장비는 모험가에게 입히거나 의뢰에 납품하거나 상점에 판다.
- 콘텐츠 루프 (하루 단위): 낮에 채집·제작·의뢰·영입·여관 관리 → 밤에 파견·자동 전투·결과창 → 번 돈으로 대장간 업그레이드와 해금 → 다음 날
- 모험가 슬롯은 최대 8명, 한 번에 파견은 최대 5명(인원은 플레이어가 고름).
- 루프의 원본은 `docs/GAME_CONCEPT.md` 2절이다. 루프를 바꿀 때는 그 문서를 고친다.

### 네트워크 구조

Host 모드(`GameMode.Host`, `GameMode.Client`)로만 동작하고 `GameMode.Single`은 쓰지 않는다. 혼자 할 때도 참가자 없는 호스트로 시작한다. 소규모 협동 게임이므로 치팅 방지와 서버 측 판정은 구현하지 않고, 상태 정합성을 우선한다. 세부 설계는 `docs/NETWORK_DESIGN.md`를 따른다.

1. 게임 상태는 호스트 소유 `NetworkBehaviour`의 `[Networked]` 프로퍼티에만 둔다.
2. 클라이언트 행동은 `RpcSources.InputAuthority` → `RpcTargets.StateAuthority` RPC로 요청한다. RPC는 Player 프리팹의 영역별 행동 컴포넌트(예: `Forge/ForgePlayerActions`)에 두고, `NetworkPlayer`에는 플레이어 자체에 관한 요청만 둔다. 호스트는 요청자, 세션, 중복 여부만 확인하고 상태를 바꾼다.
3. 타이머는 `TickTimer`나 틱 기반으로 계산한다.
4. 공유 상태를 바꾸는 로직은 `Update()`가 아니라 `FixedUpdateNetwork()`에 둔다.
5. 경제 계산과 정확도 → 등급 계산은 `Core/`의 순수 C#으로 작성한다.
6. 표시 전용 처리(UI, 이펙트)는 `Render()`나 `ChangeDetector`로 한다.
7. 캐릭터 이동은 클라이언트 예측을 쓴다. 입력은 `NetworkInput`으로 모아 `FixedUpdateNetwork()`에서 `GetInput()`으로 받는다. 기능 클래스는 입력 장치를 직접 읽지 않는다.
8. 미니게임은 실행하는 클라이언트에서만 로컬로 진행하고, 끝나면 정확도 결과만 호스트에 보낸다. 호스트는 결과를 신뢰한다.
9. 하는 동안 캐릭터 조작을 멈춰야 하는 기능은 `PlayerNetworkInputLock`의 `Lock(this)`/`Release(this)`를 쓴다. 입력 액션을 직접 끄거나 `PlayerInputCollector`에 잠금 속성을 추가하지 않는다.

### 구현 원칙

테스트와 호스트 권한 구조를 위한 원칙이다. 바꿔야 하면 먼저 사용자에게 확인한다.

- `Core/` 규칙은 `UnityEngine`과 Fusion을 참조하지 않는다.
- 무작위 규칙(미니게임, 모험가 생성, 자동 전투 등)은 시드를 받아 `System.Random(seed)`로 계산한다. 같은 시드와 입력이면 같은 결과가 나온다. `UnityEngine.Random`, `Time` 같은 비결정 요소를 쓰지 않는다.
- 밸런스 수치(길이, 확률, 슬롯 수, 가격, 업그레이드 효과)는 코드 상수가 아니라 ScriptableObject 데이터로 둔다.
- 화면의 예측 정보와 실제 계산은 같은 함수를 쓴다.

## 역할 경계

영역: 1. 대장간(`Forge/`) 2. 광장(`Plaza/`) 3. 전투(`Battle/`) 4. 인벤토리(`Inventory/`). 범위는 `docs/GAME_CONCEPT.md` 13절.

요청받은 작업의 영역 밖 파일, 여러 영역이 쓰는 `Core/` 규칙, `Network/`의 공개 API를 바꿀 때는 먼저 사용자에게 확인한다.

## 기획 문서

세부 기획과 수치는 `docs/GAME_CONCEPT.md`를 따른다. 항목 표시:

- `[담당자 결정]`, `[미정]`: 임의로 정하지 않고 사용자에게 묻는다.
- `[추정]`: 구현하되 수치와 구조를 데이터로 빼서 바꾸기 쉽게 둔다.
- `[고도화]`: 데모 1 이후 작업이다. 요청받지 않으면 구현하지 않는다.

기획을 정하거나 바꿀 때:

- 같은 PR에서 `docs/GAME_CONCEPT.md`의 해당 항목을 갱신하고 표시를 정리한다. 바뀐 이력이 아니라 지금 기획만 적는다.
- PR 본문에 "기획 변경" 항목을 둔다.
- 병합 전에 팀에 공유하고, 다른 영역에 영향이 있으면 그 담당자의 동의를 받는다. 이런 PR이면 사용자에게 팀 공유가 필요하다고 알린다.

## 작업 방식

- 코드를 쓰기 전에 관련 스크립트를 읽고, 현재 구조를 이해하고, 계획을 설명한다. 기존 코드를 재사용하고, 큰 구조 변경은 사용자에게 확인한다.
- 필요한 최소 범위만 바꾼다. 관련 없는 코드를 다시 쓰거나 다시 포맷하지 않고, 기존 명명과 구조를 유지한다.
- 응답 순서: 계획 → 구현 → 변경 요약 → 적용한 가정.

## Git 작업 흐름

`GIT_WORKFLOW.md`를 따른다. `main`과 `dev`에 직접 커밋하지 않는다.

- `dev`에서 새 브랜치를 만든다(`release/`, `hotfix/`는 사용자가 요청할 때만). 컨벤셔널 커밋으로 커밋하고, 푸시하고, `dev` 대상 PR을 만든다.
- 커밋 전에 변경 파일, 변경 요약, 커밋 메시지를 보여 주고 승인받는다. PR 전에 제목과 본문을 보여 주고 승인받는다. 승인은 그 커밋이나 PR에만 적용한다.
- 커밋 메시지에 Co-Authored-By 등 공동 작성자 정보를 넣지 않는다.
- 커밋 메시지, PR 제목, PR 본문에 Claude, AI 또는 이를 연상시키는 표현(예: 생성 도구 표시)을 넣지 않는다. 파일·규칙 이름(`CLAUDE.md`, AI 협업 기록)은 예외다. 목적과 내용만 간결하게 쓴다.

## AI 협업 기록

AI의 도움을 받은 모든 PR에는 AI 협업 기록을 정확히 한 개 `docs/ai-prompts/pr-{번호}-{기능명}.md`로 넣는다.

1. PR 전에 `docs/ai-prompts/README.md`를 읽는다.
2. 주요 요청, 논의, 결정, AI 활용 범위와 검증을 요약한다. 전체 대화나 도구 기록을 복사하지 않고, 인증 정보·토큰·개인 정보를 넣지 않는다.
3. 저장 전에 내용을 사용자에게 보여 주고 승인받는다.

## 프로젝트 진행 문서

모든 PR에서 `docs/PROJECT_PROGRESS.md`에 영향이 있는지 확인한다. 기능·시스템, 게임 흐름·UI·조작, 확인 상태, 남은 작업이 바뀌면 같은 PR에서 갱신한다.

- 시스템별 상태, 핵심 진입점, 확인하지 않은 것, 남은 일만 짧게 적는다. 구현 방법과 확인 기록은 PR 본문과 AI 협업 기록에 적는다.
- 개발 환경·빌드·배포 변경은 `docs/SETUP.md`에 적는다.
- 기준일을 맞추고, 기준 커밋 해시는 적지 않는다(병합 커밋을 미리 알 수 없다).
