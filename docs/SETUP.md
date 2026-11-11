# 개발 환경 설정

처음 저장소를 받았을 때나 환경 문제가 생겼을 때 읽는다.

## 버전

- Unity `6000.3.24f1`, URP `17.3.0`, Input System `1.20.0`, Test Framework `1.6.0`
- Photon Fusion 2.1.3, Host 모드
- 플랫폼: PC (Windows/Mac). 배포: 스토브 우선 출시 → 스팀
- 브랜치 전략: Git Flow (`GIT_WORKFLOW.md`)
- 빌드 씬 순서: `Bootstrap` → `Lobby` → `Game`. 그 뒤의 `SampleScene`(템플릿 기본값)과 테스트 씬은 개발용이다.

## Photon 설정 (로컬 최초 1회)

App ID가 담긴 `Assets/Photon/Fusion/Resources/PhotonAppSettings.asset`은 `.gitignore`로 제외한다.

1. 프로젝트를 열면 Fusion이 빈 `PhotonAppSettings.asset`을 만든다. 없으면 `Tools > Fusion > Realtime Settings`를 연다.
2. `Tools > Fusion > Realtime Settings`에서 `App Id Fusion`과 `Fixed Region`을 입력한다.
3. 값은 팀 비공개 채널에서 받는다. 지역이 다르면 같은 세션에 들어가지 못하므로 팀 전원이 같은 값을 쓴다.

- App ID를 문서, 커밋, PR에 적지 않는다.
- Player Settings의 `Allow 'unsafe' Code`, `Run In Background`, Fusion 정의 심볼은 Fusion이 요구하므로 끄지 않는다.

## 멀티플레이 실행

- `Bootstrap` 씬에서 플레이한다. 로비에서 세션명을 입력하고 `Host`나 `Join`을 누른다. 혼자 할 때도 `Host`로 시작한다.
- 두 명 테스트는 빌드한 실행 파일과 에디터를 같은 세션명으로 접속한다.
- 새 `NetworkObject` 프리팹을 스폰할 때 "has been baked with a guid ... failed to be translated into a prefab" 오류가 나면 `Tools > Fusion > Rebuild Prefab Table`을 실행한다.
- 에디터 스크립트나 CLI로 씬을 만들었다면 `Tools > Fusion > Scene > Bake Scene Objects`를 실행하고 다시 저장한다. 베이크되지 않은 씬 오브젝트는 런타임에 등록되지 않는다.

## AI 에이전트 연동

| 패키지 | 역할 | 상태 |
| --- | --- | --- |
| Unity Pipeline `0.9.1-exp.1` (`com.unity.pipeline`) | Unity CLI가 에디터에 연결해 씬·프리팹 조작, 빌드, 테스트, 플레이를 수행 | 사용 중 |
| Unity AI Assistant `2.21.0-pre.1`, AI Inference `2.6.1` | Unity MCP 서버(relay `~/.unity/relay/`) | 예비 |

- 에디터 작업은 Unity CLI로 한다. Unity MCP는 CLI에 문제가 있을 때만 쓴다. MCP는 Unity에서 지원 중단 예정이다.
- 모두 실험·프리릴리스 버전이다. 업데이트 후 에디터 오류가 나면 이 패키지들을 먼저 확인하고, 버전은 팀 전원이 같게 맞춘다.

### Unity CLI

- 팀 기준 버전: `1.0.0-beta.14`
- 설치 (Windows PowerShell):

  ```powershell
  $env:UNITY_CLI_CHANNEL='beta'; irm https://public-cdn.cloud.unity3d.com/hub/prod/cli/install.ps1 | iex
  ```

- `unity --version`이 기준보다 낮으면 `unity self-update`.
- 연결 확인: 에디터에서 이 프로젝트를 연 채 `unity command editor status`가 `ready`이고 `projectPath`가 이 저장소인지 본다.
- 기준 버전을 올리면 팀에 공유하고 이 문서를 고친다.
- 명령이 멈추거나 느리면 에디터 창이 백그라운드에서 갱신을 늦추는지 확인한다.
