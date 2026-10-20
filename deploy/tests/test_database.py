import pytest

from todoapp_deploy.database import password_from


def test_password_from_reads_a_usable_password(monkeypatch):
    monkeypatch.setenv("TODOAPP_DATABASE_PASSWORD", "Given1Password")

    assert password_from("TODOAPP_DATABASE_PASSWORD") == "Given1Password"


def test_password_from_an_unset_variable_is_none(monkeypatch):
    monkeypatch.delenv("TODOAPP_DATABASE_PASSWORD", raising=False)

    assert password_from("TODOAPP_DATABASE_PASSWORD") is None


def test_password_from_an_empty_variable_is_none(monkeypatch):
    monkeypatch.setenv("TODOAPP_DATABASE_PASSWORD", "")

    assert password_from("TODOAPP_DATABASE_PASSWORD") is None


def test_password_from_exits_on_a_password_that_needs_quoting_without_showing_it(monkeypatch):
    monkeypatch.setenv("TODOAPP_DATABASE_PASSWORD", "Given1;Password")

    with pytest.raises(SystemExit) as exit_info:
        password_from("TODOAPP_DATABASE_PASSWORD")

    assert "TODOAPP_DATABASE_PASSWORD" in str(exit_info.value)
    assert "Given1;Password" not in str(exit_info.value)
