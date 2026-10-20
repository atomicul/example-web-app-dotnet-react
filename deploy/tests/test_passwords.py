import pytest

from todoapp_deploy.passwords import ALPHABET, generate_password, is_usable_password, meets_sql_server_policy


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


def test_is_usable_password_accepts_a_generated_password():
    assert is_usable_password(generate_password())


@pytest.mark.parametrize("password", ["NoDigitsAtAll", "Has;Semicolon1", "Has'Quote1", "Has Space1"])
def test_is_usable_password_rejects_weak_passwords_and_ones_that_need_quoting(password):
    assert not is_usable_password(password)
