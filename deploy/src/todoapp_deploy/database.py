"""setup-database: installs SQL Server on a host and migrates the app's database to a release."""

import sys

from todoapp_deploy.passwords import generate_password
from todoapp_deploy.runner import argument_parser, run_playbook


def main() -> None:
    args = argument_parser(__doc__.partition(": ")[2]).parse_args()
    secrets = {
        "TODOAPP_SA_PASSWORD": generate_password(),
        "TODOAPP_DATABASE_PASSWORD": generate_password(),
    }
    sys.exit(run_playbook("database.yml", args, secrets=secrets))
