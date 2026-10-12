# Project Vector 상점 / 보급 연출

## 데이터 경계

기존 인게임의 전체 무기·스킨 테스트 보유 상태는 변경하지 않습니다. 상점 소유권만 별도로 관리하며, 재화 0/1은 기존 CareerWallet과 MoneyManager를 함께 사용합니다. 캐릭터/무기 장착, 프로필·명예 시스템은 보존합니다. 현금 결제/충전 기능은 없습니다.

서버의 `shop_catalog.csv`가 상품 ID·등급·가격을 정의합니다. 소유권·거래 기록은 `career_user_wallet.csv`의 새 `shopState` 열에 저장하며 기존 재화 잔액과 원자적으로 저장합니다. 이전 3/4/5열 지갑도 읽습니다. 최초 접근 시 기존 `shop_accounts/<계정ID의 UTF-8 hex>.txt`에서 소유권과 기록만 가져오고 별도 상점 잔액은 합산하지 않습니다. **이 폴더는 이전 사용자 데이터이므로 삭제하면 안 됩니다.**

상점 진입에 따른 별도 재화 지급은 없습니다. qwer를 포함해 기존 게임 지갑 잔액을 그대로 사용합니다. 신규 상점 보유 목록만 빈 상태에서 시작합니다. 독립 ShopService 테스트의 초기 지급 설정은 실제 서버 거래 경로에서 사용하지 않습니다.

현재 연결된 에셋은 무기 스킨 66개와 캐릭터 기본 외형 4개입니다. 별도의 캐릭터 코스튬 모델이 추가되면 카탈로그/모델 매핑을 확장해야 합니다. 등급과 가격은 테스트용 초기 설정입니다.

## 요청 → 확정 → 연출

획득 기록 화면은 최신 20건만 보여줍니다(페이지당 10건, 최대 2페이지). 서버가 페이지 요청을 제한하며 클라이언트도 세 번째 페이지를 요청하지 않습니다. 오래된 거래 원장은 같은 요청의 재결제/중복 지급 방지에 사용하므로 삭제하지 않습니다. 이 변경은 표시·전송 범위 제한이며 저장 용량 상한은 아닙니다. 저장 용량 제한은 거래 ID 중복 방지 규칙을 유지하는 별도 보관 설계가 필요합니다.

`VectorShopPanel.Transact` → `NetworkManager.SendShopRequest` → 서버 `CareerManager.ExecuteShop` → `ShopService.Engine.Execute` → 지갑·소유권·기록 원자적 저장 → 응답 → MoneyManager 갱신 → `GachaManager.Play` → 결과 UI.

서버만 추첨하고 재화를 차감합니다. 연출에서 난수를 돌려 보상을 바꾸지 않습니다. 재요청에는 최초 요청 ID와 재화 종류를 그대로 사용하며, 서버는 저장된 동일 결과를 반환합니다. 건너뛰기/상점 닫기는 확정 결과를 취소하지 않습니다.

| 단계 | 코드 | 기본 시간/동작 |
|---|---|---|
| Prepare | `Run`, `Wait` | 0.35초 준비, 단계 안내 및 스트링 서열 입력 없음 |
| Warp | `Animate`, `CreateStage`, `SpeedLines` | 2초 전진, FOV 52→78, 밝은 금속 육각 터널과 기계식 게이트 |
| RingHits | `Run`, `Burst` | 스피드라인 종료, FOV 78→54를 0.35초에 안정화, 구체당 0.52초 이동 + 타격 |
| Materialize | `VectorShopPreview.Reveal` | 신화 대표 보상 1개 실체화 2.1초 + 감상 1.1초, 밝은 회색 배경 |
| Summary | `Finish`, `ShowResults` | 1/10개 결과 카드·중복 환급 표시 |

