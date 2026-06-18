---
name: auto-pr
description: GitHub PR을 자동으로 생성하는 스킬. 사용자가 "PR 만들어줘", "PR 올려줘", "pull request 생성", "변경사항 PR", "브랜치 PR" 등을 언급하면 반드시 이 스킬을 사용한다.
---

# Auto PR

Git 변경사항을 확인하고 GitHub PR을 생성한다. Unity 프로젝트 기준으로 동작한다.

## 언어 규칙

- PR 제목: `[라벨] 한글 설명`
- PR 제목 라벨은 저장소에 존재하는 라벨 이름과 정확히 맞춘다.
- PR 본문 전체는 한글로 작성한다.
- 사용자 승인 전에는 PR을 생성하지 않는다.

## 사용 가능한 라벨

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

## 브랜치 기준

- `feature/*`: 새로운 기능 구현
- `fix/*`: 오류 수정
- `refactor/*`: 동작 변경 없는 코드 구조 개선
- `test/*`: 테스트 코드 및 동작 검증
- `chore/*`: 문서, 설정, 패키지, 폴더 정리 등 기타 작업

## 워크플로우

### 1단계: 저장소 상태 파악

```bash
git branch --show-current
git status
git diff --stat
git log origin/main..HEAD --oneline
```

### 2단계: 변경사항 분석

- base 브랜치: 보통 `main` 또는 `develop`
- head 브랜치: 현재 작업 브랜치
- 변경 파일 목록
- 커밋 메시지와 변경 의도
- Unity `.meta` 파일은 PR 설명에서 별도 변경점으로 강조하지 않는다.

### 3단계: PR 제목과 라벨 결정

- 새 기능: `[Feature]`, `Feature`
- 오류 수정: `[Bug]`, `Bug`
- UI 작업: `[UI]`, `UI`
- 그래픽/화면 구성: `[Design]`, `Design`
- 문서 작업: `[Docs]`, `Docs`
- 개선 작업: `[Improve]`, `Improve`
- 개별 미니게임: `[Minigame]`, `Minigame`
- 리팩터링: `[Refactor]`, `Refactor`
- 저장/기록 시스템: `[Save]`, `Save`
- 테스트: `[Test]`, `Test`
- 기타 설정/정리: `[Chore]`, `Chore`

라벨은 여러 개 적용 가능하다. 예: 미니게임 UI 작업은 `Minigame`, `UI`를 함께 붙인다.

### 4단계: PR 본문 작성

```md
# 개요

# 내용

## 변경 영역

# 기타 사항
```

### 5단계: 생성 전 확인

사용자에게 다음을 보여주고 확인받는다.

- Base 브랜치 → Head 브랜치
- PR 제목
- PR 본문 요약
- 적용할 라벨 목록

### 6단계: PR 생성

```bash
gh pr create \
  --base <base-branch> \
  --head <head-branch> \
  --title "<PR 제목>" \
  --body "<PR 본문>" \
  --label "<label1>" \
  --label "<label2>"
```

## 에러 처리

- 커밋되지 않은 변경사항 있음: 커밋 여부를 사용자에게 묻기
- 원격 브랜치 없음: `git push -u origin <branch>` 먼저 실행
- 이미 PR 존재: 기존 PR URL 안내
- 인증 실패: `gh auth login` 안내
- base와 head가 동일: 브랜치 확인 요청

## 전제 조건

- Git 저장소 초기화 완료
- `gh` CLI 설치 및 인증 완료
- 원격 저장소에 브랜치가 push된 상태
