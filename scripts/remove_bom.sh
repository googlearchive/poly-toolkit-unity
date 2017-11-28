#!/bin/bash

# Copyright 2017 Google Inc. All rights reserved.
#
# Licensed under the Apache License, Version 2.0 (the "License");
# you may not use this file except in compliance with the License.
# You may obtain a copy of the License at
#
#    https://www.apache.org/licenses/LICENSE-2.0
#
# Unless required by applicable law or agreed to in writing, software
# distributed under the License is distributed on an "AS IS" BASIS,
# WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
# See the License for the specific language governing permissions and
# limitations under the License.

# Abort with an error message when a command fails.
set -e
trap "echo '*** Aborted due to error.'" ERR

basedir=$(dirname $0)/..
cd "$basedir"

find . | \
    grep -v './.git' | \
    grep -v './PolyToolkitUnity/Temp/' | \
    grep -v './PolyToolkitUnity/Library/' | \
    grep -v '/ThirdParty/' | \
    egrep '\.(cs|shader)$' | while read file; do
  # Remove BOM
  echo "Processing: $file"
  sed -i -e '1s/^\xEF\xBB\xBF//' "$file"
done

echo "Done."


