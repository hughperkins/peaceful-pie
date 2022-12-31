#!/bin/bash

set -x
set -e

if command -v pyenv 1>/dev/null 2>&1; then
  eval "$(pyenv init -)"
fi
eval "$(pyenv virtualenv-init -)"

curl -sSL https://raw.githubusercontent.com/python-poetry/poetry/master/get-poetry.py | POETRY_UNINSTALL=1 python -
curl -sSL https://install.python-poetry.org | python3 -

poetry self add "poetry-dynamic-versioning[plugin]"

pyenv virtualenv 3.10.9 myenv
pyenv activate myenv
git status
poetry install --verbose --no-interaction
poetry build -v