모든 타이머는 unscaled 시간입니다. `Time.timeScale=0`에서도 진행합니다. `Skip`은 실행 코루틴을 중지하고 임시 오브젝트·RenderTexture·Material을 해제한 뒤 결과를 표시합니다. Cinemachine 3의 카메라는 수동 평가하며 기존 로비/인게임 CinemachineBrain에 등록하지 않습니다.

Timeline으로 옮길 경우 위 단계들을 클립/Signal로 표현하되, 결제/추첨은 Timeline 밖 서버 요청에 유지합니다. Seek/Skip으로 보상을 다시 지급하면 안 됩니다.

## 구체 타이밍과 파티클

구체의 이동은 `t = elapsed / 0.52`, `position = Lerp(from, target, t*t)`입니다. 위치를 목표점에 정확히 맞춘 뒤 `Burst`를 호출하므로 별도 충돌체/물리 이벤트가 필요 없습니다. 링은 UI 위에 RenderTexture로 합성되는 **3D 링**입니다. Canvas 픽셀 좌표를 그대로 월드 좌표로 사용하지 않습니다.

DOTween 대안은 `DOLocalMove(target, 0.52f).SetEase(Ease.InQuad).SetUpdate(true)`이며 OnComplete에 타격을 연결하고 취소 시 Tween을 Kill해야 합니다. 현재 구현은 코루틴만 사용합니다.

- 스피드라인: Circle 반경 5, 두께 0.2, 초당 85개, 수명 1.5초, 속도 45, 크기 0.018, 최대 300개. Stretch Billboard, Length Scale 5, Velocity Scale 0.15. 링 타격 진입 시 정리합니다.
- 타격: Sphere, 60개 즉시 Emit, 수명 0.5초, 속도 8, 크기 0.1. 등급 색상은 서버 결과에 대응합니다.
- 신화 타격: 0.24초 감쇠 플래시. 반복 스트로브는 사용하지 않습니다.
- `VectorEffects.shader`는 버텍스 색상을 받는 가산 Unlit입니다. Resources에 두어 빌드에서 셰이더가 누락되지 않게 합니다. Bloom이 없어도 링/파티클 색상은 표시됩니다.

## 홀로그램 → 실체화 / Shader Graph 대체 구성

현재 동작 코드는 URP용 `Resources/Shop/VectorHologram.shader`입니다. 원본 에셋 머티리얼은 수정하지 않습니다. 미리보기 인스턴스에 임시 머티리얼을 적용하고 종료/취소 시 원래 머티리얼을 복구합니다. 이 효과는 **격자·림 기반 홀로그램**이며 실제 삼각형 토폴로지 와이어프레임은 아닙니다.

Shader Graph로 같은 효과를 제작할 때: Universal Unlit Graph, Surface Transparent, Alpha blending, Render Face Both, Depth Write Off.

| 속성 | 기본값 | 역할 |
|---|---|---|
| `_BaseMap` Texture2D | 원본 텍스처 | 실체화되는 외형 |
| `_GlowColor` HDR Color | 등급 색 ×2 | 홀로그램/디졸브 경계 |
| `_Reveal` Float 0..1 | 0 | 코루틴이 1까지 변경 |
| `_GridScale` Float | 35 | 격자 빈도 |

노드 연결:

1. Position(Object) → Multiply(GridScale) → Fraction → Subtract(0.5) → Absolute → Split → Minimum(X,Y,Z) → Smoothstep(0.015,0.04) → One Minus = Grid.
2. Normal Vector(World)와 View Direction(World)를 Normalize → Dot → Saturate → One Minus → Power(2) = Rim.
3. Position(Object)에서 Simple Noise(또는 3D 노이즈 Custom Function) = Noise. `_Reveal × 1.1`을 `Smoothstep(Noise, Noise+0.08)`에 연결 = SolidMask.
4. `Abs(Noise - Reveal)` → Multiply(25) → One Minus → Saturate = Edge.
5. Position.Y ×160 − Time×12 → Sine ×0.2 +0.8 = Scan.
6. `(Grid + Rim + Edge) × GlowColor × Scan` = Hologram.
7. Sample Texture2D(BaseMap)의 RGB와 Hologram을 SolidMask로 Lerp → Base Color. Edge×GlowColor×0.3을 더합니다.
8. Alpha는 `Lerp(Saturate(Grid+Rim)×0.8, BaseMap.a, SolidMask)`입니다.

