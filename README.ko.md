# 코드네임 바이스 · CodeName VAIS

[English](README.md) · [Korean](README.ko.md) · [Japanese](README.ja.md)

팀으로 개발 중인 멀티플레이 FPS. C++ 서버와 경기 상태를 Unity의 플레이 경험에 연결합니다.

**C++ · Windows IOCP · C# · Unity** · 현재 개발 중

**2026.03 - 현재 · 10명 · 소프트웨어 5명, 그래픽 5명**

서버 개발·Unity 멀티플레이 연동

[개발 사례 보기](https://ysl-ryudo.github.io/ko/work/vector/) · [Portfolio](https://ysl-ryudo.github.io/ko/)

[ProjectVectorServer](https://github.com/YSL-RyuDo/ProjectVectorServer) (비공개 · 접근 권한 필요) · [projectVector](https://github.com/YSL-RyuDo/projectVector) (비공개 · 접근 권한 필요)

## 시스템별 담당 내용

| 영역 | 구현 |
|---|---|
| 연결·통신 | IOCP · 세션 · 패킷 디스패치 |
| 게임 서비스 | 방·경기 수명주기, 커리어·프로필·상점·소셜 데이터 |
| Unity 연동 | 게임플레이 패킷, 로비·결과, Sentinel 능력과 음성 권한 |
| 복원 경계 | 최신 지속 표시 상태 보관 · 게임 행동 재실행 제외 |

## ReconnectJournal

재접속한 플레이어에게 무엇을 다시 보여주고, 어떤 게임 행동은 반복하지 않아야 할까?

1. **복원할 상태 선택** — Observe는 명시된 지속 표시 패킷을 수집합니다. 피해·보상·플레이어 명령은 복원 기록에 포함하지 않습니다.
2. **최신 상태로 교체** — 패킷 종류와 개체 ID를 키로 저장합니다. 후속 갱신은 같은 항목을 교체하므로 행동 이력을 순서대로 재생하는 방식과 구분됩니다.
3. **수명주기 변경 반영** — 새 경기·라운드 패킷은 기록을 초기화합니다. 제거 패킷은 대응하는 개체 상태를 지우고, 서로 배타적인 상태는 이전 항목을 정리합니다.
4. **남은 시간으로 복원** — Snapshot은 저장된 지속 시간에서 steady clock 경과 시간을 빼고 0 이상으로 제한합니다. 0초 상태 패킷도 전달해 클라이언트의 오래된 상태를 해제합니다.

정식 명칭은 CodeName VAIS이며, Project Vector는 이전 임시 이름입니다. 저장소 이름은 유지합니다.

## 개발 시연 영상

- [Lobby UI](https://www.youtube.com/watch?v=WqRBn-yWVdo)
- [Store system](https://www.youtube.com/watch?v=OBFWkZE2rzo)
- [Profile system](https://www.youtube.com/watch?v=A3CkQvnQEGI)
- [Earlier Project Vector demo](https://www.youtube.com/watch?v=oga4ai_1Ugg)

## 내 기여

- 센티널을 포함한 캐릭터 스킬의 패킷 처리와 게임플레이 연동을 구현했습니다.
- 매칭과 경기·결과 연출, 커리어·프로필 데이터, 친구, 상점 연동, 재접속·음성 클라이언트 흐름을 작업했습니다.
- 팀원이 제공한 컨트롤러 모델·모션 통합, ADS 정렬, 훈련장 확장과 에디터 검증 도구를 작업했습니다. 통합 작업을 모델·애니메이션 원작 제작으로 주장하지 않습니다.

## 기여 근거

- [Sentinel / 스킬 / スキル](https://github.com/YSL-RyuDo/projectVector/commit/756c9a09f32ed4a073d5d3d60008b1aaa44f0f60)
- [Practice & ADS / 훈련장·조준 / 訓練場・照準](https://github.com/YSL-RyuDo/projectVector/commit/f4457e2d77c251b9e0da5d976927303d04a64b43)
- [Reconnect & voice / 재접속·음성 / 再接続・音声](https://github.com/YSL-RyuDo/projectVector/commit/b62cbeeb16e72738d2dd7c491c8d42ec31afcd18)
- [Additional YSL work / YSL 추가 작업 / YSLの追加作業](https://github.com/YSL-RyuDo/projectVector/commit/5e5c3c53b010fb2807f6b53c0eb0f5ceec8d1350)

커밋은 설명한 변경의 근거이며 기여율 점수가 아닙니다. 외부 에셋과 팀원 작업의 크레딧은 구분합니다.

## 실행·코드 확인

[ProjectVersion.txt](ProjectSettings/ProjectVersion.txt)의 Unity 버전으로 프로젝트를 여세요. 패키지 잠금 파일과 직렬화된 에셋을 유지하고, Unity를 열기 전에 문서에서 설명한 브랜치를 선택하세요.

- Unity scene: [Assets/Scenes/Login.unity](Assets/Scenes/Login.unity)

[ProjectVectorServer](https://github.com/YSL-RyuDo/ProjectVectorServer)

## 개발·검증 범위

현재 개발 중입니다. 문서 기준 `YSL`에는 `main`에 아직 없는 이동·조준·명중 피드백·원격 움직임 작업이 있으며, 최신 main 변경을 모두 포함한 상태도 아닙니다. 시연 시 코드 버전을 명확히 선택해야 합니다. 기존 기록의 에디터·빌드 검사와 두 PC 실제 음성 통화·두 클라이언트 경기 검증은 구분합니다. 동시 접속 수용량이나 상용 운영 준비 완료를 주장하지 않습니다.

## 상세 문서

- [Docs/NextWorkSession.md](Docs/NextWorkSession.md)
- [Docs/ShopGachaImplementation.md](Docs/ShopGachaImplementation.md)
