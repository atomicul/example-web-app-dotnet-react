import stat

import pytest
from cryptography.hazmat.primitives.serialization import pkcs12

from todoapp_deploy.certificate import COMMON_NAME, generate, opens, read_password


def test_generate_writes_a_pfx_that_opens_with_its_password(tmp_path):
    path = tmp_path / "dataprotection.pfx"

    generate(path, "s3cret")

    pfx = pkcs12.load_pkcs12(path.read_bytes(), b"s3cret")
    assert pfx.key is not None
    assert pfx.cert.certificate.subject.rfc4514_string() == f"CN={COMMON_NAME}"
    assert stat.S_IMODE(path.stat().st_mode) == 0o600


def test_generate_does_not_open_with_another_password(tmp_path):
    path = tmp_path / "dataprotection.pfx"

    generate(path, "s3cret")

    with pytest.raises(ValueError):
        pkcs12.load_pkcs12(path.read_bytes(), b"wrong")


def test_generate_refuses_to_replace_a_file(tmp_path):
    path = tmp_path / "dataprotection.pfx"
    path.write_bytes(b"in use")

    with pytest.raises(FileExistsError):
        generate(path, "s3cret")

    assert path.read_bytes() == b"in use"


def test_opens_with_the_pfx_password(tmp_path):
    path = tmp_path / "dataprotection.pfx"
    generate(path, "s3cret")

    assert opens(path, "s3cret")
    assert not opens(path, "wrong")


def test_opens_rejects_a_file_that_is_not_a_pfx(tmp_path):
    path = tmp_path / "dataprotection.pfx"
    path.write_bytes(b"not a pfx")

    assert not opens(path, "s3cret")


def test_opens_rejects_a_missing_file(tmp_path):
    assert not opens(tmp_path / "missing.pfx", "s3cret")


def test_read_password_takes_the_variable(monkeypatch):
    monkeypatch.setenv("TODOAPP_CERTIFICATE_PASSWORD", "s3cret")

    assert read_password() == "s3cret"


def test_read_password_is_none_when_enter_asks_to_generate_one(monkeypatch):
    monkeypatch.delenv("TODOAPP_CERTIFICATE_PASSWORD", raising=False)
    monkeypatch.setattr("getpass.getpass", lambda prompt: "")

    assert read_password() is None


def test_read_password_exits_when_the_typed_passwords_differ(monkeypatch):
    monkeypatch.delenv("TODOAPP_CERTIFICATE_PASSWORD", raising=False)
    answers = iter(["s3cret", "other"])
    monkeypatch.setattr("getpass.getpass", lambda prompt: next(answers))

    with pytest.raises(SystemExit):
        read_password()
