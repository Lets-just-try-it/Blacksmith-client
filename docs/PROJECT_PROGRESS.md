# Tempered Heroes (가제) 프로젝트 진행 현황

> 기준일: 2026-11-02
>
> 이 문서는 자신이 들어 있는 브랜치의 상태를 설명한다. 정확한 커밋은 `git log -1 -- docs/PROJECT_PROGRESS.md`로 확인한다.

## 프로젝트 요약

Tempered Heroes는 Unity 6 URP 기반의 2.5D 쿼터뷰 대장간 경영·모험가 파견 RPG다. 플레이어는 낮에 대장간에서 장비를 직접 제작해 판매하고 모험가를 영입한다. 밤에는 제작한 장비를 입힌 모험가를 던전으로 파견해 자동 전투 결과를 확인한다. 최종 목표는 모험가를 키워 마왕을 토벌하는 것이다.

대장간에서 관리할 수 있는 모험가는 대장간 업그레이드로 슬롯이 1 → 2 → 4 → 6 → 8칸으로 늘어 최대 8명이다. 한 번에 파견할 수 있는 모험가는 최대 5명이다. 슬롯이 5칸보다 많으면 관리 중인 모험가 중에서 파견할 모험가를 골라 장비를 입혀 보낸다. 5명을 꼭 채워 보낼 필요는 없고, 몇 명을 보낼지는 파견할 때 플레이어가 정한다.

현재 저장소에는 프로젝트 공통 문서(`CLAUDE.md`, `GIT_WORKFLOW.md`, 이 문서, `docs/GAME_CONCEPT.md`, `docs/NETWORK_DESIGN.md`, `docs/ai-prompts/README.md`), URP 템플릿으로 생성한 Unity 프로젝트, Photon Fusion 2 SDK와 Host 모드 네트워크 뼈대, 진행 중인 자동 전투 코드, 모험가 영입 기반 코드가 있다.

## 개발 및 배포 환경

- Unity `6000.3.24f1`
- Universal Render Pipeline `17.3.0`
- Input System `1.20.0`
- Test Framework `1.6.0`
- 네트워크: Photon Fusion 2.1.3, Host 모드
- 플랫폼: PC (Windows/Mac)
- 배포: 스토브 우선 출시 → 스팀
- 브랜치 전략: Git Flow (`main`, `dev`, 작업 브랜치, `release/`, `hotfix/`. `GIT_WORKFLOW.md` 참고)
- 빌드 씬 순서: `Bootstrap` → `Lobby` → `Game` (`SampleScene`은 템플릿 기본값으로 뒤에 남아 있다. 그 뒤의 `PlazaTest`는 개발용 테스트 씬이다)
- TODO: CI/CD 구성

### Photon 설정 (로컬 최초 1회)

App ID가 담긴 `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`은 공개 저장소에 올리지 않도록 `.gitignore`로 제외한다. 저장소를 처음 받은 뒤 각자 한 번 설정한다.

1. Unity 에디터에서 프로젝트를 열면 Fusion이 빈 `PhotonAppSettings.asset`을 만든다. 만들어지지 않았으면 `Tools > Fusion > Realtime Settings`를 연다.
2. `Tools > Fusion > Realtime Settings`에서 `App Id Fusion`과 `Fixed Region`을 입력한다.
3. App ID와 지역 값은 팀 비공개 채널에서 받는다. 지역이 다르면 같은 세션에 들어가지 못할 수 있으므로 팀 전원이 같은 값을 쓴다.

- App ID를 문서, 커밋, PR에 적지 않는다.
- SDK 임포트로 Player Settings의 `Allow 'unsafe' Code`와 `Run In Background`가 켜지고 Fusion 정의 심볼이 추가되었다. Fusion이 요구하는 설정이므로 끄지 않는다.

### 멀티플레이 실행과 주의 사항

- `Bootstrap` 씬에서 플레이를 시작한다. 로비에서 세션명을 입력하고 `Host`로 방을 열거나 `Join`으로 참가한다. 혼자 할 때도 `Host`로 시작한다.
- 두 명 테스트는 빌드한 실행 파일과 에디터를 함께 띄워 같은 세션명으로 접속한다.
- 로비 UI 문구는 한글 폰트를 UI에 적용할 때까지 영어로 둔다. 한글 폰트 에셋은 `05.Art/Fonts`에 임시로 추가했다.
- `NetworkObject`가 붙은 프리팹을 새로 만든 뒤 스폰할 때 "has been baked with a guid ... failed to be translated into a prefab" 오류가 나면 `Tools > Fusion > Rebuild Prefab Table`을 실행한다.
- 씬에 둔 `NetworkObject`는 씬을 저장할 때 Fusion이 베이크한다. 에디터 스크립트나 CLI로 씬을 만들었다면 `Tools > Fusion > Scene > Bake Scene Objects`를 실행하고 다시 저장한다. 베이크되지 않은 씬 오브젝트는 런타임에 등록되지 않는다.

