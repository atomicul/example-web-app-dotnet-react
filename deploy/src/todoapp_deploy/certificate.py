"""generate-certificate: writes a self-signed PFX certificate for the app to encrypt its Data Protection keys with."""

import argparse
import getpass
import os
import sys
from datetime import UTC, datetime, timedelta
from pathlib import Path

from cryptography import x509
from cryptography.hazmat.primitives import hashes
from cryptography.hazmat.primitives.asymmetric import rsa
from cryptography.hazmat.primitives.serialization import BestAvailableEncryption, pkcs12
from cryptography.x509.oid import NameOID

PASSWORD_VARIABLE = "TODOAPP_CERTIFICATE_PASSWORD"
COMMON_NAME = "todoapp-dataprotection"
VALIDITY = timedelta(days=3650)


def generate(path: Path, password: str) -> None:
    """Writes a new key and its self-signed certificate to path as a PFX file only its owner can read."""
    key = rsa.generate_private_key(public_exponent=65537, key_size=4096)
    name = x509.Name([x509.NameAttribute(NameOID.COMMON_NAME, COMMON_NAME)])
    now = datetime.now(UTC)
    certificate = (
        x509.CertificateBuilder()
        .subject_name(name)
        .issuer_name(name)
        .public_key(key.public_key())
        .serial_number(x509.random_serial_number())
        .not_valid_before(now)
        .not_valid_after(now + VALIDITY)
        .add_extension(x509.BasicConstraints(ca=False, path_length=None), critical=True)
        .sign(key, hashes.SHA256())
    )
    pfx = pkcs12.serialize_key_and_certificates(
        COMMON_NAME.encode(), key, certificate, None, BestAvailableEncryption(password.encode())
    )
    with open(path, "xb", opener=owner_only) as file:
        file.write(pfx)


def owner_only(path: str, flags: int) -> int:
    return os.open(path, flags, 0o600)


def read_password() -> str:
    if password := os.environ.get(PASSWORD_VARIABLE):
        return password
    password = getpass.getpass("Password for the PFX: ")
    if not password:
        sys.exit("The password can't be empty")
    if getpass.getpass("Repeat the password: ") != password:
        sys.exit("The passwords don't match")
    return password


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.partition(": ")[2])
    parser.add_argument("path", type=Path, help="where to write the PFX file; it must not exist yet")
    args = parser.parse_args()
    if args.path.exists():
        sys.exit(f"{args.path} already exists; replacing a certificate in use logs every user out")

    generate(args.path, read_password())
    print(f"Wrote {args.path}. Keep a copy of it and its password: the app can't read its keys without them.")
