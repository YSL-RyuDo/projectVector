# 프로젝트 폴더 안내

2026-10-29, 클라이언트 `YSL` 기준. 기존에 씬별로 모아 둔 스크립트 구분을 유지하면서 모델, UI, 프리팹, 데이터까지 같은 기준으로 정리했다.

## 먼저 찾을 위치

| 작업 | 위치 (`Assets/ProjectVector/` 기준) |
| --- | --- |
| 로그인, 회원가입 | `Modules/Login` |
| 로비, 프로필, 매칭, 로비 상점, 대화, 패스 | `Modules/Lobby` |
| 픽, 경기 흐름, 인게임 상점, HUD, 디바이스, 관전 | `Modules/Match` |
| 경기 결과, 랭크 결과 | `Modules/Results` |
| 훈련장 | `Modules/Practice` |
| 튜토리얼 | `Modules/Tutorial` |
| 공통 캐릭터 스킬, 플레이어, 전투, 네트워크 | `Runtime` |
| 공통 모델, 애니메이션, 무기, 맵, 재질, 소리, 이미지 | `Shared` |
| 이전 씬, 테스트 프리팹, 원본 모델, 참고 이미지, 복구본 | `Development` |
| 씬/프리팹 제작, 캡처, 빌드, 서버 맵 내보내기 | `Editor` |
| 회귀 검사 | `Tests` |

각 씬 모듈 안에서 `Scripts`는 코드, `Scenes`는 씬, `UI`는 화면 에셋, `Prefabs`는 저장된 프리팹이다. 필요한 모듈에만 `Data`, `Resources`, `Animations`, `Preview` 등을 둔다. 로비와 인게임 상점은 서로 다른 화면이므로 각각 `Modules/Lobby/Scripts/Shop`, `Modules/Match/Scripts/Store`에서 찾는다.

`Game_TestVersion`은 실제 경기 씬 이름이다. 픽, 로딩, 경기 상태가 이 씬 안에서 전환되므로 `Modules/Match`에 함께 있다. `Result`와 `RankResult`는 `Modules/Results/Scenes`, 공사장 맵 씬은 `Shared/Maps/Scenes`에 있다.

## 공통 코드와 에셋

`Runtime/Core`, `Networking`, `Player`, `Characters`, `Combat`, `Social`, `Settings`, `Shared`는 여러 씬에서 사용하는 코드다. 예를 들어 무기는 실제 경기와 훈련장에서 함께 쓰므로 한쪽 씬의 스크립트로 복사하지 않는다. 설정과 음성도 씬마다 새 관리자를 만들지 않는다.

- `Shared/Characters`: 캐릭터 모델, 애니메이션, 프리팹, 초상화, 캐릭터별 효과.
- `Shared/Weapons`: 무기 에셋, 아이콘, 데이터.
- `Shared/Maps`: TestMap, Desert, Construction 프리팹과 지도 이미지.
- `Shared/Rendering`: 공통 재질, 렌더링 설정.
- `Shared/UI/Surfaces`: 실제 화면에서 사용하는 카드, 배경, 통계 표면 이미지. 이전 `VideoReference` 이름과 달리 게임에서 사용 중인 에셋이다.
- `Shared/Resources`: 카드, 아이콘, 랭크, 지도, 공통 소리 등 동적 로딩 에셋.
- `Modules/Lobby/Resources`: 커리어, 대화, 레벨 보상, 로비 캐릭터, 상점 데이터.

## 외부 패키지와 보존 자료

`Assets/ThirdParty`에 FPS_Bundle, 캐릭터 패키지, 음향 패키지, QuickOutline 등 외부 에셋을 모았다. 패키지 내부 파일명과 상대 경로 구조는 유지한다.

`Assets/Plugins`는 Unity 플러그인 처리 경로, `Assets/TextMesh Pro`는 TMP 도구의 관례 경로이므로 루트에 유지한다. `Packages`, `ProjectSettings`도 Unity 프로젝트 필수 경로다. `Library`, `Temp`, `UserSettings`와 로컬 빌드 폴더는 자동 생성/로컬 자료이며 소스 분류를 위해 옮기거나 지우지 않는다.

`Development/Legacy`는 삭제 후보 목록이 아니다. 보관된 이전 씬끼리도 프리팹을 참조한다. 실제 튜토리얼에서 쓰는 이전 구성 요소는 `Modules/Tutorial/Prefabs/Legacy`에, 엔트리가 사용하는 기존 연막은 `Shared/Characters/Effects/Entry/LegacySmoke`에 뒀다. 사용되지 않는 듯 보인다는 이유만으로 삭제하면 안 된다. `Resources.Load`와 서버가 내려주는 이름에 따른 로딩도 확인해야 한다.

