# 보스 플레이테스트 계측 수직 슬라이스

- 상태: `Implemented`, Development WebGL 검증 완료
- 입력 가설: [체력 10 보스 플레이테스트 프로토콜](../Playtesting/BossPhaseReworkProtocol.md)
- 계측 권위: [프로토타입 계측](../Systems/Telemetry.md)
- 분석 도구: `Tools/PlaytestLogAnalyzer.mjs`

## 목적

상시 피해 허용 뒤 보스 수치를 감으로 다시 조정하지 않도록, 고정 WebGL 플레이 한 번에서 다음 사실을 재구성한다.

- 보스전 시작부터 격파 또는 플레이어 사망까지의 시간
- One / Two / LastStand별 보스 피해와 플레이어의 보스 패턴 피격
- Telegraph / Execute / Recovery별 보스 피해
- 플레이어 폭탄 / 자폭병과 폭탄 정의별 보스 적중
- 서로 다른 플레이어 폭탄 정의가 연속 적중한 횟수
- 과열 완료, 자폭병 소환·폭발·보스 적중

## 비목표

- 사건 수만으로 재미, 의도, 공정성 또는 가독성을 판정하지 않는다.
- 폭탄 교체 입력을 두 슬롯 교대 공격 의도로 간주하지 않는다.
- parity 안전 칸 재사용과 자폭병 유도 의도는 셀 사건만으로 추정하지 않는다.
- 이 슬라이스에서 보스 체력·속도·투척 간격·과열 시간을 바꾸지 않는다.

## 런타임 사건 계약

기존 `boss-damaged`와 첫 피격 존재 marker는 호환성을 위해 유지한다. Development WebGL에서 적용된 사건마다 다음 상세 marker를 추가한다.

```text
boss-damaged-phase-<one|two|last-stand>-state-<telegraph|execute|recovery>-source-<player-bomb|self-destruct>-definition-<bomb-id>-health-<remaining>
boss-player-damaged-phase-<one|two|last-stand>-pattern-<pattern>-health-<remaining>
```

- 보스 피해의 phase와 source는 적용 시점 `BossDamageResult`를 사용한다.
- state와 pattern은 같은 논리 전이에서 probe가 마지막으로 받은 확정 보스 상태를 사용한다.
- 폭탄 정의는 `BombId`와 설치·폭발 사건의 정의를 probe 생명주기 안에서 연결한다.
- fatal 피해 뒤 Core state가 `Defeated`로 바뀌어도 피해 직전의 마지막 전투 상태에 귀속한다.
- Release WebGL과 Editor 런타임에는 reporter 호출이 컴파일되지 않는다.

## 분석 계약

`bombswap/playtest-log@1` 원본은 유지하고 분석 출력만 `bombswap/playtest-summary@2`로 올린다. 분석기는 던전 보스방 marker와 독립 `BossBattlePlaytest`의 room marker를 모두 시작점으로 받아 중복 시작을 합친다.

- 완결 encounter는 `boss-defeated`, `player-died`, `run-failed` 또는 재시작으로 닫는다.
- 종료 marker가 없으면 `incomplete`와 관찰된 최소 시간만 기록한다.
- 새 상세 marker가 없는 이전 로그도 거부하지 않고 `unclassifiedEvents`로 남긴다.
- 폭탄 정의 교대 횟수는 성공한 보스 적중 사이의 정의 변화이며 시도 횟수나 의도를 뜻하지 않는다.

## 검증

- Node fixture는 던전/독립 시작 alias, 세 phase, 세 상태, 플레이어/자폭병 source, 폭탄 정의 교대, 보스 패턴 피격과 Markdown 출력을 검사한다.
- 이전 `bombswap/playtest-log@1` 실데이터를 `summary@2`로 다시 분석해 하위 입력 호환성을 확인한다.
- Unity 컴파일·PlayMode와 Development WebGL에서 실제 상세 marker 및 browser Console 오류 0을 확인한다.

최종 증거:

- Node fixture 통과, 이전 2,512사건 `playtest-log@1` 입력의 `summary@2` 재분석 통과.
- 연결 Unity EditMode `329/329`, PlayMode `133/133`, 실패·건너뜀 0.
- 11씬 Development WebGL 빌드 138,800,198 bytes, 102.214초, 오류 0·안내 경고 3.
- Chromium keyboard `46/46`, Gamepad `14/14`, Console/page error 0.
- 실제 2,473사건 요약에서 보스 피해 10건 전부 상세 분류, encounter 62.531초, phase `4/5/1`, state `1/0/9`, 플레이어 폭탄/자폭병 `9/1`, 폭탄 정의 교대 적중 6회를 재구성했다.
- 첫 WebGL 시도에서 WebGL 전용 코드가 `BombSnapshot.BombId`를 참조한 컴파일 오류 3건과 build error 1건을 발견했다. 실제 계약인 `BombSnapshot.Id`로 수정하고 실패 산출물을 보존한 뒤 새 경로에서 재검증했다.
