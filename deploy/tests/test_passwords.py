import pytest

from todoapp_deploy.passwords import ALPHABET, generate_password, meets_sql_server_policy


def test_generate_password_meets_the_policy_with_only_letters_and_digits():
    for _ in range(1000):
        password = generate_password()
        assert len(password) == 32
        assert meets_sql_server_policy(password)
        assert set(password) <= set(ALPHABET)


def test_generate_password_is_random():
    assert generate_password() != generate_password()


@pytest.mark.parametrize("password", ["Sh0rt", "alllowercase123", "ALLUPPERCASE123", "NoDigitsAtAll"])
def test_meets_sql_server_policy_rejects_weak_passwords(password):
    assert not meets_sql_server_policy(password)
