#!/bin/sh

# 현재 디렉토리의 경로를 가져옵니다
current_dir=$(pwd)

# Git hooks 디렉토리 경로 설정
hook_dir="$current_dir/.git/hooks"

# hooks 디렉토리가 존재하는지 확인
if [ -d "$hook_dir" ]; then
  # hooks 디렉토리로 이동
  cd "$hook_dir"
  
  # pre-commit 링크 생성 (기존 파일이 있을 경우 백업)
  if [ -f "pre-commit" ]; then
    mv pre-commit pre-commit.backup
  fi
  ln -s "$current_dir/hooks/pre-commit" pre-commit

  echo "Git hooks have been set up successfully."
else
  echo "Error: .git/hooks directory does not exist. Are you sure this is a Git repository?"
fi
