"""setup-database: installs SQL Server on a host and migrates the app's database to a release."""

import sys

from todoapp_deploy.passwords import generate_password
from todoapp_deploy.runner import add_release_argument, argument_parser, run_playbook


def main() -> None:
    parser = argument_parser(__doc__.partition(": ")[2])
    add_release_argument(parser)
    args = parser.parse_args()
    secrets = {
        "TODOAPP_SA_PASSWORD": generate_password(),
        "TODOAPP_DATABASE_PASSWORD": generate_password(),
    }
    sys.exit(run_playbook("database.yml", args, secrets=secrets))