저장소 루트에 있던 Unity 생성 CommandBuffer C# 7개는 `Docs/Reference/UnityGenerated`로 옮겼다. 원래 Unity 게임 컴파일 대상이 아니며 계속 참고 자료로 보관한다. 기존 콜라이더 내보내기 파일은 `Docs/Reference/LegacyMapExports`에 있다. 새 서버 맵 내보내기는 기본적으로 문서 폴더의 `VectorExports`에 저장한다. `VECTOR_EXPORT_ROOT`로 저장소 밖 다른 폴더를 지정할 수 있다.

## 팀 작업 규칙

1. 개인 이름 폴더 대신 담당 씬/기능 폴더에 저장한다. 두 씬에서 함께 쓰는 코드는 공통 영역으로 올린다.
2. 파일 이동은 `.meta`와 함께 한다. 클래스명과 직렬화 필드 변경은 폴더 이동과 별도 작업으로 다룬다.
3. 같은 파일명이라고 같은 역할은 아니다. 자체 C# 파일의 동일 이름 및 내용 완전 중복 검사는 이번 정리 시점에 각각 0건이었다. 역할이 유사한 훈련장/온라인 구현을 합쳤다는 뜻은 아니다.
4. `Resources` 아래 상대 경로는 런타임 API다. 폴더 소유 위치를 바꿔도 그 아래 이름은 유지한다. 외부 UnityChanSSU에 원래부터 존재하는 PNG/PSD 동일 키 2건은 그대로 보존했다.
5. 씬과 UI 구조는 저장된 Hierarchy에서 수정한다. 폴더 정리를 이유로 기존 UI를 제거하거나 런타임 대체 UI를 생성하지 않는다.
6. 원래 CP949인 파일은 인코딩을 유지한다. 빌드, 캡처, 검증 결과는 저장소 밖에 둔다.

## 옛 위치와 검사 방법

- `Tools/ScriptMigration.json`: 최초 개인별/기능별 폴더에서 현재 스크립트까지의 경로와 GUID.
- `Tools/AssetMigration.json`: 이번 이동 전후 전체 경로와 파일 식별 정보.
- `python Tools/verify_module_layout.py`: 532개 기존 스크립트 GUID, Core 의존성, 씬 이동 진입점 검사.
- `python Tools/verify_asset_layout.py`: 5,456개 이동 에셋, GUID 중복, Resources 키, 빌드 씬 경로 검사.
- Unity batch `AssetLayoutVerification.Run`: 실제 Resources 로딩, 기존 회귀 검사, ProjectVector 아래 저장 씬 검사.
- `RankedCaptureTestEditor.Build`, `Tools/run_scene_smoke.py`: 배포 씬 개발 빌드와 실제 플레이어의 씬 이동 검사.

빌드 씬의 순서와 활성 여부는 유지했다. 비활성 `KSW_Test` 항목에 남아 있던 오래된 GUID만 기존 씬 `.meta`에 맞췄다. 게임 규칙이나 네트워크 패킷 변경은 이 폴더 정리 범위에 포함되지 않는다.

## 이번 정리 검증 결과

- 기존 GUID 6,637개와 Resources 경로 477개 유지. 중복 GUID 및 새 Resources 이름 충돌 없음.
- 경로가 변경된 직렬화 파일 635개의 오브젝트 참조와 로컬 fileID 연결 유지.
- Unity 컴파일, Resources 에셋 436건의 실제 로딩, Core 83항목, 씬 수명 74항목과 기존 이동/ESC/디바이스/스킬 회귀 검사 통과.
- 자체 저장 씬 22개 검사. 배포 씬의 Missing Script 0개. `Development/Recovery/Scenes/0.unity`, `0 (1).unity`에는 이동 전부터 없는 스크립트가 각각 2개 있으며, 복구본의 원형을 보존했다.
- 네 캐릭터 훈련장 Play 검사에서 총/권총/칼/디바이스 전환, 스킬 복귀, 이동/앉기/점프 중 손 접촉, 사망 취소 통과.
- 최종 Windows 개발 빌드 성공, 실제 플레이어에서 6개 배포 씬 사이 9회 이동 통과.
- 서버 호감도/리포트 계약 검사, 14개 무기 피해/상점 데이터 생성 결과 일치 확인. 서버 검사 도구는 기존 main 경로와 새 YSL 경로를 모두 지원한다.

검증 로그, 캡처, 빌드는 저장소 밖 `Vector-FolderCleanup-20261029`에 있다. 계정 데이터나 가짜 경기 결과를 사용하지 않았으며, 이번 검사는 새 6인 경기/외부 PC 음성 통신 시험을 뜻하지 않는다. 숨겨진 그래픽 에디터의 OS 커서 잠금은 창 포커스 영향을 받아 실패했으므로, 상태 회귀 검사는 `-batchmode -nographics`, 실제 장비 렌더링 검사는 그래픽을 켠 별도 Play 세션에서 실행했다.
