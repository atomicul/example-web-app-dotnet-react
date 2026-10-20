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

from todoapp_deploy.passwords import generate_password

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


def opens(path: Path, password: str) -> bool:
    """Whether the file at path is a PFX that opens with password."""
    try:
        pkcs12.load_pkcs12(path.read_bytes(), password.encode())
    except OSError, ValueError:
        return False
    return True


def owner_only(path: str, flags: int) -> int:
    return os.open(path, flags, 0o600)


def read_password() -> str | None:
    """The password in TODOAPP_CERTIFICATE_PASSWORD, or else one typed twice; None when Enter asks to generate one."""
    if password := os.environ.get(PASSWORD_VARIABLE):
        return password
    password = getpass.getpass("Password for the PFX, or Enter to generate one: ")
    if not password:
        return None
    if getpass.getpass("Repeat the password: ") != password:
        sys.exit("The passwords don't match")
    return password


def main() -> None:
    parser = argparse.ArgumentParser(description=__doc__.partition(": ")[2])
    parser.add_argument("path", type=Path, help="where to write the PFX file; it must not exist yet")
    args = parser.parse_args()
    if args.path.exists():
        sys.exit(f"{args.path} already exists; replacing a certificate in use logs every user out")

    given_password = read_password()
    password = given_password or generate_password()
    generate(args.path, password)
    print(f"Wrote {args.path}. Keep a copy of it and its password: the app can't read its keys without them.")
    if given_password is None:
        print(f"Its generated password, shown only this once: {password}")


def check() -> None:
    parser = argparse.ArgumentParser(
        description=f"checks that a PFX certificate opens with its password, from {PASSWORD_VARIABLE} or asked for"
    )
    parser.add_argument("path", type=Path, help="the PFX file to check")
    args = parser.parse_args()
    password = os.environ.get(PASSWORD_VARIABLE) or getpass.getpass("Password of the PFX: ")
    if not opens(args.path, password):
        sys.exit(f"{args.path} isn't a PFX that opens with this password")
