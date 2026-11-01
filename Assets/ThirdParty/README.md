# 외부 에셋

Unity Asset Store, itch.io 등에서 받은 외부 에셋을 둔다. 새로 들여올 때는 아래 표에 출처와 라이선스를 적는다.

- 에셋은 Unity의 Project 창에서 이 폴더로 옮긴다. 탐색기에서 옮기면 `.meta`가 어긋나 참조가 깨질 수 있다.
- 에셋을 고친 경우에는 무엇을 고쳤는지 아래에 적는다.
- 라이선스 조건이 확실하지 않은 에셋은 출시 전에 다시 확인한다.

| 폴더 | 내용 | 출처 | 라이선스 | 들여온 날 | 수정 사항 |
| --- | --- | --- | --- | --- | --- |
| `PolyRonin` | Medieval Blacksmith Props & Tools Collection For Games (화로, 모루, 풀무, 집게, 망치, 검 등 로우폴리 소품 27개) | [Unity Asset Store](https://assetstore.unity.com/packages/slug/313184) | Unity Asset Store 이용약관(무료 에셋). 팀원 사용 범위는 구매 계정 기준이라 확인이 필요하다 | 2026-11-02 | 머티리얼 2개를 Standard 셰이더에서 URP Lit로 바꿨다 |
| `VOiD1 Gaming - Stylized Fire Shader` | Free Stylized Fire Shader for Unity URP (불꽃 셰이더 그래프, 머티리얼, 메시) | [itch.io](https://void1gaming.itch.io/free-stylized-fire-shader-for-unity-urp) | 패키지에 라이선스 문서가 없다. itch.io 페이지의 조건을 출시 전에 확인해야 한다 | 2026-11-02 | 없음 |

## 알려진 문제

- `PolyRonin` 소품은 크기가 서로 다르다. 화로는 굴뚝까지 높이 약 5.7, 모루는 폭 약 0.65, 통은 높이 약 0.39, 검은 길이 약 0.84다. 장면에 쓸 때 소품마다 크기를 맞춰야 한다.
- 불꽃 셰이더 그래프를 열면 "더 새로운 버전의 노드가 있다"는 경고가 한 건 나온다. 동작에는 영향이 없다.
