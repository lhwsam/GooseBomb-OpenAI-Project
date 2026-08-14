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
- 보스 Telegraph→Execute→Recovery exact boundary, 예고/실행 셀 동일성, Recovery 한정 폭탄 피해, 안전한 phase 전환과 사망 점유 단일 제거.
- Unity 보스방 단일 활성, 패턴 피해와 기존 무적 공유, 위험 셀 presenter의 Telegraph/Execute 일치, Recovery 반격 4회, 2페이즈·격파·단일 방 클리어와 실제 WebGL 가독성.
- 갑옷 적의 서로 다른 폭발 2회, 첫 피격 상태·cadence 변화, 같은 `BombId` 중복 차단과 두 번째 사망 뒤 점유 단일 제거.
- 방 경계·연결성, 서로 다른 첫 이동의 퇴로 2개, 닫힌 유도 경로와 씬 표현 일치.
- 동일 버전·정의·seed 던전 재현과 golden snapshot, 4~5 전투방·첫 보상·보스 전실/보스 경로·선택 가지·연결 트리·암시적 좌표 루프 방지.
- 던전 방향별 이웃, 첫 전투 잠금·클리어 전 퇴실 차단, 안전방 비잠금, 클리어 방 양방향 재방문과 전체 트리 왕복.
- 던전 전투방 배정의 catalog 순서 무관 재현, 활성 출구·회전 호환, 사용 균형, seed 다양성과 호환 콘텐츠 부족 실패.
- Unity 던전 카탈로그의 room asset·씬 매핑, 중복·누락 경계, Core 배정 선택 해석과 잠금·클리어·왕복 위임.
- 던전 문의 북·동·남·서 안정 순서, 비연결 `Inactive`, 미클리어 연결 `Locked`, 클리어 뒤 동일 대상 `Open`, 전투방 활성 출구 배정과 Unity 런 snapshot 일치.
- 전투방 0/90/180/270도 전체 셀·출구 회전과 크기 교환, 적 비활성 placeholder의 이동·폭탄·체력 재사용, 적 actor 미생성과 `Awake` 전 runtime spawn 준비.
- 특수방 catalog의 필수 타입·고유 씬, 로드 전 Core 미변경, 중복·로드 불가·씬 불일치 거부, 기대 씬 뒤 단일 commit, persistent host primary 단일성.
- WebGL focus 상실 뒤 입력 stuck 없음.