### AI 에이전트 연동

AI 코딩 에이전트(Claude Code)가 Unity 에디터에 연결할 수 있도록 다음 패키지를 팀 공용으로 포함한다.

| 패키지 | 역할 | 상태 |
| --- | --- | --- |
| Unity Pipeline `0.9.1-exp.1` (`com.unity.pipeline`) | Unity CLI가 에디터나 실행 중인 게임에 연결해 씬·GameObject·프리팹 조작, 빌드, 테스트, 플레이/정지를 수행한다. | 사용 중 |
| Unity AI Assistant `2.21.0-pre.1` (`com.unity.ai.assistant`), AI Inference `2.6.1` (`com.unity.ai.inference`) | Unity MCP 서버. Claude Code가 relay(`~/.unity/relay/`)를 통해 에디터에 연결한다. | 예비 |

- 에디터 작업(씬 조작 등)은 Unity CLI로 한다. Unity MCP는 CLI에 문제가 생겼을 때만 사용한다.
- Unity MCP 서버는 Unity에서 지원 중단(deprecated) 예정이며, 대체 수단으로 Unity CLI가 안내되어 있다. CLI 사용이 안정되면 MCP 등록과 `ai.assistant` 패키지 유지 여부를 정리한다.
- 세 패키지와 CLI 모두 실험·프리릴리스·beta 버전이다. 업데이트 후 에디터 오류가 생기면 이 패키지들을 먼저 확인하고, 버전을 바꿀 때는 팀 전원이 같은 버전으로 맞춘다.

#### Unity CLI 설치와 버전 맞추기

- 팀 기준 CLI 버전: `1.0.0-beta.14`
- 설치 (Windows PowerShell):

  ```powershell
  $env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex
  ```

- 이미 설치했다면 `unity --version`으로 버전을 확인하고, 기준 버전보다 낮으면 `unity self-update`로 올린다.
- 연결 확인: Unity 에디터에서 이 프로젝트를 연 상태로 `unity command editor status`를 실행해 `status`가 `ready`이고 `projectPath`가 이 저장소인지 확인한다.
- 기준 버전을 올릴 때는 팀에 공유하고 이 문서의 기준 버전도 함께 갱신한다.
- 명령이 멈추거나 느리면 Unity 에디터 창이 백그라운드에서 갱신을 늦추는지 확인한다.

## 목표 게임 흐름 (MVP)

낮에는 장비를 제작해 판매하고 모험가를 영입한다. 밤에는 모험가를 파견해 자동 전투 결과를 확인한다. 코어 루프, 콘텐츠 루프, MVP 범위는 `docs/GAME_CONCEPT.md`를 따른다.

혼자 플레이할 때도 Photon Fusion 2 Host 모드에서 참가자가 없는 호스트로 시작한다. 게임 상태는 호스트가 소유하고, 클라이언트는 RPC로 행동을 요청한다. 세부 규칙은 `CLAUDE.md`의 "네트워크 구조"와 `docs/NETWORK_DESIGN.md`를 따른다.

## 시스템별 구현 상태