실제 삼각형 Wireframe이 필요하면 각 삼각형의 barycentric 좌표를 별도 UV/Color 채널에 기록한 전용 **프리뷰 메쉬**가 필요합니다. 공유 게임 메쉬를 수정하지 마세요. 현재 텍스처 복원과 원본 Lit 머티리얼 복귀 사이에 조명 차이가 생길 수 있으므로 최종 아트 폴리싱에서는 원본 Lit Graph에 동일 Reveal 마스크를 통합하는 편이 좋습니다.

## 보상 상세 / 확률

UI는 `Assets/Scenes/Lobby.unity`의 기존 `Store Panel` 아래에 저장됩니다. 플레이 전에도 하이어라키에서 패널을 켜고 배치·색상·폰트·버튼 연결을 편집할 수 있습니다. `VectorShopPanel`은 저장된 참조의 텍스트/상태만 갱신하며 런타임에 UI를 생성하거나 재배치하지 않습니다. `GachaManager` 역시 저장된 `GachaSequencePanel`을 사용합니다. 런타임 생성 대상은 미리보기 모델과 3D 연출용 임시 스테이지뿐입니다.

- `RewardDetailsPanel`: 왼쪽 등급별 카드 목록, 중앙 `SelectedRewardModel`, 오른쪽 `RewardDescription`.
- `DrawHistoryPanel`, `DetailedProbabilityPanel`, `DrawResultPanel`, `PurchaseConfirmPanel`: 저장된 팝업.
- `GachaSequencePanel`: 저장된 화면, 플래시, 홀로그램 미리보기, 건너뛰기 버튼.
- 기존 `Main Panel`, `Profile Gacha Panel`, `Exchange Store Panel`, `Weapon Skin Gacha Panel`, `Money Group`은 보존됩니다.

앞으로 UI 수정도 씬/프리팹에 저장해야 하며, 런타임 생성 코드로 대체하거나 기존 UI를 일괄 비활성화하면 안 됩니다. `Project Vector > UI > Install Saved Shop Panels`는 최초 편집용 설치 메뉴이며 이미 설치된 씬을 재생성하지 않습니다.

왼쪽은 신화·희귀·고급별 보상/등급 확률/개별 확률, 중앙은 선택한 프리팹, 오른쪽은 이름·종류·보유 여부·가격입니다. 좌클릭 드래그로 중앙 모델만 Y축 회전합니다. 카메라와 모델은 별도 임시 스테이지를 사용합니다.

현재 테스트 확률은 신화 5%, 희귀 24%, 고급 71%이며 등급 내 균등 확률입니다. 매 회 독립 추첨, 천장 없음. 1회 재화0 300 또는 재화1 1. 10회는 각각 3,000/10. 중복은 재화0으로 신화150/희귀75/고급30 환급합니다. 다른 확률로 변경하려면 서버 추첨·클라이언트 표시를 함께 수정해야 합니다.

## 검증과 실행

서버와 클라이언트 양쪽 변경이 모두 필요합니다. 서버는 기존 CSV들과 함께 `shop_catalog.csv`가 있는 작업 디렉터리에서 실행합니다. 구버전 서버는 새 요청340에 응답하지 않습니다. 새 응답은341입니다.

