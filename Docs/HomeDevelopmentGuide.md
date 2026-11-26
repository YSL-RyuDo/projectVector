# 다른 PC에서 이어서 작업하는 방법과 폴더 정리 기준

작성일: 2026-10-29

**기존에 씬별로 모아 둔 방식을 이어간다. 씬 전용 코드는 해당 모듈, 여러 씬에서 쓰는 코드는 공통 영역에 넣는다. 새 PC에서 폴더를 다시 정리하는 것이 아니라, main에 반영된 구조를 받아 그대로 사용한다.**

폴더 정리는 `d1198c52`까지 main에 반영되어 있다. 아래 경로는 모두 클라이언트 저장소 `projectVector`의 루트를 기준으로 한다. PC마다 `C:\Users\...` 경로가 달라도 같은 방식으로 찾을 수 있다.

## 1. 자취방 PC에서 작업을 시작할 때

1. 현재 PC에서 작업한 씬과 프리팹을 저장한다. 다른 PC로 가져갈 변경은 작업 브랜치에 커밋하고 push한다.
2. 자취방 PC에도 미완료 작업이 있다면 먼저 보존한다. **stash는 해당 PC에만 저장되므로 다른 PC로 자동 전달되지 않는다.**
3. 자취방 PC에서 Unity를 종료한 상태로 원격 변경을 받고, 클라이언트 작업 브랜치 `YSL`에 최신 main을 병합한다.
4. Unity Hub에서 `Assets`, `Packages`, `ProjectSettings`가 들어 있는 프로젝트 루트를 연다. `Assets` 폴더 자체를 프로젝트로 지정하지 않는다.
5. 현재 지정 버전은 Unity `6000.3.1f1`이다. 이후 버전이 바뀌면 `ProjectSettings/ProjectVersion.txt`를 기준으로 맞춘다. 최초 임포트와 컴파일이 끝난 뒤 작업한다.

Git 명령을 사용할 경우 클라이언트 저장소에서 아래 순서로 실행한다. 각 단계가 성공한 것을 확인한 뒤 다음으로 진행한다.

```powershell
git status
git fetch origin
```

로컬 변경이 안전하게 보존된 상태에서 작업 브랜치로 이동한다.

```powershell
git switch YSL
```

새로 받은 저장소여서 로컬 YSL이 아직 없다면 위 명령 대신 다음을 사용한다.

```powershell
git switch --track origin/YSL
```

원격 YSL의 작업을 반영한 다음 최신 main을 합친다.

```powershell
git merge --ff-only origin/YSL
git merge origin/main
```

첫 번째 병합이 실패하거나 충돌이 나오면 다음 작업을 멈추고 로컬/원격의 변경 내용을 확인한다. 작업을 버리는 `reset --hard`로 해결하지 않는다. 충돌이 해결되고 검증이 끝나면 YSL을 push한다.

클라이언트 개발은 **YSL**, 서버 개발은 **ProjectVectorServer 저장소의 main**에서 진행한다. 클라이언트 main으로의 반영은 작업 완료 후 병합한다.

## 2. Unity에서 보는 것과 탐색기에서 보는 것

| 위치 | 보는 곳 | 용도 |
| --- | --- | --- |
| `Assets/ProjectVector` | Unity Project 창 | 우리가 만든 코드, 씬, 프리팹, 이미지 등 |
| `Assets/ThirdParty` | Unity Project 창 | 외부 에셋 패키지 |
| `Docs` | 탐색기, VS Code, GitHub | 작업 안내와 기능 문서 |
| `Tools` | 탐색기, VS Code, 터미널 | 이전 경로 목록, 구조 검사 스크립트 |

`Docs`와 `Tools`는 **Assets와 같은 높이**에 있다. `ScriptMigration.json`을 Unity Project 창에서 찾을 필요는 없다.

## 3. 새 스크립트를 어디에 넣을지 결정하기

다음 순서로 판단한다.

1. **기존 기능을 수정하는가?** 먼저 해당 기능의 기존 스크립트를 찾는다. 같은 일을 하는 새 관리자를 만들지 않는다.
2. **특정 씬이나 화면에만 속하는가?** `Modules/<담당 모듈>/Scripts/<기능>`에 넣는다.
3. **경기와 훈련장처럼 여러 씬에서 함께 사용하는가?** `Runtime`의 해당 기능 폴더에 넣는다.
4. **Unity 에디터에서만 실행하는 제작 도구인가?** `Editor`에 넣는다.
5. **명시적으로 실행하는 검사 코드인가?** `Tests/Editor` 또는 `Tests/Runtime`에 넣는다.

