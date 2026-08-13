# WebGL 성능 예산

- 상태: 예산 수립 방식 `Accepted`, 수치 `Proposed`

## 원칙

빈 기준 씬과 첫 수직 슬라이스를 실제 목표 브라우저/하드웨어에서 측정한 뒤 수치를 확정한다. 근거 없는 고정 목표를 완료 기준으로 만들지 않는다.

## 측정 항목

| 영역 | 측정 | 초기 기준 |
|---|---|---|
| Frame | 평균, p95/p99 frame time, 긴 프레임 | 기준 빌드 대비 회귀와 전투 peak 기록 |
| CPU | simulation, scripts, physics, rendering | 폭발/연쇄/다수 적 peak 분리 |
| Allocation | frame당 managed allocation, GC spike | 안정 상태 반복 경로 0 B/frame 지향 |
| Memory | 초기/peak heap, texture/mesh/audio | 브라우저 탭 crash 없이 여유 확보 |
| Rendering | draw calls, SetPass, triangles, overdraw | Mobile URP 기준으로 장면별 기록 |
| Download | 압축/비압축 build size, 첫 로드 | vendor/리소스 추가 전후 delta 기록 |
| Loading | cold/warm start, progress 정지 | 실제 호스팅에서 측정 |

## 기본 최적화 규칙

- 폭탄, 폭발, 피격/사망 VFX는 반복 수요가 확인되면 풀링한다.
- `Update`에서 LINQ, boxing, 새 컬렉션, 문자열 로그를 만들지 않는다.
- renderer material 복제를 피하고 MaterialPropertyBlock 또는 공유 머티리얼을 검토한다.
- 실시간 광원과 그림자는 최소화하고 환경은 가능한 한 bake한다.
- 물리 쿼리는 표현/접촉 후보에 한정하고 폭발 셀 판정을 대체하지 않는다.
- Resources 사용은 명시적 목록과 크기를 관리하고 무분별한 전역 로드를 피한다.

## 예산 확정 절차

1. 빈 TestSandbox WebGL release 기준선.
2. 기본 폭탄+적 1종 수직 슬라이스.
3. 예상 최대 동시 폭탄/연쇄/적 스트레스 씬.
4. 목표 데스크톱 브라우저 최소 2종에서 측정.
5. 수용 가능한 체감과 측정값으로 수치 확정.
6. CI 또는 정기 Web 검증에 경고/차단 기준 연결.
