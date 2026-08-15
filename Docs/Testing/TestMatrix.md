# 변경 유형별 테스트 매트릭스

| 변경 유형 | Static | EditMode | PlayMode | Content/Visual | WebGL/Browser |
|---|---:|---:|---:|---:|---:|
| Core 값/규칙 | 필수 | 필수 | 영향 시 | - | 마일스톤 시 |
| 입력/명령 변환 | 필수 | 변환 규칙 | 필수 | UI 표시 | 필수 |
| MonoBehaviour 연결 | 필수 | Core 영향 시 | 필수 | 씬 확인 | 기능 완료 시 |
| 프리팹/ScriptableObject | 참조/호환 | validator | 필수 | 필수 | 포함/로딩 확인 |
| 방 콘텐츠 | 메타데이터 | 연결성/seed | 필수 | 필수 | 마일스톤 시 |
| VFX/카메라/UI | 할당/참조 | - | 필수 | 필수 | 성능/가독성 |
| WebGL 설정 | 설정 diff | - | Editor 기준 | - | build+browser 필수 |
| 패키지 변경 | 사용처/asmdef | 전체 | 전체 | 영향 자산 | build+browser 필수 |
| Unity 버전 변경 | 전체 diff | 전체 | 전체 | 대표 씬 | build+browser 필수 |
| 문서만 변경 | 링크/중복 | - | - | - | - |

## 핵심 회귀 묶음

- 폭발 벽 차단과 파괴 벽 종료.
- 모든 폭탄 종류 연쇄와 중복 예약 방지.
- 설치 직후 통과 권한 종료.
- 두 슬롯 독립 쿨타임과 비활성 회복.
- 피해 무적과 사망 단일 발생.
- run 체력의 방 이동·재입장 persistence, Recovery의 상한 `+2`·최대 체력 비소비·노드별 단일 소비·terminal 거부와 무적 상태 불변.
- 보스 Telegraph→Execute→Recovery exact boundary, 예고/실행 셀 동일성, 4칸 이상 폐쇄 cardinal 이동 route 검증, 목적지 danger 포함, 한 칸 이동·actor 차단 재시도·bomb 동시 점유와 제거 독립성, Recovery 한정 폭탄 피해, 안전한 phase 전환과 사망 점유 단일 제거.
- Unity 보스방 단일 활성과 `LureLoop` 전달, 패턴 피해와 기존 무적 공유, 위험 셀·목적지 ghost의 Telegraph/Execute 표시, pause 중 이동 보간 정지, 선행 설치 적중과 네 이동·4회 반격, 2페이즈·격파·단일 방 클리어와 실제 WebGL 가독성.
- 플레이어 HUD의 초기/피해/회복/사망 체력 snapshot과 bar 비율, 일반 방의 보스 panel 비표시, 보스 HUD의 초기/피해/2페이즈/격파 반영, 열 씬의 단일 HUD·session 참조와 실제 WebGL 배치 가독성.
- 보스방 도착과 클리어 완료 구분, 완료 UI 단일 표시와 전투 정지, 완료 전·pending 중 재시작 거부, 같은 seed의 새 run state·시작방·초기 한 슬롯 복구, WebGL `R` 재시작.
- 갑옷 적의 서로 다른 폭발 2회, 첫 피격 상태·cadence 변화, 같은 `BombId` 중복 차단과 두 번째 사망 뒤 점유 단일 제거.
- 방 경계·연결성, 서로 다른 첫 이동의 퇴로 2개, 닫힌 유도 경로와 씬 표현 일치.
- 중앙 게이트 방의 고정 장벽 8셀·파괴 문 2셀·좌우 우회 연결, 추격자 단일 구성, 논리/시각 파괴 벽 수 일치와 실제 WebGL 진입·클리어·HUD 비중첩.
- 동일 버전·정의·seed 던전 재현과 golden snapshot, 4~5 전투방·첫 보상·보스 전실/보스 경로·선택 전투 가지·보스 근처 단일 Recovery leaf·연결 트리·암시적 좌표 루프 방지.
- 던전 방향별 이웃, 첫 전투 잠금·클리어 전 퇴실 차단, 안전방 비잠금, 클리어 방 양방향 재방문과 전체 트리 왕복.
- 미니맵 snapshot의 시작 `2방/1연결`, 이동 뒤 현재·방문·직접 인접 frontier, 미방문 방 종류·그 너머 연결 비공개, 안정 순서·수정 불가 컬렉션. 열 scene presenter·binder 단일 참조, 토큰 HUD 하단 배치와 WebGL 시작·Recovery·보스 전실·새 run 초기화 marker/캡처.
- 던전 전투방 배정의 catalog 순서 무관 재현, 활성 출구·회전 호환, 사용 균형, seed 다양성과 호환 콘텐츠 부족 실패.
- 전투 노드 5개·전투방 정의 5개인 그래프에서 각 정의를 정확히 한 번 사용하는 배정.
- Unity 던전 카탈로그의 room asset·씬 매핑, 중복·누락 경계, Core 배정 선택 해석과 잠금·클리어·왕복 위임.
- 던전 문의 북·동·남·서 안정 순서, 비연결 `Inactive`, 미클리어 연결 `Locked`, 클리어 뒤 동일 대상 `Open`, 전투방 활성 출구 배정과 Unity 런 snapshot 일치.
- 전투방 0/90/180/270도 전체 셀·출구 회전과 크기 교환, 적 비활성 placeholder의 이동·폭탄·체력 재사용, 적 actor 미생성과 `Awake` 전 runtime spawn 준비.
- 특수방 catalog의 필수 타입·고유 씬, 로드 전 Core 미변경, 중복·로드 불가·씬 불일치 거부, 기대 씬 뒤 단일 commit, persistent host primary 단일성.
- pause 진입 시 이동 의도 해제, 입력·설치·교체 차단, 논리 시계·fuse·쿨타임·적·보스 정지, UI 표시, 재개 시 유지 방향 재샘플링과 WebGL 실제 상태 marker.
- WebGL focus 상실 뒤 입력 stuck 없음.
- 게임패드 binding 구조, 합성 Input System 스틱·D-pad·버튼→의미 명령, WebGL 표준 가상 장치 연결→스틱·D-pad 해제·이동 중 분리 정지·동일 index 재연결 복구·South 설치와 실패·West 교체 명령·Start pause 차단/유지 스틱 재개·Select 재시작. 물리 장치별 연결과 조작감은 수동 확인.
