#!/bin/bash

set -e

if command -v pyenv 1>/dev/null 2>&1; then
  eval "$(pyenv init -)"
fi
eval "$(pyenv virtualenv-init -)"

pyenv virtualenv 3.10.9 myenv
pyenv activate myenv

set -x

poetry self update --quiet
poetry self add --quiet "poetry-dynamic-versioning[plugin]"
poetry install --quiet --no-interaction

python -c 'import peaceful_pie'
python -c 'import peaceful_pie; print(peaceful_pie.__version__)'

poetry build --quiet
find . -cmin -1
ls -lh dist
python -m twine upload dist/*
