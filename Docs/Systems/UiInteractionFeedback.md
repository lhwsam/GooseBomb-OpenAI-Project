# UI 상호작용 피드백

- 상태: `Accepted`
- 기준일: 2026-08-22
- 관련: [로비 수직 슬라이스](../Development/LobbySlice.md), [사용자 설정과 오디오](UserSettingsAndAudio.md), [WebGL 브라우저 테스트](../WebGL/BrowserTestMatrix.md)

## 목적

버튼이 마우스 hover, 포인터 누름, 키보드 선택과 Submit에 즉시 반응해 현재 상호작용 대상을 명확하게 보여 준다. 이 피드백은 버튼의 클릭 규칙을 바꾸지 않는 표현 계층이며 게임 규칙이나 입력 명령의 권위 상태가 아니다.

## 상태 계약

| 상태 | 기본 배율 | 의미 |
|---|---:|---|
| Normal | `1.00` | 입력 대상이 아님 |
| Hover 또는 키보드 선택 | `1.06` | 선택 가능한 현재 대상 |
| 포인터 누름 또는 UI Submit pulse | `0.96` | 입력을 눌렀다는 즉시 피드백 |
| Disabled | `1.00` | `Button.IsInteractable()`이 false이면 강조하지 않음 |

- 상태 전환 기본 시간은 `0.10초`, Submit 누름 pulse는 `0.08초`다.
- pause 설정에서도 동작해야 하므로 `Time.unscaledDeltaTime`을 사용한다.
- 눌림이 hover/선택보다 우선하고, hover/선택이 Normal보다 우선한다.
- 포인터가 버튼에 들어오면 EventSystem 선택도 해당 버튼으로 넘겨 이전 키보드 선택의 확대가 남지 않게 한다. 포인터가 벗어나면 현재 선택 여부와 관계없이 Normal로 복귀하고, 이후 키보드 이동 또는 Submit이 들어오면 키보드 선택 표현을 다시 사용한다.
- 비활성화될 때는 저작된 원래 scale로 정확히 복원한다.
- 선택적 `colorTarget`이 있으면 Normal에서 `startColor`, Hover·키보드 선택·Pressed에서 `targetColor`로 전환한다. 로비 메인 메뉴의 `StartRunButton`과 `ControlsButton`은 각각의 자식 `TextMeshProUGUI`를 기본 대상으로 사용하며 버튼 배경은 변경하지 않는다.
- 선택적 `hoverVisualTargets`는 Normal·Disabled에서 꺼지고 Hover·키보드 선택·Pressed에서 켜진다. 현재 로비의 시작·설정 버튼은 좌우 화살표를 함께 사용한다.
- 설정 리바인딩에서 중복 키가 거부되면 선택한 키 버튼만 0.32초 동안 최대 8 reference pixel 좌우로 흔들리고, 값 라벨은 `이미 사용 중`과 경고색을 약 1초 표시한 뒤 원래 키·색·위치로 정확히 복원한다. 이 알림은 hover/press 상태나 입력 override의 권위가 아니다.

## 구현과 저작

