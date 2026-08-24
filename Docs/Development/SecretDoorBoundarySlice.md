# 작업: 비밀문 출구 경계 판정

- 상태: `Accepted`
- 기준일: 2026-08-18

## 목표

- 금이 간 비밀문은 일반 문과 정확히 같은 방 경계 위치에 보여야 한다.
- 플레이어는 미공개 문 바로 앞의 저작 출구 셀까지 이동할 수 있고, 보이지 않는 한 칸 안쪽 벽에 막히지 않아야 한다.
- 어떤 폭탄 종류든 폭발 영향 셀이 문 앞 출구 셀에 도달하면 해당 비밀 연결만 공개해야 한다.

## 근거

- 사용자 플레이 피드백: 문 외형은 추후 에셋으로 교체하되 일반 문과 같은 위치·공간 문법을 사용한다.
- [금이 간 벽 비밀방](SecretRoomSlice.md)
- [폭탄과 폭발](../Systems/BombAndExplosion.md)
- [방 저작](../Systems/RoomAuthoring.md)
- 현재 코드 진입점: `BombExplosion`, `PrototypeDungeonRoomBinder`, `PrototypeDungeonDoorPresenter`, `PrototypeContentBuilder`

## 범위

- 변경 허용: 폭발 결과 조회 API, 비밀문 runtime adapter, 문 presenter/builder/validator, 관련 EditMode·PlayMode·WebGL 하네스와 문서.
- 직렬화 변경: 11개 던전·TestSandbox scene의 네 방향 secret crack root 위치. Unity Editor builder로만 저장한다.
- 변경 금지: 일반 파괴벽의 지형·전파 규칙, 폭탄 범위·fuse, 던전 그래프·Secret 공개 상태, 패키지·Input Actions·렌더 설정.
- 비목표: 범용 파괴 오브젝트 프레임워크, 최종 문 에셋·애니메이션·VFX·오디오, 토큰 소비처.

## 계약과 불변식

- 일반 파괴벽은 계속 Core `GridTerrain.DestructibleWall`이며 `DestroyedWalls`가 지형을 `Floor`로 바꾸고 ray 전파를 끝낸다.
- 비밀문은 지형 셀이 아니다. 미공개 상태의 이동 차단은 Core `DungeonRoomExitStatus.SecretWall`이 출구 경계를 소유한다.
- 문 앞 저작 출구 셀은 항상 `Floor`다. 플레이어·폭탄이 점유할 수 있고, 바깥 방향 이동 요청만 공개 전 거부된다.
- `BombExplosion.AffectedCells`에 문 앞 출구 셀이 포함되면 폭발이 문 면에 도달한 것으로 판정한다. `DestroyedWalls`에는 비밀문이 포함되지 않는다.
- 한 폭발이 여러 비밀문 impact cell에 닿으면 각 연결을 안정된 폭발 셀 순서로 독립 공개한다.
- 금 간 표현 root는 대응하는 일반 door renderer와 같은 world position을 사용하고 Collider를 갖지 않는다. `SecretWall`에서는 금 간 표현만 보이고, 공개 뒤와 방 재진입 시에는 금 간 표현과 일반 문을 모두 숨겨 빈 통로를 유지한다.
- 공개 상태는 `DungeonRunState`, 폭발 footprint는 `BombExplosion`, 방별 셀→출구 방향 매핑은 `PrototypeDungeonRoomBinder`, 시각 상태는 door presenter가 소유한다.

## 폭발 영향 오브젝트 확장 기준

- 전파를 막거나 지형을 바꾸는 오브젝트는 Core grid/resolver 계약에 추가한다.
- 피해 가능한 actor·bomb은 Core simulation이 `AffectedCells`를 소비한다.
- 전파를 바꾸지 않는 방 기믹은 Unity room adapter가 저작된 논리 셀·출구 edge를 `AffectedCells`에 매핑하고, 확정 결과만 해당 Core 상태 소유자와 presenter에 전달한다.
- 구현 하나뿐인 범용 인터페이스나 물리 Collider 기반 폭발 감지는 이번 범위에 추가하지 않는다.

## 완료 조건

- EditMode: `BombExplosion.Affects`가 포함·비포함 셀을 정확히 조회한다.
- PlayMode: 미공개 비밀문 앞 출구 셀이 Floor이고 접근 가능하지만 이동은 차단되며, 그 셀의 실제 폭발 영향으로 공개된다. 비밀문은 `DestroyedWalls`에 나타나지 않는다.
- Content: secret crack root와 대응 door renderer의 위치가 일치하고 Collider가 없으며 11개 scene validator가 통과한다.
- WebGL: 기존 seed-0 비밀문 발견·폭파·미니맵 공개·비밀방 왕복과 Console/page error 0을 유지한다.
- 문서: 비밀방, 폭발, 방 저작, 던전 생성, 현재 상태를 새 경계 계약으로 동기화한다.

## 위험과 롤백

- 현재 WebGL 자동 경로는 같은 출구 셀에 폭발을 닿게 하므로 폭탄 수치 변경은 필요하지 않다.
- 문 root 위치만 바꾸고 runtime 파괴벽을 남기면 시각·판정 불일치가 재발하므로 binder·session·validator·scene을 한 묶음으로 변경한다.
- 롤백 단위는 `AffectedCells` 기반 비밀문 adapter, runtime wall 제거, 문 root scene 위치, 테스트·문서다.

## 검증 근거

- Unity Editor builder로 11개 던전·TestSandbox scene의 네 방향 secret door root를 대응 일반 문 위치로 동기화했다.
- 연결 Unity EditMode `311/311` 통과: `Artifacts/Verification/ConnectedTests/20260818-092238-300.json`.
- 연결 Unity PlayMode `128/128` 통과: `Artifacts/Verification/ConnectedTests/20260818-092309-619.json`.
- StaticOnly: `Artifacts/Verification/20260818-183316-static/` 통과.
- `Artifacts/Verification/20260818-182656-secret-door-boundary-web/`의 11씬 Development WebGL 빌드는 138,263,355 bytes·124.752초·오류 0으로 성공했다. 기존 패키지·셰이더 범주의 경고 351건을 기록했다.
- Edge 키보드 smoke `40/40`, 가상 Gamepad smoke `14/14`, 1,161개 플레이테스트 사건 분석이 통과했고 두 실행의 Console/page error는 0이다. 실제 십자 폭탄으로 서쪽 비밀문 공개, 미니맵 갱신, Secret 입장·cache·양방향 복귀와 전체 보스·재시작 경로를 확인했다.
- `webgl-dungeon-secret-wall.png`에서 금 간 문이 서쪽 일반 문과 같은 외벽 경계 위치에 있고 플레이어가 문 앞 출구 셀까지 접근한 상태를 확인했다.
