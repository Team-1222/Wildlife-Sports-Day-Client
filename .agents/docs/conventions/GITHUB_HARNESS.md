# GitHub 하네스

## 운영 기준

- 현재 기준: 1인 클라이언트 개발 흐름
- 과한 Git Flow를 강제하지 않는다.
- 기본은 `main`, `develop` 중심으로 단순하게 운영한다.
- 작은 작업은 브랜치 없이 `develop`에서 직접 작업 가능하다.
- 범위가 크거나 되돌릴 가능성이 있으면 작업 브랜치를 만든다.
- 커밋, PR, 이슈의 상세 절차는 `.codex/skills/*/SKILL.md`를 따른다.

## 관련 스킬

- `.codex/skills/auto-commit/SKILL.md`: 커밋 생성
- `.codex/skills/auto-pr/SKILL.md`: PR 생성
- `.codex/skills/github-issue/SKILL.md`: GitHub 이슈 생성

## 브랜치 기준

- `main`: 안정 버전
- `develop`: 일상 개발 통합
- `feature/*`: 새로운 기능 구현
- `fix/*`: 오류 수정
- `refactor/*`: 동작 변경 없는 코드 구조 개선
- `test/*`: 테스트 코드 및 동작 검증
- `chore/*`: 문서, 설정, 패키지, 폴더 정리 등 기타 작업

## 최소 규칙

- 커밋 전 변경 파일을 확인한다.
- Unity `.meta` 파일은 대응 원본 파일과 함께 다룬다.
- 사용자 승인 전에는 커밋, push, PR, 이슈 생성을 하지 않는다.
- PR/이슈 라벨은 현재 GitHub 저장소 라벨 이름과 정확히 맞춘다.
- 검증하지 못한 항목은 결과 보고에 남긴다.
