import argparse

import pytest

from todoapp_deploy.webapp import parse_components_directory, parse_settings_file


def test_parse_components_directory_resolves_the_directory(tmp_path, monkeypatch):
    (tmp_path / "components").mkdir()
    monkeypatch.chdir(tmp_path)

    assert parse_components_directory("components") == tmp_path.resolve() / "components"


@pytest.mark.parametrize("name", ["missing", "emails.yaml"])
def test_parse_components_directory_rejects_anything_but_a_directory(tmp_path, name):
    (tmp_path / "emails.yaml").touch()

    with pytest.raises(argparse.ArgumentTypeError):
        parse_components_directory(str(tmp_path / name))


def test_parse_settings_file_resolves_a_json_object(tmp_path, monkeypatch):
    (tmp_path / "appsettings.json").write_text('{"TrustedProxies": {"Networks": ["10.0.0.0/8"]}}')
    monkeypatch.chdir(tmp_path)

    assert parse_settings_file("appsettings.json") == tmp_path.resolve() / "appsettings.json"


@pytest.mark.parametrize("content", ["[]", '{"RateLimits":', '"Production"'])
def test_parse_settings_file_rejects_anything_but_a_json_object(tmp_path, content):
    (tmp_path / "appsettings.json").write_text(content)

    with pytest.raises(argparse.ArgumentTypeError):
        parse_settings_file(str(tmp_path / "appsettings.json"))


@pytest.mark.parametrize("name", ["missing.json", "directory"])
def test_parse_settings_file_rejects_anything_but_a_file(tmp_path, name):
    (tmp_path / "directory").mkdir()

    with pytest.raises(argparse.ArgumentTypeError):
        parse_settings_file(str(tmp_path / name))
