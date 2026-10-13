"""setup-webapp: installs a release of the app and its Dapr sidecar on a host as systemd services."""

import argparse
import os
import sys
from pathlib import Path

from todoapp_deploy.runner import add_release_argument, argument_parser, run_playbook


def parse_components_directory(value: str) -> Path:
    path = Path(value).expanduser()
    if not path.is_dir():
        raise argparse.ArgumentTypeError(f"expected a directory of Dapr component YAML files, got {value!r}")
    return path.resolve()


def main() -> None:
    parser = argument_parser(__doc__.partition(": ")[2])
    add_release_argument(parser)
    parser.add_argument(
        "--change-database-password",
        action="store_true",
        help="ask for the database password even though the host already has one",
    )
    parser.add_argument(
        "--change-certificate",
        action="store_true",
        help="replace the host's Data Protection certificate, which logs every user out",
    )
    parser.add_argument(
        "--dapr-components",
        type=parse_components_directory,
        help="replace the Dapr sidecar's components on the host with the YAML files of this directory",
    )
    args = parser.parse_args()
    variables = {
        "todoapp_change_database_password": args.change_database_password,
        "todoapp_change_certificate": args.change_certificate,
        "todoapp_controller_directory": os.getcwd(),
        "dapr_components_source": str(args.dapr_components or ""),
    }
    sys.exit(run_playbook("webapp.yml", args, variables=variables))
