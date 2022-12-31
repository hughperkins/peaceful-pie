#!/bin/bash

set -x
set -e

pyenv virtualenv 3.10.9 myenv
pyenv activate myenv
poetry install
poetry build
