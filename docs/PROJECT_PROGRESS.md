# Tempered Heroes (가제) 프로젝트 진행 현황

> 기준일: 2026-10-30
>
> 기준 브랜치: `dev` (`e75368d`, Unity 프로젝트 초기화 병합)
>
> 기준 main: `origin/main` (`d469a13`, Initial commit)

## 프로젝트 요약

Tempered Heroes는 Unity 6 URP 기반의 2.5D 쿼터뷰 대장간 경영·모험가 파견 RPG다. 플레이어는 낮에 대장간에서 장비를 직접 제작해 판매하고 모험가를 영입한다. 밤에는 제작한 장비를 입힌 모험가를 던전으로 파견해 자동 전투 결과를 확인한다. 최종 목표는 모험가를 키워 마왕을 토벌하는 것이다.

현재 저장소에는 프로젝트 공통 문서(`CLAUDE.md`, `GIT_WORKFLOW.md`, 이 문서, `docs/ai-prompts/README.md`)와 URP 템플릿으로 생성한 빈 Unity 프로젝트가 있다. 프로토타입 스크립트는 아직 저장소에 들어오지 않았다.

## 개발 및 배포 환경

- Unity `6000.3.24f1`
- Universal Render Pipeline `17.3.0`
- Input System `1.20.0`
- Test Framework `1.6.0`
- 네트워크: Photon Fusion 2, Host 모드 (SDK 연동 예정)
- 플랫폼: PC (Windows/Mac)
- 배포: 스토브 우선 출시 → 스팀
- 브랜치 전략: Git Flow (`main`, `dev`, 작업 브랜치, `release/`, `hotfix/`. `GIT_WORKFLOW.md` 참고)
- 빌드 씬 순서: `SampleScene` (템플릿 기본값)
- TODO: CI/CD 구성

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

코어 루프 (낮 10분 동안 반복)

```text
행동 (단조·담금질) → 보상 (성공도에 따른 장비 등급) → 다음 행동 (판매하거나 모험가 몫으로 남기고 다음 장비 선택)
```

콘텐츠 루프 (하루 단위)

```text
한 판 (낮: 제작·판매·영입 → 밤: 파견·자동 전투·결과창)
→ 강화·해금 (대장간 업그레이드, 모험가 슬롯 1 → 2 → 4 → 6 → 8, 장신구 제작대, 층 보스 격파 시 새 층·재료)
→ 다음 판
```

혼자 플레이할 때도 Photon Fusion 2 Host 모드에서 참가자가 없는 호스트로 시작한다. 게임 상태는 호스트가 소유하고, 클라이언트는 RPC로 행동을 요청한다. 세부 규칙은 `CLAUDE.md`의 "네트워크 구조"를 따른다.

## 시스템별 구현 상태

