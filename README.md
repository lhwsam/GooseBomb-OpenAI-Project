<p align="center">
  <img src="Assets/Arts/Team/Team_Icon.png" alt="Team VIGILANTE" width="360">
</p>

<h1 align="center">GooseBomb</h1>

<p align="center">
  <strong>놓고, 바꾸고, 유도하고, 터뜨려라!</strong><br>
  폭탄이 터질 위치와 시간을 설계하며 던전을 돌파하는<br>
  3D 탑다운 룸 액션 로그라이트
</p>

<p align="center">
  <a href="https://lhwsam.github.io/OpenAI-Project/">
    <strong>🎮 브라우저에서 바로 플레이하기</strong>
  </a>
</p>

<p align="center">
  <img src="https://img.shields.io/badge/Unity-6000.5.3f1-000000?logo=unity&amp;logoColor=white" alt="Unity">
  <img src="https://img.shields.io/badge/Platform-WebGL-5C2D91" alt="WebGL">
  <img src="https://img.shields.io/badge/Development-2%20Weeks-EF4444" alt="2 Weeks">
  <img src="https://img.shields.io/badge/Team-2%20Developers-F59E0B" alt="2 Developers">
  <img src="https://img.shields.io/badge/AI%20Assisted-Codex-10A37F" alt="Codex">
</p>

---

## 게임 소개

**GooseBomb**는 거위가 되어 다양한 폭탄을 설치하고 교체하며 던전을 돌파하는 3D 탑다운 룸 액션 로그라이트입니다.

폭탄마다 폭발 범위와 재사용 대기시간이 다르기 때문에 단순히 적을 향해 공격하는 것만으로는 충분하지 않습니다. 적의 움직임을 예상하고, 몇 초 뒤 폭탄이 터질 공간을 설계하며, 자신이 만든 위험에서 빠져나와야 합니다.

서로 다른 폭탄을 상황에 맞게 교체하고 연쇄 폭발을 만들어 던전의 마지막에 기다리는 보스를 쓰러뜨리는 것이 목표입니다.

<p align="center">
  <img src="Docs/Media/README/gameplay.png" alt="GooseBomb Gameplay" width="49%">
  <img src="Docs/Media/README/boss-clear.png" alt="GooseBomb Boss Battle" width="49%">
</p>

## 핵심 특징

### 상황에 맞게 교체하는 폭탄

일반 폭탄, 범위 폭탄, 직선 폭탄은 서로 다른 폭발 형태와 재사용 대기시간을 가집니다. 두 개의 폭탄 슬롯을 교체하며 방 구조와 적의 위치에 맞는 폭탄을 선택해야 합니다.

### 미래의 공간을 설계하는 전투

폭탄은 설치 즉시 피해를 주지 않습니다. 폭발까지 남은 시간 동안 적을 범위 안으로 유도하고, 다음 폭탄을 배치하며, 안전한 퇴로를 확보해야 합니다.

### 수제 방과 절차적 던전

전투 공간은 직접 설계한 방을 기반으로 구성되며, 매 게임마다 방의 연결과 진행 경로가 달라집니다. 전투방뿐 아니라 보상방, 회복방, 비밀방과 보스방을 탐색할 수 있습니다.

### 서로 다른 행동 패턴의 적

플레이어를 추격하는 적, 빠르게 돌진하는 적, 스스로 폭발하는 적과 폭탄을 던지는 적이 등장합니다. 마지막에는 여러 공격 패턴과 페이즈를 가진 보스가 기다립니다.

### 전투를 전달하는 시각·청각 연출

폭발 예상 범위, 홀로그램 텔레그래프, 폭발 VFX, 카메라 흔들림, 상황별 UI와 사운드를 통해 위험과 공격 타이밍을 직관적으로 전달합니다.

## 조작 방법

| 동작 | 기본 키 |
|---|:---:|
| 이동 | 방향키 |
| 폭탄 설치 | `Z` |
| 폭탄 교체 | `X` |
| 오브젝트 상호작용 | `F` |
| 일시정지 | `Esc` |
| 런 재시작 | `R` |

방향키 대신 `WASD`도 사용할 수 있으며, 설정 화면에서 주요 키를 변경할 수 있습니다.

> WebGL 실행 후 키가 반응하지 않는 경우 게임 화면을 한 번 클릭해 주세요.  
> PC 환경의 Chrome 또는 Edge 브라우저 플레이를 권장합니다.

## 프로젝트 정보

| 항목 | 내용 |
|---|---|
| 게임명 | GooseBomb |
| 장르 | 3D 탑다운 룸 액션 로그라이트 |
| 플랫폼 | PC WebGL |
| 개발 기간 | 약 2주 |
| 개발 인원 | 2명 |
| 엔진 | Unity 6000.5.3f1 |
| 렌더링 | Universal Render Pipeline |
| 주요 기술 | C#, Unity Input System, DOTween, ScriptableObject |

