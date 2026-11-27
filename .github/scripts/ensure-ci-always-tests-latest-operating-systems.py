#!/usr/bin/env python3
"""Entry point of ensure-ci-always-tests-latest-operating-systems.yml; ensure_ci_always_tests_latest_operating_systems/commands.py lists the commands."""
import os
import sys

# python3 -I leaves this folder off sys.path, and the package lives next to this script.
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from ensure_ci_always_tests_latest_operating_systems.commands import main  # noqa: E402

sys.exit(main(sys.argv[1:]))
