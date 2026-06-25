# Check Missing Scripts (com.actionfit.checkmissingscripts)

열린 씬과 프로젝트의 모든 프리팹에서 **Missing 스크립트**를 찾아 보고하고 일괄 제거하는 Unity 에디터 도구입니다.

## 설치 (manifest.json, Git URL)

```json
{
  "dependencies": {
    "com.actionfit.checkmissingscripts": "https://github.com/ActionFit-Editor/Check_Missing_Scripts.git#1.0.1"
  }
}
```

## 구성

- **Editor** (`com.actionfit.checkmissingscripts.Editor`): `MissingScriptTools` (정적 메뉴 도구).

## 사용

- `Tools > Missing Scripts > Find In Open Scenes` / `Find In All Prefabs` — 탐색·보고
- `Tools > Missing Scripts > Remove In Open Scenes` / `Remove In All Prefabs` — 일괄 제거
