#!/bin/bash

set -x
set -e

if command -v pyenv 1>/dev/null 2>&1; then
  eval "$(pyenv init -)"
fi
eval "$(pyenv virtualenv-init -)"

env

pyenv virtualenv 3.10.9 myenv
pyenv activate myenv
git status

env

pip install colorama
pip install boto3
pip freeze

pip install .
pip freeze
python -c 'import peaceful_pie'
python -c 'import peaceful_pie; print(peaceful_pie.__version__)'

poetry self update --quiet

# curl -sSL https://raw.githubusercontent.com/python-poetry/poetry/master/get-poetry.py | POETRY_UNINSTALL=1 python -
# curl -sSL https://install.python-poetry.org | python3 -

poetry self add --quiet "poetry-dynamic-versioning[plugin]"

poetry install --quiet --no-interaction
poetry build --quiet
find . -cmin -1
ls deps