- Unity 메뉴 `Tools > Project Vector > Verify Shop Assets and Protocol`: 셰이더·모델·아이콘·패킷 길이·확률합 검사. 씬을 저장하거나 보유 데이터를 변경하지 않습니다.
- 서버 `Tests/ShopServiceTests.cpp`: 별도 임시 디렉터리에서 최초 지급·비지급·재화 부족·구매·영속성·동시 재요청·저장 실패 검사.
- Unity를 종료한 상태에서 `-batchmode -projectPath <프로젝트> -executeMethod VectorShopPlayVerification.RunBatch -logFile <로그>`: 저장된 Lobby UI로 좌클릭 회전·10개 타격·홀로그램·건너뛰기·정리 검증. 로그인/매치 컨트롤러는 검사 중 메모리에서만 비활성화하며 씬을 저장하지 않습니다.
- `Tools > Project Vector > Rebuild Shop Thumbnails`: 기존 모델에서 70개 썸네일을 상점 전용 단일 아틀라스로 생성합니다. 원본 스킨 아이콘을 변경하지 않습니다.
- 실제 플레이 회귀 확인: 프로필 최근 방문자/교체/스킨 선택, 명예 hover, 기존 무기 장착, 상점 열기/닫기 반복, 1회/10회·Skip·ESC·재접속, 보상 상세 좌클릭 회전, 로비 카메라 복귀.

제공된 스크린샷·분석에 더해 로컬 MP4의 구간별 프레임을 추출해 금속 터널, 기계식 링, 밝은 실체화 공간을 확인하고 반영했습니다. 원작의 전용 모델·음원·시네마틱을 복제한 것이 아니라 현재 프로젝트 에셋으로 구현한 연출입니다.

