#!/bin/bash

set -x
set -e

if command -v pyenv 1>/dev/null 2>&1; then
  eval "$(pyenv init -)"
fi
eval "$(pyenv virtualenv-init -)"

poetry self add "poetry-dynamic-versioning[plugin]"

pyenv virtualenv 3.10.9 myenv
pyenv activate myenv
git status
poetry install --verbose --no-interaction
poetry build -v