아래 표의 경로 앞에는 `Assets/ProjectVector/`가 붙는다.

| 수정할 내용 | 코드 위치 |
| --- | --- |
| 로그인, 회원가입 | `Modules/Login/Scripts` |
| 로비 탐색, 프로필, 커리어, 매칭 | `Modules/Lobby/Scripts`의 해당 기능 폴더 |
| 로비 상점 | `Modules/Lobby/Scripts/Shop` |
| 캐릭터 픽과 교환 | `Modules/Match/Scripts/Pick` |
| 경기 진행 단계 | `Modules/Match/Scripts/Flow` |
| 라운드와 공수 교대 | `Modules/Match/Scripts/Rounds` |
| 인게임 상점 | `Modules/Match/Scripts/Store` |
| 경기 HUD, 관전 | `Modules/Match/Scripts/HUD`, `Modules/Match/Scripts/Spectator` |
| 디바이스 | `Modules/Match/Scripts/Device` |
| 결과, 랭크 결과 | `Modules/Results/Scripts` |
| 훈련장 전용 기능 | `Modules/Practice/Scripts` |
| 튜토리얼 전용 기능 | `Modules/Tutorial/Scripts` |
| 공통 이동, 카메라, 장비 선택 | `Runtime/Player` |
| 네 캐릭터의 스킬 | `Runtime/Characters/<캐릭터 역할>` |
| 공통 무기와 피해 | `Runtime/Combat/Weapons`, `Health`, `HitDetection` |
| 전투 리포트, 핑, 킬로그 | `Runtime/Combat/Report`, `Ping`, `KillFeed` |
| 연결, 패킷, 재접속 | `Runtime/Networking` |
| 음성, 채팅, 친구 | `Runtime/Social` |
| 설정 | `Runtime/Settings` |
| Unity에 의존하지 않는 상태와 계약 | `Runtime/Core` |

캐릭터 역할 폴더는 `Initiator`, `Controller`, `Sentinel`, `Entry`이며 캐릭터 공통 코드는 `Runtime/Characters/Shared`에 있다.

**기능 이름이 같다는 이유로 씬마다 복사하지 않는다.** 예를 들어 총기 피해 계산을 훈련장 폴더에 새로 복제하지 말고, 기존 공통 계산과 훈련장 연결부를 확인한다. 반대로 로비 상점과 인게임 구매창은 역할이 다르므로 이름이 비슷하다고 하나로 합치지 않는다.

## 4. 스크립트와 관련 에셋은 이렇게 묶는다

씬 전용 자료는 해당 모듈 안에 모은다. 필요하지 않은 하위 폴더까지 미리 만들지는 않는다.

```text
Assets/ProjectVector/Modules/Match/
├─ Scripts/       픽, HUD, 상점, 라운드 등의 코드
├─ Scenes/        Game_TestVersion과 관련 씬 자료
├─ UI/            경기 화면 전용 이미지와 UI 자료
├─ Prefabs/       경기 전용 저장 프리팹
├─ Data/          경기 전용 설정과 데이터
├─ Device/        디바이스 전용 아트와 셰이더
└─ Resources/     기존 로딩 경로를 유지해야 하는 경기 에셋
```

여러 화면이나 모드에서 함께 쓰는 자료는 다음 위치를 사용한다.

| 에셋 | 위치 |
| --- | --- |
| 캐릭터 모델, 애니메이션, 초상화, 프리팹 | `Assets/ProjectVector/Shared/Characters` |
| 캐릭터별 스킬 효과 | `Assets/ProjectVector/Shared/Characters/Effects`의 해당 역할 폴더 |
| 공통 무기 에셋, 아이콘, 데이터 | `Assets/ProjectVector/Shared/Weapons` |
| TestMap, Desert, Construction 등 공통 맵 | `Assets/ProjectVector/Shared/Maps` |
| 공통 UI 이미지 | `Assets/ProjectVector/Shared/UI` |
| 공통 재질과 렌더링 설정 | `Assets/ProjectVector/Shared/Rendering` |
| 공통 소리 | `Assets/ProjectVector/Shared/Audio` |
| 외부 패키지 | `Assets/ThirdParty/<패키지명>` |
| 실험 자료와 이전 버전 보관 | `Assets/ProjectVector/Development` |

`Development/Legacy`는 삭제해도 된다는 표시가 아니다. 보관된 씬과 프리팹 사이의 참조가 남아 있다. 실제 기능으로 채택한 자료는 사용처에 배치하고 참조를 검증한다.

## 5. 파일을 이동하거나 이름을 바꿀 때

