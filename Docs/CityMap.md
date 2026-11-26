# City / 도시 — 성현 맵 인게임 연결

성현님의 `Development/Maps/CityPrototype/City.prefab`을 실제 매치에서 선택하고 불러오도록 연결한다. 별도의 도시맵을 새로 만든 것이 아니다.

- 게임 프리팹: `Assets/ProjectVector/Shared/Maps/Prefabs/City.prefab`. 원본의 프리팹 Variant로 지형, 충돌, 공격/수비 각 3개 스폰, 데스매치 12개 스폰, A/B 설치 구역 및 구매 장벽을 유지한다.
- 원본의 `ASite`/`BSite` 마커를 미니맵에 연결한다. 지형이 사용하는 Ground 레이어를 도시맵 미니맵에만 추가하고 다음 맵에서는 원래 카메라 마스크로 복원한다.
- 사설방의 저장된 맵 선택 UI에 **도시**를 추가한다. 네트워크 ID는 `City=3`이며 기존 TestMap=0, Desert=1, Construction=2를 유지한다.
- 일반/랭크 랜덤 풀은 TestMap, Desert, City다. Construction은 사설방 전용을 유지한다.
- `CityPresentation.prefab`은 픽/맵 소개/결과 화면용으로 저장한 복사본이다. 플레이 로직과 Collider를 제거하며 게임 맵과 별도로 레이어 31을 사용한다.
- 서버에 City 이름/ID, 팀 핑, 스폰, 설치 구역, 차폐 지형을 등록한다. 클라이언트와 서버 변경을 함께 사용해야 한다.

## 원본 맵 수정 후 갱신

Unity가 프로젝트를 열고 있지 않을 때만 배치 모드에서 `CityMapIntegration.InstallAndVerify`를 실행한다. 게임 Variant와 화면용 프리팹을 갱신하고 저장된 씬 연결을 검증한다. 변경 없이 확인하려면 `CityMapVerification.Run`을 사용한다.

`CityMapIntegration.VerifyAndExport`는 검사 후 임시 디렉터리에 다음 파일을 내보낸다. 서버 저장소의 동일 이름 생성 헤더를 갱신하고 다시 빌드한다.

- `VectorPlantZones.generated.h` → `PlantZones.generated.h`
- `VectorMovementSpawns.generated.h` → `MovementSpawns.generated.h`
- `ScannerOcclusion.generated.h`
- `ScannerOcclusionCases.csv` — 서버 차폐 검증 입력

## 검증

- Unity `CityMapVerification.Run`: 원본 연결, 스폰/설치 구역, 저장된 선택 버튼, 실제 MatchManager 로딩, 미니맵, 픽 소개, 결과 배경 등 51개 검사.
- 서버 `CityMapTests.cpp`: 맵 ID, 선택 권한, 일반/랭크 풀, 원본 스폰/설치 구역/차폐 지형.
- 서버 `ScannerDetectionTests.cpp`: Unity와 서버의 4개 맵 차폐 판정 16,000개 및 실제 스캐너 패킷 처리. 장벽 활성/비활성 포함.
- 서버 `ConstructionMapTests.cpp`: 기존 공사장 선택과 스폰/설치 구역/장벽 회귀 검사.
- 서버 `Tests/CityMapIntegration.py <server.exe>`: 임시 계정으로 6명 방 입장, 도시 선택, 픽, 로딩 완료, 원본 좌표 이동, 팀 핑 검사. 포트 9000이 이미 사용 중이면 중단하며 운영 계정 데이터는 사용하지 않는다.

검사는 기능 연결을 확인한다. 6명 Unity 클라이언트를 이용한 장시간 플레이 및 맵 밸런스 검증을 대체하지 않는다.