| 영역 | 상태 | 구현 내용 |
| --- | --- | --- |
| 장비 제작 미니게임·판매 | 진행 중 | `Core/Forge`에 미니게임 기반(`MiniGame`, `MiniInput`, `MiniEvent`)과 담금질 규칙(`QuenchGame`: 시드 기반 기준 시간, 정확도 계산). 수치는 `QuenchRulesData`(`03.ScriptableObjects/Balance/QuenchRules.asset`). 나머지 미니게임 6종, 입력 연결, 화면, 작업대 사용권·결과 전송, 판매는 남아 있다. |
| 장비 등급 규칙 | 구현 | 미니게임 점수 평균을 5단계 등급으로 변환하는 `GradeRule`(`Core/`)과 경계값·등급 이름 데이터(`GradeThresholds`) |
| 낮·밤 사이클 | 미구현 | |
| 던전·파견·자동 전투 | 진행 중 | `Core/Battle`에 시드 기반 자동 전투 시뮬레이션(마나 자동 스킬, 스테이지 연속 진행, 쓰러짐·사망 판정)과 던전 단계별 스테이지 선택·진행 규칙. `Battle/`에 쿼터뷰 재생, 하단 HP·마나 HUD, 결과창. 모험가 정보(`AdventurerData`)를 전투 유닛으로 바꾸는 `AdventurerSetupRules`(체력·공격력·방어력은 `AdventurerStatRules.GetCurrentStats`)와, 직업별 전투 수치(사거리, 공격 간격, 이동 속도, 마나)·전투 모델과 `SkillId` → `SkillData` 표를 담은 `AdventurerBattle` 데이터. `01.Scenes/BattleTest`에서 광장과 같은 규칙으로 생성한 모험가 파티(레벨 지정)와 테스트 던전으로 확인한다. 생성된 모험가 유닛은 오브젝트 이름이 `이름 Lv레벨 (#ID)`이고, 인스펙터의 `AdventurerUnitInfo`에서 모험가 정보와 실제 전투 능력치를 볼 수 있다. 파견 창, 보상·경험치, 실제 몬스터 데이터, Fusion 연동은 남아 있다. |
| 모험가 영입·랜덤 생성 | 진행 중 | `Core/Adventurers`에 모험가 정보 원본 형식(`AdventurerData`: 이름, 직업, 레벨, 외형 번호, 기본·성장 체력·공격력·방어력, 스킬 번호, 착용 장비 번호, 몸 상태), 시드 기반 생성(`AdventurerGenerator`, 후보 1~4명), 레벨 성장 공식(`AdventurerStatRules`). 수치는 `AdventurerGeneration`·직업(`Job_Warrior`)·`AdventurerRoster` 데이터. `Plaza/`에 호스트 소유 관리 목록(`AdventurerRosterState`, 최대 8), 광장 후보 스폰·영입 처리(`PlazaRecruitment`, `RecruitCandidate`), Player 프리팹의 `PlazaPlayerActions`(E로 영입 요청 RPC), 목록 창 UI, 초상 뼈대(`AdventurerPortraitProvider`). `01.Scenes/PlazaTest`에서 로비 없이 호스트로 시작해 확인한다. 직업은 전사 1종이다. 파견 창, 전투 결과 반영(부상·사망·경험치), 외형 적용, `Game` 씬 배치는 남아 있다. |
| 대장간 업그레이드·해금 | 미구현 | |
| 저장/불러오기 | 미구현 | |
| Photon Fusion 2 연동 (Host 모드) | 진행 중 | Fusion 2.1.3 SDK. `Bootstrap` → `Lobby` → `Game` 씬 흐름, 세션명으로 Host 시작·Client 참가, 세션 종료 시 로비 복귀, 호스트의 플레이어 스폰·디스폰, `NetworkInput` 기반 예측 이동(입력 구조는 `docs/NETWORK_DESIGN.md` 3절), 필드 없는 공용 `GameState`. 임시 캡슐 모델을 쓴다. 클라이언트 요청 RPC는 Player 프리팹에 영역별 행동 컴포넌트(예: `ForgePlayerActions`)로 나눠 두기로 했다. 영역별 공유 상태·RPC, 스팀·스토브 초대는 남아 있다. |
| 방송 연동 | 미구현 | |
| 자동화 테스트 | 일부 | `02.Scripts/Tests/EditMode`에 전투 시뮬레이션 결정성·승패·마나·쓰러짐, 스테이지 선택·진행 테스트 15개, 장비 등급 경계값·평균 테스트 14개, 담금질 규칙 테스트 13개, 모험가 생성 결정성·범위와 레벨 성장 테스트 12개, 모험가 → 전투 유닛 변환 테스트 5개 |

## 검증 상태