- 런타임 컴포넌트는 `PrototypeButtonScaleFeedback`이며 `Button`과 같은 GameObject에 하나만 둔다.
- `visualTarget`은 실제로 확대·축소할 `RectTransform`이다. 별도 대상이 없는 기존 로비 버튼은 버튼 root를 사용한다.
- `colorTarget`은 선택적 `Graphic` 참조이며 TMP 텍스트를 포함한다. Inspector에서 `startColor`와 `targetColor`를 버튼별로 조정할 수 있고, 참조가 없으면 해당 버튼은 scale만 전환한다. 로비의 시작·설정 버튼은 참조가 비어 있을 때 각 버튼의 자식 TMP 라벨을 런타임 fallback으로 사용한다.
- `hoverVisualTargets`는 선택적 자식 GameObject 목록이며 Inspector의 직렬화 참조만 사용한다. 런타임에는 이름·태그·계층 검색 fallback이 없다. 로비의 시작·설정 버튼은 각 좌우 화살표 두 개를 명시적으로 참조해야 하며 Editor validator가 누락과 버튼 계층 밖 참조를 거부한다.
- 로비 시작 시 `StartRunButton`은 키보드·게임패드 Submit을 위해 EventSystem 선택을 유지하지만, 첫 포인터·탐색·Submit 입력 전에는 선택 시각 효과를 숨겨 Normal 상태로 표시한다.
- hit 영역을 고정해야 하거나 Layout Group 영향에서 시각 요소를 분리할 UI는 버튼 아래에 `Visual` 자식을 만들고 그 자식을 `visualTarget`으로 지정한다.
- Editor authoring과 validator는 버튼 자신 또는 그 하위 `RectTransform`만 유효한 대상으로 인정하며, 디자이너가 지정한 하위 대상과 이름이 `Visual`인 직접 자식을 보존한다.
- Inspector에서 hover·pressed 배율과 두 시간을 버튼별로 조정할 수 있다. 로비 공통값으로 되돌리려면 `Bomb Swap > UI > Apply Button Feedback To Lobby`를 실행한다.
- 로비 builder는 씬 생성·구 설정 마이그레이션 때 누락된 컴포넌트를 같은 공통값으로 보완한다. Editor validator는 비활성 설정 패널을 포함한 로비의 모든 Button을 검사한다.

## 성능과 WebGL

- DOTween은 `BombSwap.Unity` 표현 계층의 짧은 scale·색상 보간에만 사용하고 Core 규칙이나 입력 권위에는 사용하지 않는다.
- 컴포넌트마다 scale Tween과 선택적 색상 Tween을 하나씩만 소유한다. 새 상태 전에 기존 Tween을 종료하고 `OnDisable`/`OnDestroy`에서 정리한 뒤 저작 scale·색상을 즉시 복원한다.
- Tween은 unscaled update를 사용한다. Coroutine·material 인스턴스를 만들지 않으며 frame 반복 경로에서 컬렉션이나 LINQ를 사용하지 않는다.
- 중복 키 알림 Sequence는 설정 presenter가 최대 하나만 소유한다. 새 리바인딩, 초기화, 패널 비활성화 전에 기존 Sequence를 종료하고 저작된 anchored position·색·키 라벨을 복원한다.
- WebGL에서는 마우스 hover/click, 키보드 선택/Submit, pause 중 피드백과 브라우저 focus 복귀를 실제 canvas에서 확인한다.

## 검증

- PlayMode 테스트는 `timeScale = 0`에서도 hover·press·복귀가 끝나는지 확인한다.
- 빠른 hover·press·exit 반복에서 중첩 Tween을 종료하고 원래 scale로 복귀하는지 확인한다.
- 시작·설정 버튼은 각각 지정된 TMP 라벨만 `startColor ↔ targetColor`로 전환하고 버튼 배경은 바꾸지 않으며 비활성화 시 `startColor`로 복원되는지 확인한다.
- 시작·설정 버튼의 좌우 화살표가 hover·키보드 선택·누름에는 함께 켜지고 포인터 이탈·선택 해제·Disabled에는 함께 꺼지는지 확인한다.
- 로비 최초 표시에서 시작 버튼이 Normal 상태이고, 첫 Submit 또는 탐색 뒤에만 선택 시각 효과가 나타나는지 확인한다.
- 키보드 Select·Submit pulse·Deselect 순서를 확인한다.
- 별도 자식 `visualTarget`이 저작된 비균일 base scale을 보존하고 버튼 root는 바꾸지 않는지 확인한다.
- 씬 검증은 로비의 모든 Button에 정확히 하나의 컴포넌트와 공통 기본 구성이 있는지 확인한다.
- 중복 키 거부 시 override가 이전 값으로 돌아가고 선택 버튼만 흔들리며, 완료·취소·비활성화 뒤 위치·색·키 라벨이 복원되는지 확인한다.