저장소 기준으로는 구현된 시스템이 없다. 아래 상태는 저장소 밖에 있는 프로토타입(`Scripts.zip`, C# 28개 파일)을 기준으로 하며, 저장소에 들인 뒤 다시 확인한다.

| 영역 | 상태 | 구현 내용 |
| --- | --- | --- |
| 마을·대장간 씬, 씬 이동 | 프로토타입 | Town, Forge 두 씬. 포털, 출현 지점, 페이드 |
| 플레이어 이동·상호작용 | 프로토타입 | WASD 화면 기준 이동, 가장 가까운 시설에 E |
| 제작 4단계 미니게임 | 프로토타입 | 정제·단조·담금질·마감. 기획과 다른 부분 있음 |
| 제작 연출 | 프로토타입 | 카메라 포커스, 히트스톱, 흔들림, 파티클, 절차 생성 효과음 |
| 품질·판매 경제 | 일부 | 광석 구매 → 제작 → 판매 한 줄 흐름만 있음 |
| HD-2D 후처리 | 프로토타입 | `Hd2dLook` |
| 픽셀 표현 | 실험 | 화면 픽셀화 `PixelLook`만 있음 |
| HUD | 임시 | IMGUI(`OnGUI`) |
| 낮·밤 사이클 | 미구현 | |
| 던전·파견·자동 전투 | 미구현 | |
| 모험가 영입·랜덤 생성 | 미구현 | |
| 대장간 업그레이드·해금 | 미구현 | |
| 숙련도·장인 슬롯 | 미구현 | |
| 저장/불러오기 | 미구현 | |
| Photon Fusion 2 연동 (Host 모드) | 미구현 | |
| 방송 연동 | 미구현 | |
| 자동화 테스트 | 미구축 | |

## 저장소 반영 시 확인할 구조

- 프로토타입 스크립트는 `CLAUDE.md`의 폴더 구조(`02.Scripts/Core`, `Network`, `Forge` 등)에 맞춰 배치한다.
- `SceneBuilder`는 `UnityEditor`를 사용하므로 `02.Scripts/Editor/`에 둔다.
- `GameBootstrap`과 `SceneBuilder`의 코드 생성 방식은 프리팹·씬 배치 방식으로 옮기기 전까지 임시 예외다.
- 밸런스 수치는 현재 `GameConfig`와 미니게임 클래스의 상수에 있으며, ScriptableObject로 옮겨야 한다.

## 기획과 구현 차이 (결정 필요)

| # | 항목 | 결정할 것 |
| --- | --- | --- |
| 1 | 단조 방식 | 리듬 방식을 채택할지, 기획(위치 선택)대로 바꿀지, 섞을지 |
| 2 | 제작 길이 | 판매용(현재 약 1~1.5분)과 대작(기획 4~6분)을 나눌지 |
| 3 | 정제 구성 | 불·걷기 동시 방식 유지 여부, 광석 조합 추가 시점 |
| 4 | 각인 칸 수 | 품질 계산 순서를 바꿔 칸 수를 품질로 정할지 |
| 5 | 각인 확률 보정 | 보정 폭과 연속 보정 |
| 6 | 품질 등급 | S/A/B/C와 1~3 중 등급 체계 통일 |
| 7 | 담금질 | 길이, 보조 표시 기본값 |
| 8 | 아트 | 픽셀 표현 방식, 에셋 교체 일정 |
| 9 | 수치 관리 | ScriptableObject 이전 시점 |

## 검증 상태

- Unity 프로젝트는 템플릿 상태이며, 빌드·컴파일 검증은 아직 하지 않았다.
- 프로토타입 내용은 기획 문서에 정리된 구현 현황을 옮긴 것이며, 저장소에서 직접 확인하지 않았다.

## 남은 우선 작업

1. 공통 문서를 팀 리뷰 후 `dev`에 병합하고, `main`과 `dev`에 브랜치 보호를 설정한다. `main`은 첫 출시 준비 때 `release/` 브랜치를 병합해 채운다.
2. Photon Fusion 2 SDK를 연동하고 Host 모드 뼈대(`NetworkBootstrap`, `PlayerSpawner`, `GameState`)를 만든다.
3. 프로토타입을 폴더 구조에 맞춰 저장소에 들이고 Unity `6000.3.24f1`에서 컴파일을 확인한다.
4. 기획-구현 차이 중 단조 방식을 팀에서 결정한다.
5. 역할별(대장간, 광장, 전투) 기능을 구현하고 MVP 시연으로 피드백을 받는다.
6. 밸런스 수치를 ScriptableObject로 옮긴다.
7. `Core` 규칙의 시드 결정성을 확인하는 EditMode 테스트를 추가한다.
8. Unity CLI 사용이 안정되면 예비로 둔 Unity MCP 연동을 정리한다.

## 관련 문서

- `CLAUDE.md`: 프로젝트 개발 지침
- `GIT_WORKFLOW.md`: 브랜치, 커밋, PR 규칙
- `docs/GAME_CONCEPT.md`: 게임 컨셉과 세부 기획 (추가 예정)
- `docs/ai-prompts/README.md`: AI 협업 기록 규칙
