# Deploy

Sets up the todo app and its database on a single Ubuntu host with Ansible. uv is the only requirement: it installs
the pinned Python, Ansible and every other dependency from `uv.lock`.

## Requirements

- On this machine: uv, and a clone of the repository whose releases you deploy. The scripts read `OWNER/REPO` from the
  clone's `origin` remote, so a fork deploys its own GitHub releases and `ghcr.io` images, which must be public.
- The host: Ubuntu 24.04 on x86_64 with at least 2 GB of RAM, which SQL Server needs, reachable over SSH with a key, as
  a user who can `sudo`. Pass `-K` to any script when sudo asks that user for a password.

## Usage

Run the scripts from `deploy/`, with the host as `user@host` and a release tag. For each release, set the database up
first, then the app:

```sh
uv run setup-database ubuntu@203.0.113.10 v1.2.3
uv run setup-webapp ubuntu@203.0.113.10 v1.2.3
```

Both scripts can be run again: a run only changes what differs from the release and settings it is given, so the same
two commands upgrade the host to a new release.

### `setup-database`

Installs SQL Server and Docker, applies the release's migrations with the `migrate` command of its Docker image, and
creates the `todoapp` login the app connects with, which can read and write the app's tables but not change them.

The first run generates two passwords:

- The `todoapp` login's password, which it **shows once** at the end of the run and stores nowhere. `setup-webapp`
  asks for it.
- The `sa` password, kept for later migrations in `/etc/todoapp/database-admin.env`, which only root can read.

SQL Server listens on `127.0.0.1` only. It runs as the Express edition, which is free in production; set
`mssql_edition` in `playbooks/group_vars/all.yml` before the first run to use a licensed one.

### `setup-webapp`

Installs the ASP.NET Core runtime and the release's zip, and runs the app as the `todoapp` systemd service on port
`8080`, then waits for `/health` to answer `Healthy`. It serves plain HTTP: put a reverse proxy in front of it for
HTTPS, and set the API's `TrustedProxies` (see the [root README](../README.md#rate-limiting)).

It only asks for what the host doesn't have yet:

- The `todoapp` database password, which `setup-database` showed.
- The PFX certificate that encrypts the app's Data Protection keys: press Enter to generate one, saved locally as its
  backup, or type the path of an existing PFX. Either way, it then asks for the PFX's password.

To change either of them on a host that has them, pass `--change-database-password` or `--change-certificate`.
Replacing the certificate logs every user out, and losing it does too: keep the PFX and its password.

### `generate-certificate`

`uv run generate-certificate dataprotection.pfx` writes a self-signed certificate that `setup-webapp` accepts, after
asking for its password. It refuses to overwrite a file.

## On the host

| Path | Holds |
|---|---|
| `/etc/todoapp/database-admin.env` | The `sa` password and connection string, for migrations (root only) |
| `/etc/todoapp/database.env` | The app's connection string (root only; systemd passes it to the app) |
| `/etc/todoapp/certificate.env`, `dataprotection.pfx` | The certificate's settings (root only) and the PFX (readable by the app) |
| `/opt/todoapp/releases/<tag>` | Each installed release; `/opt/todoapp/current` links to the one running |
| `/opt/dotnet/<version>` | The ASP.NET Core runtime |

`journalctl -u todoapp` shows the app's logs.

## Pinned versions

`playbooks/group_vars/all.yml` pins SQL Server and its tools, Docker, the ASP.NET Core runtime, and the signing keys of
their repositories by checksum. `pyproject.toml`, `uv.lock` and `.python-version` pin Python and the Python packages.
The few packages that come from Ubuntu's own archive, such as `unzip` and `libicu74`, follow Ubuntu 24.04's updates.

## Checks

The root `make check` formats and lints the Python with ruff, runs its tests with pytest, and lints and syntax-checks
the playbooks with ansible-lint and `ansible-playbook --syntax-check`.
