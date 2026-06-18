# GitHub 하네스

## 운영 기준

- 현재 기준: 1인 클라이언트 개발 흐름
- 과한 Git Flow를 강제하지 않는다.
- 기본은 `main`, `develop` 중심으로 단순하게 운영한다.
- 작은 작업은 브랜치 없이 `develop`에서 직접 작업 가능하다.
- 범위가 크거나 되돌릴 가능성이 있으면 작업 브랜치를 만든다.
- 커밋, PR, 이슈 작업은 `.codex/skills/*/SKILL.md` 기준을 우선한다.

## 관련 스킬 문서

- `.codex/skills/auto-commit/SKILL.md`: 변경사항을 기능 단위로 나눠 커밋
- `.codex/skills/auto-pr/SKILL.md`: GitHub PR 생성
- `.codex/skills/github-issue/SKILL.md`: GitHub 이슈 생성

## 브랜치 전략

- `main`: 안정 버전. 실행/배포 가능한 상태 유지.
- `develop`: 일상 개발 통합. 작은 기능, 문서, 일반 수정은 여기서 처리 가능.
- `feature/*`: 새로운 기능 구현
- `bug/*`: 오류 수정
- `ui/*`: UI 배치/기능 구현
- `design/*`: 그래픽, 화면 구성 작업
- `docs/*`: 문서 작업
- `improve/*`: 사용자 위주의 기존 기능 개선
- `minigame/*`: 개별 미니게임 구현
- `refactor/*`: 동작 변경 없는 코드 구조 개선
- `save/*`: 설정 및 기록 저장 시스템
- `test/*`: 테스트 코드 및 동작 검증
- `chore/*`: 설정, 패키지, 폴더 정리 등 기타 작업

## 커밋 메시지 규칙

- Conventional Commits 형식을 사용한다.
- 기본 형식: `<type>(<scope>): <subject>`
- `type`과 `scope`는 영어로 작성한다.
- `subject`는 반드시 한글로 작성한다.
- 커밋 type은 Conventional Commits 기준이라 `feat`, `fix`, `docs` 같은 축약형을 유지한다.
- 이슈와 연결되는 경우 본문에 `Closes #N` 또는 `Related to #N`을 적는다.
- 사용자가 자동 커밋을 요청하면 `.codex/skills/auto-commit/SKILL.md` 절차를 따른다.
- 사용자 승인 전에는 커밋하지 않는다.

예시:

- `feat(Minigame): 악어 물기 미니게임 추가`
- `fix(UI): 버튼 입력 중복 실행 수정`
- `docs(Codex): AI 작업 하네스 문서 보강`
- `chore(Build): Unity 프로젝트 설정 갱신`

## GitHub 라벨 기준

- `UI`: UI 배치/기능 구현
- `Bug`: 오류 수정
- `Chore`: 설정, 패키지, 폴더 정리 등 기타 작업
- `Design`: 그래픽, 화면 구성 작업
- `Docs`: README, 기획서, 주석 등 문서 작업
- `Feature`: 새로운 기능 구현
- `Improve`: 사용자 위주의 기존 기능 개선
- `Minigame`: 개별 미니게임 구현
- `Refactor`: 동작 변경 없이 코드 구조 개선
- `Save`: 설정 및 기록 저장 시스템
- `Test`: 테스트 코드 및 동작 검증

## 이슈 규칙

- 이슈 제목은 `[라벨] 한글 설명` 형식으로 작성한다.
- 이슈 제목의 라벨은 위 GitHub 라벨 이름과 정확히 맞춘다.
- 이슈 본문은 한글로 작성한다.
- 사용자가 이슈 생성을 요청하면 `.codex/skills/github-issue/SKILL.md` 절차를 따른다.
- 사용자 승인 전에는 이슈를 생성하지 않는다.

예시:

- `[Feature] 랜덤 미니게임 선택 흐름 추가`
- `[Bug] 스테이지 후보 선택 중 타이머 오류 수정`
- `[Minigame] 악어 물기 미니게임 구현`
- `[UI] HUD 가독성 개선`

## PR 규칙

- 1인 개발에서는 PR을 필수로 강제하지 않는다.
- `main` 반영 전 검토가 필요하거나 작업 규모가 크면 PR을 사용한다.
- PR 제목은 `[라벨] 한글 설명` 형식으로 작성한다.
- PR 제목의 라벨은 위 GitHub 라벨 이름과 정확히 맞춘다.
- PR 본문 전체는 한글로 작성한다.
- PR에는 변경 내용에 맞는 라벨을 하나 이상 붙인다.
- 사용자가 PR 생성을 요청하면 `.codex/skills/auto-pr/SKILL.md` 절차를 따른다.
- 사용자 승인 전에는 PR을 생성하지 않는다.

PR 제목 예시:

- `[Feature] 랜덤 미니게임 선택 흐름 추가`
- `[Bug] 버튼 입력 중복 실행 수정`
- `[Docs] 하네스 문서 보강`

PR 본문 기본 구조:

```md
# 개요

# 내용

## 변경 영역

# 기타 사항
```

## Unity 파일 커밋 주의사항

- `*.meta`는 대응 원본 파일과 반드시 같은 커밋에 포함한다.
- `.meta` 파일만 단독 커밋하지 않는다.
- `Library/`, `Temp/`, `obj/`는 커밋하지 않는다.
- `ProjectSettings/` 변경은 의도적 변경일 때만 포함한다.
- 씬 파일은 변경 범위가 크므로 관련 스크립트/프리팹과 묶을지 별도 커밋할지 먼저 판단한다.

## main 반영 전 체크리스트

- [ ] Unity에서 컴파일 오류가 없는가
- [ ] 주요 씬이 열리는가
- [ ] 변경한 기능을 직접 확인했는가
- [ ] 불필요한 `.meta` 변경이 없는가
- [ ] 임시 파일/로그가 포함되지 않았는가
- [ ] 커밋/PR/이슈 형식이 `.codex/skills` 문서와 충돌하지 않는가
