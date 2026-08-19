# 코딩 컨벤션 하네스

## 프로젝트 기준

- 게임 제목: `야생동물 스포츠 하는 날`
- 장르: 종합 미니게임 / 싱글플레이
- 주요 시스템: 랜덤 미니게임, 스테이지 선택, 제한 시간, 점수, 서버 점수 저장
- 엔진: Unity
- 주 언어: C#
- 주요 Unity 패키지: `Packages/manifest.json` 기준 Input System, URP, Test Framework, uGUI 등

## 네이밍 컨벤션

- 클래스 / 타입: PascalCase
  - 예: `PlayerController`
- 인터페이스: `I` + PascalCase
  - 예: `IDamageable`
- 로컬 변수 / 매개변수: camelCase
  - 예: `moveSpeed`
- private 필드: `_camelCase`
  - 예: `_moveSpeed`
- `private readonly` 필드: `_camelCase`
  - 예: `_defaultSpeed`
- `const`: PascalCase
  - 예: `MaxHealth`
- 프로퍼티: PascalCase
  - 예: `MoveSpeed`
- 메서드: PascalCase, 동사 우선
  - 예: `Move()`, `ResetState()`
- Unity 기본 함수: Unity 기본 이름 유지
  - 예: `Awake()`, `Start()`, `Update()`
- 이벤트 변수: PascalCase
  - 예: `PlayerDied`
- 이벤트 함수: `On` + PascalCase
  - 예: `OnPlayerDied()`
- 파일명: 대표 클래스명과 동일
  - 예: `PlayerController.cs`
- 씬/프리팹 이름: 의미가 드러나게 작성
  - 예: `MainMenu`, `PlayerPrefab`

## 한글 우선 규칙

- 코드 식별자는 영어를 사용한다.
- 에디터 표시 이름은 한글을 사용할 수 있다.
- 인스펙터 `Header`, `Tooltip`은 한글을 우선한다.
- 디버그 오버레이와 테스트 UI는 한글을 우선한다.
- 문서와 작업 템플릿은 한글을 우선한다.

## Unity / C# 스타일

- 직렬화 필드는 `private` + `[SerializeField]`를 우선한다.
- public 필드는 외부에서 직접 수정해야 하는 명확한 이유가 있을 때만 사용한다.
- 프로퍼티는 외부 읽기/제어가 필요할 때 사용한다.
- 인스펙터 참조는 실행 전 또는 `Awake`에서 검증한다.
- 반복 접근하는 컴포넌트는 캐싱한다.
- `Update()`는 매 프레임 처리가 실제로 필요할 때만 사용한다.
- 코루틴은 시간 흐름이 명확한 작업에만 사용한다.
- 이벤트 구독/해제는 `OnEnable`/`OnDisable` 쌍을 우선한다.
- 네임스페이스는 프로젝트 구조가 확정된 뒤 도입 여부를 결정한다.

## 주석 규칙

- public API, 공용 데이터 구조, 사용 조건이 중요한 메서드는 필요할 때 XML 주석을 사용한다.
- 복잡한 의도가 있는 코드에는 짧은 일반 주석을 허용한다.
- Unity 인스펙터 설명은 `[Tooltip]`을 우선한다.
- 자명한 코드는 주석을 달지 않는다.
- 임시 코드는 `TODO:`와 이유를 함께 남긴다.

## 구조 규칙

- 한 함수는 한 가지 역할을 우선한다.
- 함수가 읽기 어렵게 길어지면 private 메서드로 분리한다.
- 매직 넘버는 상수, `[SerializeField]`, 설정 데이터로 분리한다.
- 매니저 클래스는 실제 전역 책임이 있을 때만 만든다.
- 공통화는 2회 이상 반복되고 의미가 같을 때 검토한다.
- 리팩터링은 요청 범위 안에서만 수행한다.
- 폴더 구조는 현재 구조와 실제 결정된 내용을 기준으로 조정한다.

## 파일 작성 체크리스트

- [ ] 파일명과 대표 클래스명이 일치하는가
- [ ] private 필드는 `_camelCase`인가
- [ ] 클래스/메서드는 PascalCase인가
- [ ] 인스펙터 참조 필드는 `[SerializeField] private`인가
- [ ] 매직 넘버가 의미 있는 이름으로 분리됐는가
- [ ] 함수 역할이 과하게 섞이지 않았는가
- [ ] 불필요한 매니저나 추상화를 추가하지 않았는가
