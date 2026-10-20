"""Runs this package's playbooks against a single host, for a release of the GitHub repository it comes from."""

import argparse
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass
from importlib.metadata import distribution
from pathlib import Path

PACKAGE_DIR = Path(__file__).resolve().parent
PLAYBOOKS_DIR = PACKAGE_DIR / "playbooks"

TARGET = re.compile(r"(?P<user>[^@\s]+)@(?P<host>[^@\s]+)")
VERSION_TAG = re.compile(r"v?\d+\.\d+\.\d+(?:-[0-9A-Za-z.-]+)?")
GITHUB_REMOTE = re.compile(
    r"(?:git@github\.com:|ssh://git@github\.com/|https://github\.com/)(?P<owner>[^/]+)/(?P<repo>[^/]+?)(?:\.git)?/?"
)


@dataclass(frozen=True)
class Target:
    user: str
    host: str


def parse_target(value: str) -> Target:
    match = TARGET.fullmatch(value)
    if match is None:
        raise argparse.ArgumentTypeError(f"expected user@host, got {value!r}")
    return Target(**match.groupdict())


def parse_version_tag(value: str) -> str:
    if VERSION_TAG.fullmatch(value) is None:
        raise argparse.ArgumentTypeError(f"expected a release tag such as v1.2.3, got {value!r}")
    return value


def parse_github_repository(url: str) -> str:
    match = GITHUB_REMOTE.fullmatch(url)
    if match is None:
        raise ValueError(f"{url!r} is not a GitHub repository")
    return f"{match['owner']}/{match['repo']}"


def vcs_url(direct_url: dict) -> str | None:
    """The repository URL of a PEP 610 direct_url.json, when the package was installed from version control."""
    return direct_url["url"] if "vcs_info" in direct_url else None


def installed_repository_url() -> str | None:
    """The git repository this package was installed from, e.g. by uvx --from git+https://github.com/OWNER/REPO."""
    direct_url = distribution("todoapp-deploy").read_text("direct_url.json")
    return vcs_url(json.loads(direct_url)) if direct_url else None


def origin_remote_url() -> str:
    """The origin remote of the clone this package runs from."""
    result = subprocess.run(["git", "remote", "get-url", "origin"], cwd=PACKAGE_DIR, capture_output=True, text=True)
    if result.returncode != 0:
        raise ValueError("this package wasn't installed from git, nor does it run from a clone with an origin remote")
    return result.stdout.strip()


def deployed_repository() -> str:
    """The OWNER/REPO whose releases to deploy: the GitHub repository this package was installed or cloned from."""
    return parse_github_repository(installed_repository_url() or origin_remote_url())


def release_variables(repository: str, version_tag: str) -> dict[str, str]:
    return {
        "github_repository": repository,
        "image_repository": repository.lower(),
        "release_tag": version_tag,
        "app_version": version_tag.removeprefix("v"),
    }


def argument_parser(description: str) -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=description)
    parser.add_argument("target", type=parse_target, help="the Ubuntu host to set up, as user@host")
    parser.add_argument(
        "-K", "--ask-become-pass", action="store_true", help="ask for the user's sudo password on the host"
    )
    return parser


def add_release_argument(parser: argparse.ArgumentParser) -> None:
    parser.add_argument("version", type=parse_version_tag, help="the release tag to install, e.g. v1.2.3")


def release_variables_of(args: argparse.Namespace) -> dict[str, str]:
    """The release variables of the version in args, when the script takes one."""
    if "version" not in args:
        return {}
    try:
        repository = deployed_repository()
    except ValueError as error:
        sys.exit(
            f"Can't tell which GitHub repository to deploy: {error}.\nInstall this package from GitHub, e.g. with "
            "uvx --from git+https://github.com/OWNER/REPO@v1.2.3#subdirectory=deploy, or run it from a clone."
        )
    return release_variables(repository, args.version)


def run_playbook(
    playbook: str,
    args: argparse.Namespace,
    variables: dict[str, object] | None = None,
    secrets: dict[str, str] | None = None,
) -> int:
    """Runs a playbook against args.target, passing secrets as environment variables so they never reach argv."""
    become = ["--ask-become-pass"] if args.ask_become_pass else []
    extra_variables = release_variables_of(args) | (variables or {})
    command = [
        str(Path(sys.executable).parent / "ansible-playbook"),
        *become,
        "--inventory",
        f"{args.target.host},",
        "--user",
        args.target.user,
        "--extra-vars",
        json.dumps(extra_variables),
        str(PLAYBOOKS_DIR / playbook),
    ]
    environment = os.environ | {"ANSIBLE_CONFIG": str(PLAYBOOKS_DIR / "ansible.cfg")} | (secrets or {})
    return subprocess.run(command, env=environment).returncode
