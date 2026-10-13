"""setup-webapp: installs a release of the app on a host as a systemd service."""

import os
import sys

from todoapp_deploy.runner import add_release_argument, argument_parser, run_playbook


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
    args = parser.parse_args()
    variables = {
        "todoapp_change_database_password": args.change_database_password,
        "todoapp_change_certificate": args.change_certificate,
        "todoapp_controller_directory": os.getcwd(),
    }
    sys.exit(run_playbook("webapp.yml", args, variables=variables))
