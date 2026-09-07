# AI 개발 하네스

## 프로젝트 개요

- 프로젝트명: `Wildlife-Sports-Day`
- 게임 제목: `야생동물 스포츠 하는 날`
- 엔진: Unity
- Unity 버전: `6000.0.68f1`
- 제품명: `ProjectSettings/ProjectSettings.asset` 기준 `Wildlife-Sports-Day`
- 현재 상태: 초기 Unity 프로젝트 상태 / 기획 일부 확정
- 장르: 종합 미니게임 / 싱글플레이
- 플랫폼: PC
- 핵심 플레이: 제한 시간 안에 야생동물 관련 랜덤 미니게임을 클리어하며 점수 기록
- 아트 방향: 병맛 분위기, 세부 스타일은 확인 필요

> 기획, 세계관, 장르, 캐릭터, 시스템, 스토리, 분위기는 실제 결정 또는 `PROJECT_PLAN.md`의 확정 항목으로 기록되기 전까지 단정하지 않는다.

## 문서 우선순위

1. 현재 대화의 직접 지시
2. `AGENTS.md`
3. `.agents/docs/conventions/AI_DEVELOPMENT_RULES.md`
4. `.agents/docs/conventions/CODING_CONVENTIONS.md`
5. `.agents/docs/conventions/GITHUB_HARNESS.md`
6. `.agents/docs/planning/PROJECT_PLAN.md`
7. `.agents/docs/planning/VISUAL_HARNESS.md`
8. `.agents/skills/*/SKILL.md` (해당 작업 요청 시)

충돌 시 더 높은 우선순위를 따른다.

## 문서 맵

- `AGENTS.md`: AI 작업자가 먼저 읽는 진입 문서
- `.agents/docs/conventions/AI_DEVELOPMENT_RULES.md`: AI 구현/검증/기록 규칙
- `.agents/docs/conventions/CODING_CONVENTIONS.md`: Unity/C# 코드 스타일
- `.agents/docs/conventions/GITHUB_HARNESS.md`: 1인 개발 기준 Git 운영 규칙
- `.agents/docs/planning/PROJECT_PLAN.md`: 현재 상태 기록 중심 작업 문서
- `.agents/docs/planning/VISUAL_HARNESS.md`: 시각/UI/UX 검수 기준
- `.agents/skills/auto-commit/SKILL.md`: 자동 커밋 절차
- `.agents/skills/auto-pr/SKILL.md`: PR 생성 절차
- `.agents/skills/github-issue/SKILL.md`: GitHub 이슈 생성 절차

## 현재 확인된 프로젝트 구조

- `Assets/Scenes/SampleScene.unity`: 기본 샘플 씬
- `Assets/Settings/`: Unity 설정 에셋 폴더
- `Assets/TutorialInfo/`: Unity 템플릿 안내용 스크립트/에셋
- `Packages/manifest.json`: Input System, URP, Test Framework, uGUI 등 포함
- `ProjectSettings/ProjectSettings.asset`: 제품명 `Wildlife-Sports-Day`, 버전 `0.1.0`

확인되지 않은 폴더 구조를 문서나 코드에서 전제로 삼지 않는다.

## 작업 우선순위

1. 현재 대화에서 명시된 요청 수행
2. 사용자 변경사항 보존
3. 씬/프리팹/메타 참조 안정성 유지
4. 기존 구조와 public API 유지
5. 가능한 검증 수행
6. 작업 내용과 미검증 항목 기록

## 작업 규칙

- 확인되지 않은 프로젝트 정보는 지어내지 않는다.
- 이전 프로젝트 내용이나 다른 대화 내용을 섞지 않는다.
- 확실하지 않은 내용은 빈칸 또는 `확인 필요`로 남긴다.
- 명시되지 않은 리팩터링은 하지 않는다.
- 기존 public API는 요청 없이 변경하지 않는다.
- 기존 파일명, 클래스명, 씬명, 프리팹명 변경은 참조 영향을 먼저 확인한다.
- Unity `.meta` 파일의 GUID 연결을 임의로 끊지 않는다.
- 씬/프리팹/ScriptableObject의 인스펙터 참조를 코드 변경으로 깨뜨리지 않는다.
- 사용자가 수정한 파일은 임의로 복구하거나 되돌리지 않는다.
- 새 구조를 만들기 전에 현재 `Assets` 구조와 기존 패턴을 먼저 확인한다.
- 새 UI/HUD/Canvas를 만들기 전에 기존 씬의 Canvas Render Mode, Camera, CanvasScaler, 기준 해상도, 정렬 방식을 먼저 확인하고 가능한 그대로 따른다. 단, 씬 전환 중 유지되는 전체 게임 HUD처럼 월드 오브젝트에 가려지면 안 되는 UI는 사용자 확인 후 Overlay Canvas를 사용할 수 있다.
- 기존 구조에 없는 fallback, 대체 탐색, 자동 생성 로직은 사용자가 요청하거나 기존 패턴으로 확인된 경우에만 추가한다.
- 확정된 기획 범위는 `PROJECT_PLAN.md`를 따른다.
- 미확정 항목은 게임 루프 세부값, 실제 씬 구현 방식, 저장 시스템 구현 방식, 전체 UI 구조, 세부 아트 스타일 등을 강제하지 않는다.

## 문서 처리 규칙

- 사용자가 이미 작성한 규칙은 삭제하지 말고 필요한 내용만 추가/정리한다.
- `PROJECT_PLAN.md`는 완성형 설계 문서가 아니라 현재 상태 기록 중심 문서로 취급한다.
- 단정된 기획 내용은 `PROJECT_PLAN.md`에 실제 확정 항목으로 적힌 내용만 사용한다.
- `PROJECT_PLAN.md`의 아이디어 메모는 확정 정보로 취급하지 않는다.
- `PROJECT_PLAN.md`가 사용자 기획으로 갱신된 뒤 필요한 가이드 문서를 다시 보강한다.
  - `AGENTS.md`
  - `.agents/docs/conventions/AI_DEVELOPMENT_RULES.md`
  - `.agents/docs/planning/VISUAL_HARNESS.md`
- 문서는 장문 설명보다 실무형 규칙과 체크리스트를 우선한다.
- 커밋, PR, 이슈 관련 규칙이 충돌하면 `.agents/skills` 문서를 우선한다.

## 작업 완료 전 체크

- [ ] 요청 범위 밖 변경이 없는가
- [ ] 확인되지 않은 정보를 단정하지 않았는가
- [ ] Unity 메타/씬/프리팹 참조를 깨뜨리지 않았는가
- [ ] public API 변경이 필요한 경우 이유를 남겼는가
- [ ] 가능한 검증을 수행했는가
- [ ] 검증하지 못한 항목과 이유를 기록했는가
