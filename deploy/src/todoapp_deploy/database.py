"""setup-database: installs SQL Server on a host and migrates the app's database to a release."""

import os
import sys

from todoapp_deploy.passwords import generate_password, is_usable_password
from todoapp_deploy.runner import add_release_argument, argument_parser, run_playbook


def password_from(variable: str) -> str | None:
    """The password the environment variable holds, if any, once SQL Server and a connection string can take it."""
    password = os.environ.get(variable)
    if password and not is_usable_password(password):
        sys.exit(
            f"{variable} must hold at least 8 letters and digits only, with a lowercase letter, an uppercase letter "
            "and a digit"
        )
    return password or None


def main() -> None:
    parser = argument_parser(__doc__.partition(": ")[2])
    add_release_argument(parser)
    args = parser.parse_args()
    sa_password = password_from("TODOAPP_SA_PASSWORD")
    database_password = password_from("TODOAPP_DATABASE_PASSWORD")
    variables = {"database_login_password_generated": database_password is None}
    secrets = {
        "TODOAPP_SA_PASSWORD": sa_password or generate_password(),
        "TODOAPP_DATABASE_PASSWORD": database_password or generate_password(),
    }
    sys.exit(run_playbook("database.yml", args, variables=variables, secrets=secrets))