- 에디터 컴파일 오류 없음. EditMode 테스트 59개 통과 (`unity test --mode EditMode`).
- `PlazaTest` 씬 플레이 모드에서 호스트 자동 시작, 영입 후보 1~4명 생성, 후보에게 다가가면 "E 영입" 표시, E 입력으로 영입해 후보가 사라지고 목록에 들어가는 것, 슬롯이 가득 차면 영입되지 않는 것, 목록 창을 연 상태에서 바로 갱신되는 것, 버튼으로 창을 열고 닫는 것을 확인했다. 콘솔 오류 없음. 빌드한 실행 파일과 에디터로 두 명이 접속해 영입, 관리 목록 동기화, 클라이언트 화면의 후보 위치를 확인했다. 이때 로비가 `PlazaTest`를 불러오도록 로컬에서만 임시로 바꿔 테스트했다.
- `BattleTest` 씬 플레이 모드에서 스테이지 3개 진행, 승리, 결과창 표시를 확인했다. 진행 상황에 따라 다음 스테이지부터 선택한 스테이지까지만 싸우는 것도 확인했다. 생성한 모험가 4명(1·3·5·8레벨)이 레벨에 맞는 능력치로 싸우고, 결과창에 모험가 이름이 나오는 것을 확인했다. 콘솔 오류 없음.
- Fusion SDK 임포트 후 에디터 컴파일 오류 없음, Fusion IL 위버 동작 확인.
- Host 모드 뼈대: 에디터 플레이 모드에서 `Bootstrap` → `Lobby` → Host 시작 → `Game` 씬 로드, 플레이어 스폰, 키 입력에 따른 쿼터뷰 기준 이동, 세션 종료 후 로비 복귀를 확인했다. 세션명이 비었거나 인증에 실패하면 로비에 실패 문구가 뜨고 다시 시도할 수 있다. 빌드한 실행 파일과 에디터로 두 명이 접속해 이동과 동기화에 이상이 없는 것을 확인했다.
- 기본 TextMesh Pro 폰트에 한글 글리프가 없어 UI 한글이 네모로 보인다. 한글 폰트 에셋(주아, 고운바탕)을 추가해 한글 글리프가 들어가는 것을 확인했지만, 기존 UI에는 아직 적용하지 않았다.
- 외부 에셋 `PolyRonin` 대장간 소품과 `VOiD1` 불꽃 셰이더를 `Assets/ThirdParty`에 들여왔다. 소품 머티리얼은 URP Lit로 바꿔 임시 장면에서 분홍색 없이 표시되는 것을 확인했다. 소품 크기가 제각각이고 아직 어떤 장면에도 쓰지 않았다. 출처와 라이선스는 `Assets/ThirdParty/README.md`에 적었다.
- 빌드 검증은 아직 하지 않았다.

## 남은 우선 작업

1. `main`과 `dev`에 브랜치 보호를 설정한다. `main`은 첫 출시 준비 때 `release/` 브랜치를 병합해 채운다.
2. 플레이어 캐릭터 담당을 팀에서 정한다.
3. 역할별(대장간, 광장, 전투) 기능을 구현하고 MVP 시연으로 피드백을 받는다.
   - 대장간: 나머지 제작 미니게임 6종 이식(정제, 단조, 연마, 판 성형, 조립, 장신구), 미니게임 입력·화면, 작업대 사용권·결과 전송(`Forge/ForgePlayerActions`), 인벤토리, 판매
   - 전투: 파견 창(광장 담당과 협의), 보상·경험치, 실제 몬스터·보스 데이터, 장비별 이펙트, Fusion 연동(시드 동기화, 결과를 `GameState`에 적용). 파견할 때는 관리 목록(`AdventurerRosterState`)의 `AdventurerData`를 `AdventurerBattle.ToSetup`으로 바꿔 쓴다. 장비·직업 보정과 직업별 공격 방식은 정해지면 `AdventurerSetupRules`와 `AdventurerBattle`에 추가한다.
   - 광장: 파견 창, 전투 결과(부상·사망·경험치)를 관리 목록에 반영하는 API, 외형 프리셋 적용, 낮이 시작될 때 후보 재생성, `Game` 씬에 관리 목록·광장 배치
4. 다른 `Core` 규칙(가격)에도 시드 결정성 EditMode 테스트를 추가한다.
5. UI용 한글 폰트를 확정하고 기존 UI에 적용한다. 주아(`Jua`)와 고운바탕(`GowunBatang`)은 확정 전 임시 폰트다. 둘 다 SIL OFL 1.1이며 `05.Art/Fonts`에 라이선스 문서와 함께 둔다.
6. Unity CLI 사용이 안정되면 예비로 둔 Unity MCP 연동을 정리한다.

## 관련 문서

- `CLAUDE.md`: 프로젝트 개발 지침
- `GIT_WORKFLOW.md`: 브랜치, 커밋, PR 규칙
- `docs/GAME_CONCEPT.md`: 게임 컨셉과 세부 기획
- `docs/NETWORK_DESIGN.md`: 멀티플레이 네트워크 설계
- `docs/ai-prompts/README.md`: AI 협업 기록 규칙