API 참고: [Cinemachine 3.1 CameraState](https://docs.unity.cn/Packages/com.unity.cinemachine%403.1/api/Unity.Cinemachine.CameraState.html), [Shader Graph Fresnel](https://docs.unity.cn/Packages/com.unity.shadergraph%4017.2/manual/Fresnel-Effect-Node.html). 실제 프로젝트의 Cinemachine 3.1.6 소스/API로 컴파일 검증합니다.

### 이전 보상 상세 / 가챠 단계 검증 결과

- C++ Debug x64 서버 빌드 성공(기존 경고는 남아 있음).
- 클라이언트 Editor/Player 정의의 C# 컴파일 성공.
- Unity 배치 에셋 검사: 셰이더 2개, 캐릭터 4개, 무기 스킨 66개/썸네일, 패킷 파싱·확률합·잘린 패킷 거부 통과.
- 저장된 Lobby UI 플레이 검사 통과: 카드 70개/결과 슬롯 10개 참조, 좌클릭 회전, 워프·링 타격·실체화, Skip, 임시 스테이지 정리, 종료 후 UI 보존. 로그인/매치 컨트롤러를 분리한 검사이며 실제 서버 로그인 회귀 검증을 대체하지 않습니다.
- 씬 오브젝트 ID 비교: 기존 오브젝트 삭제 0개. 기존 상점 섹션 5개와 버튼 참조 보존.
- 서버 거래 단위 테스트: 재화 분리, qwer 최초 지급, 재화 부족, 잘못된 횟수 거부, 재접속 후 동일 결과, 중복 요청, 동시 요청, 저장 실패 시 미차감 통과.
- 이번 작업에서 실제 로그인 서버와 완성 Windows 클라이언트 5개를 연결하는 종단간 검증은 수행하지 않았습니다. 원작과의 영상 단위 시각 일치도는 보장하지 않습니다.

## 전체 상점 레이아웃 수정 (2026-09-29, 씬 저장 및 격리 플레이 검증 완료)

`VectorShopLayoutEditor.Apply`는 기존 상점 버튼/패널을 보존하면서 추천 배너와 스크롤, 5개 상점 탭, 왼쪽 분류와 4열 스킨 상품 목록, 오른쪽 뽑기 버튼과 보상/기록/확률 메뉴를 편집 시점에 작성하고 Lobby에 저장합니다. 유료 충전·VIP·현금 가격·유료 멤버십은 추가하지 않습니다. 프로젝트의 기존 이미지와 모델 썸네일을 사용합니다.

런타임 `VectorShopNavigation`은 저장된 오브젝트의 표시, 분류, 가격 정렬과 데이터만 갱신합니다. 재화는 기존 상단 MoneyManager 표시를 사용하며 기존 프로필/교환 패널은 보존합니다. 상점 소유권은 기존 인게임 전체 보유 상태와 연결하지 않습니다.

사용자가 Unity를 종료한 뒤 `Apply`와 명시적 `Polish`를 실행하여 Lobby에 저장했습니다. 실행 전 씬 사본은 임시 폴더에 보존했습니다. 적용 전후 직렬화 오브젝트 ID 비교에서 기존 오브젝트 삭제는 0개였습니다. 원래 배너의 하위 이미지/제목 좌표를 보정하고, 기존 무기 미리보기 카메라를 포함한 1920×1080 렌더 캡처를 확인했습니다.

최종 `VectorShopPlayVerification.RunBatch` 결과: 5개 탭, 70개 상품 참조/콜백, 캐릭터 4개·무기 66개 필터, 가격 정렬, 상품→상세 이동, 기존 섹션 이동, 좌클릭 모델 회전, 워프·링·실체화, 10개 타격, Skip 및 임시 스테이지 정리 통과. 검증은 가상 서버 응답을 사용하는 격리 검사이며 실제 계정/재화를 변경하지 않았습니다. 실제 서버 로그인·거래 종단간 검증은 수행하지 않았습니다.

검증 로그: `%TEMP%/VectorShopReferencePlay.log`. 화면: `%TEMP%/VectorShopRecommendation.png`, `VectorShopCatalog.png`, `VectorShopDrawLayout.png`, `VectorShopSavedHierarchy.png`. 상단 닉네임/재화가 기본값으로 나오는 것은 로그인 컨트롤러를 분리한 테스트 조건입니다. 레퍼런스의 배치 구조를 기존 프로젝트 에셋에 맞게 적용했으며 원작 아트나 모든 이벤트 페이지를 복제한 것은 아닙니다.

현재 씬에는 이미 적용되어 있으므로 다시 설치할 필요가 없습니다. 최초 설치 도구는 `Project Vector > UI > Apply Reference Shop Layout`이며 자동 실행되지 않습니다. 저장되지 않은 Lobby가 있으면 적용을 거부합니다. 이후 수정은 저장된 Hierarchy에서 진행합니다. 로그인/매치 컨트롤러를 분리한 검사이므로 실제 서비스 종단간 검증은 추가로 필요합니다.

## 재화 통합 / 로컬 영상 반영 (2026-09-29 후속 수정)

- `RefinePresentation`으로 중복 잔액·개발용 안내를 숨기고 비용/상태/상단 잔액 배치를 수정하여 Lobby에 저장했습니다. UI는 계속 Hierarchy에서 편집합니다. 기존 교환용 재화 2는 유지합니다.
- `VectorGachaChamber`의 금속 벽면·레일·게이트, 약한 로컬 Bloom, 구체 궤적·타격 스파크, 밝은 홀로그램 배경을 적용했습니다. 임시 연출 재질/메쉬/볼륨은 종료 시 정리하며 원본 모델과 전역 렌더 설정은 변경하지 않습니다.
- `Tests/SharedShopWalletTests.cpp`: 기존 소유권 이관, 동일 지갑 소비, 경기 보상 후 최신 잔액, 재접속, 동시 재요청 중복 차감 방지, 저장 실패 롤백 통과. 모든 테스트는 별도 임시 데이터로 실행했습니다.
- 서버 Debug x64 빌드와 Unity 배치 플레이 검사 통과. 마지막 로그는 `%TEMP%/VectorShopChamberPlay.log`입니다. UI 이동·회전·전체 단계·10회 타격·Skip·정리를 검사했습니다.
- 실제 운영 서버 재시작이나 실제 계정 거래는 수행하지 않았습니다. 적용 시 서버와 클라이언트를 함께 갱신해야 합니다. 지갑 CSV 형식이 확장되므로 배포 전 기존 계정 데이터 백업을 보존하세요.