### Unity Project 창에서 이동할 때

1. 씬과 프리팹을 저장하고 현재 Git 변경을 확인한다.
2. Project 창에서 대상 폴더로 **이동**한다. 같은 기능의 복사본을 만들지 않는다.
3. Unity의 임포트와 컴파일을 기다린다.
4. 연결된 씬과 프리팹을 열어 Missing Script, Inspector의 빈 참조, 이미지와 재질 누락을 확인한다.
5. 코드에 적힌 경로도 검색한다. GUID 연결이 살아 있어도 `AssetDatabase.LoadAssetAtPath`, 파일 읽기, 제작 도구의 문자열 경로는 따로 수정해야 한다.

### 탐색기나 스크립트로 이동할 때

1. Unity를 종료한 상태에서 진행한다.
2. 에셋과 옆의 `.meta`를 함께 이동한다. 폴더 이동 시 해당 폴더의 `.meta`도 함께 보존한다.
3. `.meta`를 지우거나 새로 생성해서 GUID를 바꾸지 않는다.
4. 목적지에 같은 이름의 파일이 있으면 덮어쓰지 말고 두 파일의 역할과 GUID를 확인한다.
5. Unity를 다시 열고 위와 같은 참조 검사를 진행한다.

**폴더 이동과 클래스명, 네임스페이스, 직렬화 필드 변경은 분리한다.** 이름을 바꾸는 작업은 별도의 직렬화 호환성 검토가 필요하다. 기존 CP949 파일을 무조건 UTF-8로 일괄 변환하지 않는다.

기존 기록에 있는 파일을 다시 이동했다면 `ScriptMigration.json`의 현재 `new` 경로와 `AssetMigration.json`의 관련 경로 기록도 맞춘다. GUID와 최초 경로는 보존한다. 새 파일 추가를 과거 이동 기록인 것처럼 만들어 넣을 필요는 없다.

## 6. Resources는 특히 경로를 유지한다

예를 들어 기존 호출이 다음과 같다면:

```csharp
Resources.Load<Sprite>("IconCard/icon0_card");
```

현재 에셋 위치는 다음과 같은 구조다.

```text
Assets/ProjectVector/Shared/Resources/IconCard/icon0_card.png
```

`Resources` 앞의 물리적 위치와 달리, **그 아래 `IconCard/icon0_card`는 호출에 사용하는 이름**이다. 이를 바꾸려면 호출부와 데이터에서 전달되는 이름까지 함께 확인해야 한다.

서로 다른 Resources 폴더에 같은 상대 경로의 파일을 새로 만들지 않는다. 단순히 런타임에서 쓴다는 이유로 모든 이미지와 프리팹을 Resources에 넣지도 않는다. 기존 Inspector 연결이나 프로젝트의 로딩 방식을 우선 사용한다.

## 7. 옛 파일 위치를 찾는 방법

프로젝트 루트를 VS Code로 열고 다음 파일을 검색한다.

- `Tools/ScriptMigration.json`: 원래 스크립트 경로, 현재 경로, GUID.
- `Tools/AssetMigration.json`: 이전 에셋 경로와 현재 경로.

`ScriptMigration.json`에서 파일명을 검색한 뒤 `new`에 적힌 위치로 이동하면 된다. `old`는 이전 위치다. 파일이 사라졌다고 생각해서 옛 폴더에 새 스크립트를 만들지 않는다.

PowerShell에서 특정 스크립트를 찾을 수도 있다.

```powershell
Get-Content -Raw -Encoding UTF8 Tools/ScriptMigration.json |
    ConvertFrom-Json |
    Where-Object { $_.old -like '*PlayerItem.cs' -or $_.new -like '*PlayerItem.cs' } |
    Select-Object old, new, guid
```

## 8. 팀원의 이전 브랜치와 충돌할 때

예전 폴더에서 수정한 파일과 main에서 이동한 파일이 충돌할 수 있다.

1. 이전 경로 목록과 GUID로 같은 파일인지 확인한다.
2. **팀원의 코드 변경은 새 위치의 파일에 반영한다.** 파일을 옛 위치로 되돌려서 해결하지 않는다.
3. 같은 스크립트와 서로 다른 GUID가 두 군데 생기지 않았는지 확인한다.
4. 씬이나 프리팹 충돌은 전체를 무조건 내 것/상대 것으로 선택하지 말고, 실제 UI 구조와 연결 변경을 확인한다.
5. 코드 컴파일뿐 아니라 해당 씬의 저장된 연결과 실제 동작을 확인한 뒤 병합한다.

