# Tempered Heroes (가제) 프로젝트 진행 현황

> 기준일: 2026-10-30
>
> 기준 브랜치: `dev` (`e75368d`, Unity 프로젝트 초기화 병합)
>
> 기준 main: `origin/main` (`d469a13`, Initial commit)

## 프로젝트 요약

Tempered Heroes는 Unity 6 URP 기반의 2.5D 쿼터뷰 대장간 경영·모험가 파견 RPG다. 플레이어는 낮에 대장간에서 장비를 직접 제작해 판매하고 모험가를 영입한다. 밤에는 제작한 장비를 입힌 모험가를 던전으로 파견해 자동 전투 결과를 확인한다. 최종 목표는 모험가를 키워 마왕을 토벌하는 것이다.

대장간에서 관리할 수 있는 모험가는 대장간 업그레이드로 슬롯이 1 → 2 → 4 → 6 → 8칸으로 늘어 최대 8명이다. 한 번에 파견할 수 있는 모험가는 최대 5명이다. 슬롯이 5칸보다 많으면 관리 중인 모험가 중에서 파견할 모험가를 골라 장비를 입혀 보낸다. 5명을 꼭 채워 보낼 필요는 없고, 몇 명을 보낼지는 파견할 때 플레이어가 정한다.

현재 저장소에는 프로젝트 공통 문서(`CLAUDE.md`, `GIT_WORKFLOW.md`, 이 문서, `docs/ai-prompts/README.md`)와 URP 템플릿으로 생성한 빈 Unity 프로젝트가 있다.

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

저장소 기준으로는 구현된 시스템이 없다.

| 영역 | 상태 | 구현 내용 |
| --- | --- | --- |
| 장비 제작 미니게임·판매 | 미구현 | |
| 낮·밤 사이클 | 미구현 | |
| 던전·파견·자동 전투 | 미구현 | |
| 모험가 영입·랜덤 생성 | 미구현 | |
| 대장간 업그레이드·해금 | 미구현 | |
| 숙련도·장인 슬롯 | 미구현 | |
| 저장/불러오기 | 미구현 | |
| Photon Fusion 2 연동 (Host 모드) | 미구현 | |
| 방송 연동 | 미구현 | |
| 자동화 테스트 | 미구축 | |

## 검증 상태

- Unity 프로젝트는 템플릿 상태이며, 빌드·컴파일 검증은 아직 하지 않았다.

## 남은 우선 작업

1. 공통 문서를 팀 리뷰 후 `dev`에 병합하고, `main`과 `dev`에 브랜치 보호를 설정한다. `main`은 첫 출시 준비 때 `release/` 브랜치를 병합해 채운다.
2. Photon Fusion 2 SDK를 연동하고 Host 모드 뼈대(`NetworkBootstrap`, `PlayerSpawner`, `GameState`)를 만든다.
3. 역할별(대장간, 광장, 전투) 기능을 구현하고 MVP 시연으로 피드백을 받는다.
4. `Core` 규칙의 시드 결정성을 확인하는 EditMode 테스트를 추가한다.
5. Unity CLI 사용이 안정되면 예비로 둔 Unity MCP 연동을 정리한다.

## 관련 문서

- `CLAUDE.md`: 프로젝트 개발 지침
- `GIT_WORKFLOW.md`: 브랜치, 커밋, PR 규칙
- `docs/GAME_CONCEPT.md`: 게임 컨셉과 세부 기획 (추가 예정)
- `docs/ai-prompts/README.md`: AI 협업 기록 규칙
