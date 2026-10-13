import argparse

import pytest

from todoapp_deploy.tls import parse_domain


@pytest.mark.parametrize("value", ["todo.example.com", "example.co", "xn--bcher-kva.example", "a-b.example.org"])
def test_parse_domain_accepts_domain_names(value):
    assert parse_domain(value) == value


def test_parse_domain_lowercases_the_name():
    assert parse_domain("Todo.Example.COM") == "todo.example.com"


@pytest.mark.parametrize(
    "value", ["", "localhost", "203.0.113.10", "user@example.com", "todo .example.com", "-todo.example.com"]
)
def test_parse_domain_rejects_anything_else(value):
    with pytest.raises(argparse.ArgumentTypeError):
        parse_domain(value)
