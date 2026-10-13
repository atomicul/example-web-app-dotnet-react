"""setup-tls: gets a Let's Encrypt certificate for the app's domain with certbot and serves the app over HTTPS."""

import argparse
import re
import sys

from todoapp_deploy.runner import argument_parser, run_playbook

DOMAIN = re.compile(r"(?=.{1,253}$)(?:(?!-)[a-z0-9-]{1,63}(?<!-)\.)+[a-z]{2,63}", re.IGNORECASE)


def parse_domain(value: str) -> str:
    if DOMAIN.fullmatch(value) is None:
        raise argparse.ArgumentTypeError(f"expected a domain name such as todo.example.com, got {value!r}")
    return value.lower()


def main() -> None:
    parser = argument_parser(__doc__.partition(": ")[2])
    parser.add_argument("domain", type=parse_domain, help="the domain name that points at the host")
    parser.add_argument("--email", help="the address Let's Encrypt registers the host's account with")
    args = parser.parse_args()
    variables = {
        "todoapp_domain": args.domain,
        "todoapp_acme_email": args.email or "",
    }
    sys.exit(run_playbook("tls.yml", args, variables=variables))
