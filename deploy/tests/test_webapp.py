import argparse

import pytest

from todoapp_deploy.webapp import parse_components_directory


def test_parse_components_directory_resolves_the_directory(tmp_path, monkeypatch):
    (tmp_path / "components").mkdir()
    monkeypatch.chdir(tmp_path)

    assert parse_components_directory("components") == tmp_path.resolve() / "components"


@pytest.mark.parametrize("name", ["missing", "emails.yaml"])
def test_parse_components_directory_rejects_anything_but_a_directory(tmp_path, name):
    (tmp_path / "emails.yaml").touch()

    with pytest.raises(argparse.ArgumentTypeError):
        parse_components_directory(str(tmp_path / name))
