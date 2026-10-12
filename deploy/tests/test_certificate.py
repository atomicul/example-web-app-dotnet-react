import stat

import pytest
from cryptography.hazmat.primitives.serialization import pkcs12

from todoapp_deploy.certificate import COMMON_NAME, generate


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
