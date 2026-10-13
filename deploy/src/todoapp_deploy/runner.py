"""Runs this project's playbooks against a single host, for the release of this repository given by its tag."""

import argparse
import json
import os
import re
import subprocess
import sys
from dataclasses import dataclass
from pathlib import Path

PROJECT_DIR = Path(__file__).resolve().parents[2]

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


def parse_github_repository(remote_url: str) -> str:
    match = GITHUB_REMOTE.fullmatch(remote_url)
    if match is None:
        raise ValueError(f"the origin remote {remote_url!r} is not a GitHub repository")
    return f"{match['owner']}/{match['repo']}"


def origin_repository() -> str:
    """The OWNER/REPO of the GitHub repository this project was cloned from, whose releases it deploys."""
    remote_url = subprocess.run(
        ["git", "remote", "get-url", "origin"], cwd=PROJECT_DIR, capture_output=True, text=True, check=True
    ).stdout.strip()
    return parse_github_repository(remote_url)


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
        repository = origin_repository()
    except (subprocess.CalledProcessError, ValueError) as error:
        sys.exit(f"Can't tell which GitHub repository to deploy: {error}")
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
        str(PROJECT_DIR / "playbooks" / playbook),
    ]
    environment = os.environ | {"ANSIBLE_CONFIG": str(PROJECT_DIR / "ansible.cfg")} | (secrets or {})
    return subprocess.run(command, env=environment).returncode
