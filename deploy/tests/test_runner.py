import argparse

import pytest

from todoapp_deploy import runner
from todoapp_deploy.runner import (
    Target,
    argument_parser,
    installed_tag,
    inventory,
    parse_github_repository,
    parse_target,
    parse_version_tag,
    release_variables,
    release_variables_of,
    vcs_url,
)


def test_parse_target_splits_user_and_host():
    assert parse_target("deploy@example.com") == Target(user="deploy", host="example.com")


@pytest.mark.parametrize("value", ["example.com", "@example.com", "deploy@", "a@b@c", "deploy @example.com"])
def test_parse_target_rejects_anything_but_user_at_host(value):
    with pytest.raises(argparse.ArgumentTypeError):
        parse_target(value)


@pytest.mark.parametrize("value", ["v1.2.3", "1.2.3", "v1.2.3-rc.1"])
def test_parse_version_tag_accepts_release_tags(value):
    assert parse_version_tag(value) == value


@pytest.mark.parametrize("value", ["latest", "v1.2", "main", "v1.2.3 "])
def test_parse_version_tag_rejects_anything_else(value):
    with pytest.raises(argparse.ArgumentTypeError):
        parse_version_tag(value)


@pytest.mark.parametrize(
    "remote_url",
    [
        "git@github.com:Owner/Repo.git",
        "git@github.com:Owner/Repo",
        "ssh://git@github.com/Owner/Repo.git",
        "https://github.com/Owner/Repo.git",
        "https://github.com/Owner/Repo/",
        "https://github.com/Owner/Repo",
    ],
)
def test_parse_github_repository_reads_ssh_and_https_urls(remote_url):
    assert parse_github_repository(remote_url) == "Owner/Repo"


def test_parse_github_repository_rejects_other_hosts():
    with pytest.raises(ValueError):
        parse_github_repository("git@gitlab.com:Owner/Repo.git")


def test_release_variables_lowercase_the_image_and_drop_the_tag_prefix():
    assert release_variables("Owner/Repo", "v1.2.3") == {
        "github_repository": "Owner/Repo",
        "image_repository": "owner/repo",
        "release_tag": "v1.2.3",
        "app_version": "1.2.3",
    }


def test_release_variables_of_a_script_without_a_release_are_empty():
    assert release_variables_of(argparse.Namespace(targets=[Target("deploy", "example.com")])) == {}


def test_vcs_url_reads_the_repository_a_git_install_came_from():
    direct_url = {
        "url": "https://github.com/Owner/Repo",
        "vcs_info": {"vcs": "git", "commit_id": "6110fa6", "requested_revision": "v1.2.3"},
        "subdirectory": "deploy",
    }

    assert vcs_url(direct_url) == "https://github.com/Owner/Repo"


def test_vcs_url_of_an_editable_install_is_none():
    assert vcs_url({"url": "file:///home/me/repo/deploy", "dir_info": {"editable": True}}) is None


def test_installed_tag_reads_the_release_a_git_install_asked_for():
    direct_url = {"url": "https://github.com/Owner/Repo", "vcs_info": {"vcs": "git", "requested_revision": "v1.2.3"}}

    assert installed_tag(direct_url) == "v1.2.3"


@pytest.mark.parametrize(
    "direct_url",
    [
        {"url": "https://github.com/Owner/Repo", "vcs_info": {"vcs": "git", "requested_revision": "main"}},
        {"url": "https://github.com/Owner/Repo", "vcs_info": {"vcs": "git", "commit_id": "6110fa6"}},
        {"url": "file:///home/me/repo/deploy", "dir_info": {"editable": True}},
    ],
)
def test_installed_tag_of_a_branch_commit_or_editable_install_is_none(direct_url):
    assert installed_tag(direct_url) is None


def test_release_variables_of_use_the_given_version(monkeypatch):
    monkeypatch.setattr(runner, "deployed_repository", lambda: "Owner/Repo")
    monkeypatch.setattr(runner, "default_version_tag", lambda repository: pytest.fail("looked up a default"))

    variables = release_variables_of(argparse.Namespace(version="v1.2.3"))

    assert variables["release_tag"] == "v1.2.3"


def test_release_variables_of_default_to_the_installed_or_latest_release(monkeypatch):
    monkeypatch.setattr(runner, "deployed_repository", lambda: "Owner/Repo")
    monkeypatch.setattr(runner, "default_version_tag", lambda repository: "v2.0.0")

    variables = release_variables_of(argparse.Namespace(version=None))

    assert variables["release_tag"] == "v2.0.0"


def test_inventory_connects_to_each_host_as_its_user():
    targets = [Target("ubuntu", "203.0.113.10"), Target("deploy", "203.0.113.11")]

    assert inventory(targets) == {
        "all": {
            "hosts": {
                "203.0.113.10": {"ansible_user": "ubuntu"},
                "203.0.113.11": {"ansible_user": "deploy"},
            }
        }
    }


def test_argument_parser_takes_several_hosts_when_asked():
    args = argument_parser("", several_hosts=True).parse_args(["ubuntu@a.example", "deploy@b.example"])

    assert args.targets == [Target("ubuntu", "a.example"), Target("deploy", "b.example")]


def test_argument_parser_takes_a_single_host_by_default():
    assert argument_parser("").parse_args(["ubuntu@a.example"]).targets == [Target("ubuntu", "a.example")]

    with pytest.raises(SystemExit):
        argument_parser("").parse_args(["ubuntu@a.example", "deploy@b.example"])
