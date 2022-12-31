#!/bin/bash

set -x
set -e

if command -v pyenv 1>/dev/null 2>&1; then
  eval "$(pyenv init -)"
fi
eval "$(pyenv virtualenv-init -)"

pyenv virtualenv 3.10.9 myenv
pyenv activate myenv
git status
poetry install --verbose --no-interactive
poetry build -v
