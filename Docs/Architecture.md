# Project Vector 코드 구조와 작업 규칙

## 기준

2026-10-29, 현재 개발분을 `main`에 병합한 뒤 `YSL`에서 구조 정리를 진행했다. 서버는 `main`을 사용한다. 씬, 프리팹, 공개 컴포넌트 이름, 직렬화 필드, 패킷 번호와 계정 데이터 형식은 유지한다.

## 코드를 찾는 위치

자체 C# 코드는 `Assets/ProjectVector` 안에서 찾는다. `KSW`, `Prefab`, `Resources`에는 기존 모델, 이미지, 데이터, 프리팹을 유지한다. 에셋 패키지에 포함된 외부 코드는 이번 이동 대상이 아니다.

| 경로 | 담당하는 내용 |
| --- | --- |
| `Runtime/Core` | Unity에 의존하지 않는 데이터 계약, 씬 분류, 구매 요청 상태, 장비 숨김 상태 |
| `Runtime/Lobby/Login` | 로그인, 회원가입 화면 |
| `Runtime/Lobby` | 로비 탐색, 매칭, 프로필, 커리어, 상점, 재화 |
| `Runtime/Match` | 픽, 로딩, 라운드, 인게임 HUD, 구매, 디바이스, 관전, 결과 |
| `Runtime/Characters` | 캐릭터별 스킬과 연출 |
| `Runtime/Player` | 이동, 입력, 카메라, 사망, 장비 선택 |
| `Runtime/Combat` | 무기, 명중, 피해, 리포트, 킬로그, 핑, 가시성 |
| `Runtime/Networking` | 연결, 패킷 송수신, 재접속, 서버 확정 데이터 적용 |
| `Runtime/Social` | 음성, 채팅, 친구 화면 |
| `Runtime/Practice`, `Runtime/Tutorial` | 로컬 훈련장, 튜토리얼과 해당 씬에 연결된 기존 코드 |
| `Runtime/Settings`, `Runtime/Shared` | 설정, 공통 UI와 씬 이동 |
| `Editor` | 저장된 씬/프리팹 제작 도구, 빌드 검사, 캡처 도구 |
| `Tests/Editor`, `Tests/Runtime` | 회귀 검사, 명시적으로 실행하는 시연/검증 코드 |

옛 경로는 `Tools/ScriptMigration.json`에서 검색하면 새 경로와 원래 GUID를 찾을 수 있다. 532개 기존 스크립트와 `.meta`를 함께 이동했다. 파일명이 비슷하다고 삭제하거나 다른 파일로 덮어쓰지 않는다.

## 씬과 화면 수명

| 씬 | 역할과 상태 |
| --- | --- |
| `Login` | 로그인 UI만 연결, 로비로 이동하면 해당 UI 참조 해제 |
| `Lobby` | 로비 컨트롤러 등록, 매칭/프로필/친구/상점 표시 |
| `Game_TestVersion` | 현재 실제 경기 씬. 픽 → 로딩 → 경기 단계는 같은 씬 안에서 전환 |
| `Result` | 서버 결과 캐시로 MVP, 랭크, 보상, 결과표 표시 |
| `RankResult` | 기존 별도 랭크 결과 경로. 결과 데이터가 없으면 `Result`로 복귀 |
| `WeaponTest` | 로컬 훈련장. 인증 상태에 따라 로비 또는 로그인으로 복귀 |
| `Tutorial` | 기존 튜토리얼. 현재 배포 빌드 목록에는 포함되지 않음 |
| `CharacterPick`, `Game`, `SampleScene`, `SystemSample`, `KSW_Test` | 기존 실험/이전 씬. 배포 흐름으로 자동 편입하거나 삭제하지 않음 |