## 기술적 특징

### 논리와 표현의 분리

캐릭터의 Transform이나 물리 충돌 결과가 아닌 정수 XZ 논리 격자를 게임 판정의 기준으로 사용했습니다. 이동, 점유, 폭탄, 폭발과 연쇄 판정을 표현 계층과 분리하여 일관된 게임 규칙을 유지했습니다.

### 고정 시뮬레이션

플레이어 입력과 이동, 폭탄 퓨즈, 적 AI와 피해 판정을 10ms 고정 시뮬레이션 구조로 처리했습니다. 프레임 변화가 게임 규칙에 미치는 영향을 줄이고 WebGL에서도 일관된 동작을 목표로 했습니다.

### 데이터 기반 콘텐츠

폭탄과 적, 보스, 방 정보는 ScriptableObject 기반 저작 데이터로 분리했습니다. 규칙 코드를 변경하지 않고도 콘텐츠와 튜닝 값을 조정할 수 있도록 구성했습니다.

### WebGL 대응

브라우저 입력 포커스, 키보드 입력, 오디오 재생 시작, 화면 크기와 픽셀 카메라 표현을 점검하고 GitHub Pages에서 바로 플레이할 수 있도록 배포했습니다.

## 팀

| 이름 | 담당 |
|---|---|
| **이현우** | 기획 · 프로그래밍 |
| **허한결** | 아트 · 프로그래밍 |

두 명이 약 2주 동안 기획, 아트, 프로그래밍과 플레이테스트를 함께 진행했습니다.

## AI 활용

GooseBomb는 전체 제작 과정의 약 **90%에서 Codex와 GPT를 적극적으로 활용한 AI 협업 프로젝트**입니다.

### Codex·GPT 활용 영역

- 게임 구조 설계와 시스템 구현
- 대부분의 C# 게임플레이 코드 작성 및 리팩터링
- 플레이어 이동, 폭탄, 적 AI와 보스 시스템 구현
- UI, VFX, 애니메이션과 사운드 연결
- 오류 원인 분석과 회귀 수정
- EditMode·PlayMode 테스트 작성
- 프로젝트 문서화와 Git 작업
- WebGL 빌드 점검 및 GitHub Pages 배포
- 일부 이미지 리소스 제작
- 게임 배경음악 제작

### Unity MCP 활용

Codex와 Unity Editor를 MCP로 연결하여 다음 작업을 보조했습니다.

- Unity 컴파일 상태 확인
- Console 오류 및 경고 점검
- Scene·Prefab·ScriptableObject 연결 검증
- EditMode·PlayMode 테스트 실행
- 실제 플레이 상태와 UI 확인

### 사람이 직접 결정한 영역

AI가 구현과 검증을 보조했지만 다음 항목은 개발자가 직접 판단하고 결정했습니다.

- 게임의 핵심 콘셉트와 플레이 방향
- 폭탄 교체 시스템과 전투 규칙
- 이동 조작감과 전투 밸런스
- UI 구성과 아트 디렉션
- 애니메이션, VFX와 카메라 연출
- 플레이테스트 결과에 따른 유지·수정·제거 판단
- 최종 콘텐츠 선택과 출시 범위

AI가 제안한 결과물을 그대로 사용하는 것이 아니라 실제 플레이와 테스트를 반복하며 개발자의 의도에 맞게 수정했습니다.

## 개발 및 검증

프로젝트의 게임 규칙은 Unity에 직접 의존하지 않는 Core 계층과 Unity 표현 계층으로 분리했습니다.

검증 과정에서는 다음 방법을 사용했습니다.

- Core 규칙 EditMode 테스트
- Scene·Prefab 연결 PlayMode 테스트
- Unity 컴파일 및 Console 확인
- 콘텐츠 참조 검사
- WebGL 빌드
- 브라우저 키보드·오디오·화면 표시 스모크 테스트
- 실제 플레이를 통한 조작감과 연출 확인

## 사용 리소스

프로젝트에는 팀이 직접 제작한 리소스, 생성형 AI로 제작한 이미지와 음악, CC0 리소스 및 외부 Unity 패키지가 함께 사용되었습니다.

외부 리소스는 각 원본의 이용 조건에 따라 관리했으며, 생성형 AI를 활용한 콘텐츠는 위의 AI 활용 항목에 공개했습니다.

---

<p align="center">
  <strong>Team VIGILANTE</strong><br>
  Made in 2 weeks with Unity and Codex
</p>
