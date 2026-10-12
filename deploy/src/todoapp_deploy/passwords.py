"""Random passwords that SQL Server's password policy accepts and that need no quoting in a connection string."""

import secrets
import string

ALPHABET = string.ascii_letters + string.digits
CHARACTER_CLASSES = (str.islower, str.isupper, str.isdigit)


def generate_password(length: int = 32) -> str:
    while not meets_sql_server_policy(password := "".join(secrets.choice(ALPHABET) for _ in range(length))):
        pass
    return password


def meets_sql_server_policy(password: str) -> bool:
    """SQL Server wants at least 8 characters from three of: lowercase, uppercase, digits and symbols."""
    return len(password) >= 8 and all(any(map(is_class, password)) for is_class in CHARACTER_CLASSES)