- 씬 이름/역할은 `Core/SceneCatalog.cs`가 담당한다. 새 실제 씬을 추가할 때 이 목록, Build Settings, 씬 검증을 함께 갱신한다.
- 실제 이동은 `Shared/SceneNavigation.cs`의 `Load`, `LoadAsync`를 사용한다. 재접속은 같은 경기 씬을 다시 로드할 수 있으므로 무조건 같은 씬 이동을 막지 않는다.
- 이동 함수는 보상 지급이나 방 이탈을 대신 처리하지 않는다. 기존처럼 해당 서버 응답을 받은 기능이 이동을 요청한다.
- `NetworkManager.SceneBindings.cs`에서 로그인, 로비, 픽, 경기 UI의 등록/해제를 관리한다. 씬 파괴 시 **자기 참조만** 해제하고, 씬 unload 시 남은 참조를 정리한다. 이전 화면의 파괴가 새 화면의 등록을 지우면 안 된다.
- `GameController`는 게임 단계/커서를, `SettingsMenu`는 열린 설정창을 담당한다. 리스폰은 씬 이동이 아니며 열린 설정창을 닫지 않는다.
- F10 표시와 재접속 대상 씬은 `SceneCatalog`의 정책으로 판단한다. 픽/로딩 중에는 음성 UI를 경기 HUD처럼 노출하지 않는다.

## 네트워크 파일의 책임

`NetworkManager.cs`는 연결, 수신 큐, 패킷 분배와 공통 경기 처리의 진입점이다. 로비, 방, 픽, 결과, 장비, 디바이스, 네 캐릭터의 스킬 송수신은 각각 `NetworkManager.<기능>.cs`에서 찾는다. 분리된 기존 패킷 처리 본문은 그대로 옮겼다.

이 `partial` 파일들은 **여전히 하나의 클래스**다. 파일 분리가 곧 결합도 해소를 뜻하지 않는다. 새 기능은 이 파일들을 다시 비대하게 만들기보다, 검증/상태는 Core 또는 기능별 서비스에 두고 네트워크 계층은 변환과 전달을 담당하도록 작성한다. 기존 공개 API는 호출부를 한 기능씩 옮기면서 유지한다.

`TeamPurchaseState`는 구매 요청 파싱/방 식별/결과 상태를 처리한다. UI는 확정 크레딧 이벤트를 구독한다. 상점을 잠시 숨긴 상태에서도 구매 완료 결과를 받아야 하므로 씬에서 파괴될 때 구독을 해제한다. 실제 결제와 지급 권한은 계속 서버에 있다.

## 장비 표시의 단일 규칙

스킬, 드론, 디바이스, 사망 처리가 같은 무기를 각각 켜고 끄던 경합을 `EquipmentVisibilityState`로 관리한다.

```csharp
// 이 기능이 무기를 숨길 때
inventory.SetWeaponVisibility(false, this);
// 이 기능이 끝나면 자신의 요청만 해제
inventory.SetWeaponVisibility(true, this);
```

다른 기능이 숨기고 있으면 계속 숨긴다. 같은 요청을 반복해도 카운트가 누적되지 않는다. 새 라운드/리스폰에서는 이전 생명의 요청을 초기화한다. 1인칭/3인칭 표시 선택은 `PlayerWeaponViewController`, 실제 선택 슬롯과 미러 표시는 `PlayerItem`/`WeaponHolderModel`이 같은 결과를 따른다. 기능 코드에서 홀더를 별도로 강제 활성화하지 않는다.

## 여러 사람이 작업할 때

1. 새 스크립트는 작성자 이름 폴더 대신 해당 기능 폴더에 넣는다. 새 화면은 기존 컴포넌트 연결과 재사용 가능 여부를 먼저 확인한다.
2. MonoBehaviour 파일 이동은 `.cs`와 `.meta`를 함께 한다. GUID를 새로 만들지 않는다. 클래스/필드 이름 변경은 별도 직렬화 마이그레이션으로 다룬다.
3. `TESTPlayerItem`, `TESTPlayerMovement`, `TESTWeaponADS`, `TEST_Wepon` 등 일부 코드는 실제 훈련장/튜토리얼 씬·프리팹에 연결되어 있다. 이름만 보고 지우지 않는다.
4. 공통 네트워크/입력/씬 수명 코드를 변경할 때 관련 기능 담당자와 범위를 공유한다. 한 커밋에서 경로 이동과 동작 변경을 혼합하지 않는 것을 원칙으로 한다.
5. 기존 CP949 파일은 인코딩을 보존한다. 한글 전체가 깨지는 일괄 UTF-8 변환을 하지 않는다.
6. UI 구조는 저장된 씬/프리팹에서 제작한다. 런타임 코드는 값, 표시 상태, 연출을 갱신한다.
7. 서버 무기 데이터 생성 도구는 기존 main 경로와 새 YSL 경로를 모두 지원한다. 생성된 피해/상점 수치 파일을 직접 조정하지 않는다.

