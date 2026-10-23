"""setup-webapp: installs a release of the app and its Dapr sidecar on one or more hosts as systemd services."""

import argparse
import json
import os
import sys
from pathlib import Path

from todoapp_deploy.runner import add_release_argument, argument_parser, run_playbook


def parse_components_directory(value: str) -> Path:
    path = Path(value).expanduser()
    if not path.is_dir():
        raise argparse.ArgumentTypeError(f"expected a directory of Dapr component YAML files, got {value!r}")
    return path.resolve()


def parse_certificate_file(value: str) -> Path:
    path = Path(value).expanduser()
    if not path.is_file():
        raise argparse.ArgumentTypeError(f"expected a PFX file, got {value!r}")
    return path.resolve()


def parse_env_file(value: str) -> Path:
    path = Path(value).expanduser()
    if not path.is_file():
        raise argparse.ArgumentTypeError(f"expected a file of environment variables, got {value!r}")
    return path.resolve()


def parse_settings_file(value: str) -> Path:
    path = Path(value).expanduser()
    try:
        settings = json.loads(path.read_text())
    except OSError, ValueError:
        settings = None
    if not isinstance(settings, dict):
        raise argparse.ArgumentTypeError(f"expected a JSON file of app settings, like appsettings.json, got {value!r}")
    return path.resolve()


def main() -> None:
    parser = argument_parser(__doc__.partition(": ")[2], several_hosts=True)
    add_release_argument(parser)
    parser.add_argument(
        "--certificate",
        type=parse_certificate_file,
        help="the PFX to encrypt the keys protecting sessions with, its password in TODOAPP_CERTIFICATE_PASSWORD; "
        "replacing the host's logs every user out",
    )
    parser.add_argument(
        "--dapr-components",
        type=parse_components_directory,
        help="replace the Dapr sidecar's components on the host with the YAML files of this directory",
    )
    parser.add_argument(
        "--settings",
        type=parse_settings_file,
        help="replace the deployment's app settings on the host with this JSON file, under the ones the deploy sets",
    )
    parser.add_argument(
        "--env-file",
        type=parse_env_file,
        help="replace the deployment's environment variables on the host, such as its connection string, with this "
        "file in systemd's EnvironmentFile format",
    )
    args = parser.parse_args()
    variables = {
        "todoapp_controller_directory": os.getcwd(),
        "dapr_components_source": str(args.dapr_components or ""),
        "todoapp_settings_source": str(args.settings or ""),
        "todoapp_env_file_source": str(args.env_file or ""),
        "todoapp_certificate_source": str(args.certificate or ""),
    }
    sys.exit(run_playbook("webapp.yml", args, variables=variables))