내용이 비슷한 스크립트를 발견했을 때도 사용처와 네트워크/로컬 처리 차이를 확인한 후 통합 여부를 결정한다.

## 9. 작업을 마치기 전에

- [ ] 클라이언트 작업 브랜치가 YSL인지 확인했다.
- [ ] 개인 이름 폴더, 루트 Scripts 폴더 등에 새 파일을 흩어 놓지 않았다.
- [ ] 기존 기능을 복제하지 않고 담당 모듈에 수정했다.
- [ ] 파일 이동 시 `.meta`와 기존 GUID를 보존했다.
- [ ] 씬, 프리팹, Resources와 문자열 경로를 확인했다.
- [ ] 기존 UI를 삭제하거나 런타임에서 대체 화면을 만들어 덮지 않았다. UI 구조는 저장된 씬/프리팹 Hierarchy에 있다.
- [ ] 수정한 기능을 실제 사용 씬에서 확인했다. 공통 코드라면 관련 훈련장/로비 흐름도 확인했다.
- [ ] 빌드, 녹화, 검증 로그를 저장소 밖에 저장했다.
- [ ] Git 변경 목록에 의도하지 않은 씬, 폰트, RenderTexture 변경이 섞였는지 확인했다. 필요 없는지 판단하기 전에는 지우지 않았다.
- [ ] 한글 커밋 메시지를 작성하고, 다른 PC에서 이어서 작업할 변경을 push했다.

Python이 설치되어 있다면 프로젝트 루트에서 구조 검사를 실행할 수 있다.

```powershell
python Tools/verify_module_layout.py
python Tools/verify_asset_layout.py
```

이 검사는 GUID, 경로와 기본 구조를 확인한다. 실제 플레이 검증을 대신하지 않는다. 단순한 문서 수정에는 게임 전체 검증을 반복할 필요가 없고, 공통 코드나 넓은 범위의 에셋 이동이라면 빌드와 관련 씬 전환까지 확인한다.

Unity batch 검사는 같은 프로젝트가 다른 Unity 에디터에서 열려 있지 않을 때만 실행한다. 내보내기 도구의 기본 저장 위치는 문서 폴더의 `VectorExports`이며, `VECTOR_EXPORT_ROOT`로 저장소 밖 경로를 지정할 수 있다.

## 10. 자취방에서 AI에게 작업을 맡길 때 붙여 넣을 내용

```text
이 프로젝트는 씬별 모듈 기준으로 폴더를 정리한 상태다.
먼저 AGENTS.md, Docs/FolderLayout.md, Docs/Architecture.md,
Docs/HomeDevelopmentGuide.md를 읽고 현재 Git 상태와 브랜치를 확인해라.

클라이언트 개발은 YSL, 서버 개발은 ProjectVectorServer의 main에서 한다.
기존 로컬 변경과 stash를 보존하고, 작업 내용을 버리거나 임의로 섞지 마라.
이전 작업 기록만 보고 구현을 추정하지 말고 현재 파일과 연결 상태를 확인해라.

씬 전용 코드는 Assets/ProjectVector/Modules/<모듈>/Scripts에,
공통 코드는 Assets/ProjectVector/Runtime의 해당 기능에 둬라.
에셋은 해당 모듈 또는 Shared에, 외부 패키지는 Assets/ThirdParty에 둬라.
기존 기능을 먼저 찾아서 수정하고 개인 이름 폴더나 중복 관리자를 만들지 마라.

옛 경로는 Tools/ScriptMigration.json과 Tools/AssetMigration.json에서 찾아라.
이동 시 .meta와 GUID, Resources 이름, 씬/프리팹 연결을 보존해라.
폴더 이동과 클래스/직렬화 구조 변경을 한꺼번에 하지 마라.
UI는 저장된 씬/프리팹에서 수정하고 기존 화면과 연결을 유지해라.

사용 중인 Unity를 임의로 종료하거나 저장하지 않은 씬을 덮지 마라.
컴퓨터 UI 자동화 없이 파일과 코드, 프로젝트가 닫혀 있을 때의 batch 검증을 사용해라.
검증 결과와 빌드는 저장소 밖에 두고, 수행한 검사와 미검증 범위를 구분해라.
```

전체 폴더 위치는 [FolderLayout.md](FolderLayout.md), 공통 코드의 책임과 수명 규칙은 [Architecture.md](Architecture.md)를 참고한다. `NextWorkSession.md`는 2026-10-13에 작성된 특정 작업의 과거 기록이므로 현재 전체 구현 상태를 판단하는 기준으로 사용하지 않는다.