## 검증

- `python Tools/verify_module_layout.py`: 이동한 532개 GUID, 원래 위치에 중복 파일이 남았는지 확인.
- Unity batch `-executeMethod SceneLifecycleVerification.RunAll`: Core, 씬 역할/참조 수명, 장비 숨김, 구매 패킷, 이동 보간, 스폰 방향, ESC/리스폰, 디바이스, 스킬 복귀 검사.
- `RankedCaptureTestEditor.Build`: Build Settings의 활성 씬으로 실제 Windows 개발 빌드. `VECTOR_CAPTURE_ROOT`는 저장소 밖 출력 폴더로 지정.
- 개발 빌드 `-verify-scene-transitions`: 로그인 → 로비 → 픽 진입 → 로비 → 결과 → 랭크 결과 → 훈련장 → 로비 → 로그인 반복 검사. 계정 로그인이나 보상 지급은 하지 않는다. 실제 매칭/6인 전투 검증을 대신하지 않는다.
- `python Tools/run_scene_smoke.py --client <개발빌드.exe> --log <외부로그경로>`: 위 검사를 계정 데이터 없이 실행한다. 9000 포트에 응답 데이터를 만들지 않는 로컬 TCP 연결만 제공한다. 실제 서버가 포트를 점유하고 있으면 실행하지 않는다.
- `SavedSceneInventory.Run`: `Assets/Scenes`, `Assets/KSW`의 저장 씬 전체를 열어 빌드 포함 여부, 연결된 기능, Missing Script 수를 외부 JSON에 기록한다. 기존 `SystemSample`의 `Enemy`가 참조하던 삭제된 PlayerHP GUID는 같은 클래스의 현행 스크립트로 다시 연결했다. 같은 누락이 있던 이전 `Player` 프리팹도 함께 복구했다.
- `SceneScriptReferenceBuildGuard`: 배포 씬에 불안정한 scene-local MonoScript가 들어가면 빌드 차단.

Unity가 프로젝트를 열고 있으면 같은 프로젝트에 별도 batch 에디터를 띄우지 않는다. 검증 로그와 빌드는 저장소 밖에 둔다.

### 2026-10-29 확인 결과

- 기존 스크립트 532개 이동, GUID 유지 검사 통과.
- Core/구매/표시 소유권 83항목, 씬 정책/참조 수명 74항목 통과.
- 저장된 19개 씬 검사, Missing Script 0개. 배포 씬과 비배포 씬을 모두 열어 확인했다.
- 실제 Windows 개발 빌드 성공, 6개 배포 씬 사이 9회 이동 완료. 테스트용 로컬 TCP 연결만 사용했으며 로그인 계정이나 가짜 경기 결과를 만들지 않았다.
- 4캐릭터 장비 전환 Play 검사, 설치/해체 복귀, 사망/리스폰, 스킬 종료, ESC 설정 유지, 두 맵 스폰 방향, 이동 보간 회귀 검사 통과.
- 서버의 리포트 계약, 호감도 계약, 무기 피해/상점 수치 생성 결과 일치 검사 통과.
- 이번 검증은 새로 6인 경기를 끝까지 진행하거나 외부 PC에서 음성을 주고받은 검증은 아니다. 해당 검증 결과로 확대 해석하지 않는다.

## 이어서 분리할 책임

이번 구조 정리는 전체 동작을 갈아엎는 재작성은 아니다. 여전히 네트워크 수신부의 일부 UI 직접 호출, 플레이어 전역 탐색, 기존 훈련장 전용 구현이 남아 있다. 각 기능의 서버 계약과 실제 씬 회귀 검증을 확보하면서 서비스/어댑터로 옮겨야 한다. 파일 수를 줄이려고 서로 다른 훈련장/온라인 규칙을 섣불리 합치지 않는다.
