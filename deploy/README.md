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

To serve HTTPS, point a domain name at the host and, once the app is set up, run `setup-tls` with that domain:

```sh
uv run setup-tls ubuntu@203.0.113.10 todo.example.com --email you@example.com
```

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
`80`, then waits for `/health` to answer `Healthy`. Beside it, it runs the app's Dapr sidecar as the `todoapp-dapr`
service (see [Dapr sidecar](#dapr-sidecar)). It serves plain HTTP until `setup-tls` gives the host a
certificate, and from then on HTTPS on port `443` as well. To terminate HTTPS in a reverse proxy instead, skip
`setup-tls` and set the API's `TrustedProxies` (see the [root README](../README.md#rate-limiting)).

It only asks for what the host doesn't have yet:

- The `todoapp` database password, which `setup-database` showed.
- The PFX certificate that encrypts the app's Data Protection keys: press Enter to generate one, saved locally as its
  backup, or type the path of an existing PFX. Either way, it then asks for the PFX's password.

To change either of them on a host that has them, pass `--change-database-password` or `--change-certificate`.
Replacing the certificate logs every user out, and losing it does too: keep the PFX and its password.

`--dapr-components DIR` replaces the sidecar's components on the host with the `*.yaml` and `*.yml` files of a local
directory, and removes the ones it doesn't have. Without it, the host keeps the components it has.

### `setup-tls`

Gets a Let's Encrypt certificate for the domain with certbot and serves the app over HTTPS with it, redirecting HTTP to
HTTPS (see the [root README](../README.md#https)). It needs:

- The app installed by `setup-webapp`, from a release that serves ACME challenges.
- The domain's DNS pointing at the host, and port `80` reachable from the internet: Let's Encrypt checks that the
  domain is yours by fetching a file that certbot puts in `/var/lib/todoapp/acme`, which the app serves.
- Optionally `--email`, for Let's Encrypt to write to about the account. Without it, the account has no address.

certbot's timer renews the certificate before it expires. Its hook copies each new certificate to `/etc/todoapp/tls`,
where the app can read it, and restarts the app, so the app restarts only when the certificate changes.
`sudo certbot renew --dry-run` on the host tests a renewal. Running `setup-tls` again with the same domain changes
nothing.

The app then sends HSTS, which makes browsers that visited it refuse plain HTTP for 30 days: don't remove HTTPS once
it's served.

### Dapr sidecar

The app reaches cloud services, such as the queue its emails go on, only through a [Dapr](https://dapr.io) sidecar
(see the [root README](../README.md#cloud-services)). `setup-webapp` runs `daprd` as the `todoapp-dapr` user, which
serves the Dapr API only on Unix sockets in `/run/todoapp-dapr` that the app's group can open, and points the app at
them with `Dapr:GrpcEndpoint`.

This project ships no components: which provider's queue or bucket a name leads to, and how the sidecar authenticates
to it, are yours to decide and to keep, e.g. in a private directory next to the PFX backup. Each feature needs its
component in a file of the same name, which also turns the feature on:

| File | Turns on |
|---|---|
| `emails.yaml` | Queueing emails on the output binding named `emails` (`Email:Queue`), in the format the [root README](../README.md#email) describes; without it, the app logs them |

Setting a provider up is manual. For AWS, for example:

1. Create the SQS queue and whatever consumes it, e.g. a Lambda that sends each message with SES.
2. Let the host send to the queue, preferably with an instance profile: the sidecar then needs no keys.
3. Write `emails.yaml` as an [SQS binding](https://docs.dapr.io/reference/components-reference/supported-bindings/sqs/)
   named `emails`. Any secret it needs can come from a secret store component, such as
   [SSM Parameter Store](https://docs.dapr.io/reference/components-reference/supported-secret-stores/aws-parameter-store/),
   in the same directory.
4. Run `uv run setup-webapp user@host v1.2.3 --dapr-components path/to/components`.

`journalctl -u todoapp-dapr` shows whether the sidecar loaded the components.

### `generate-certificate`

`uv run generate-certificate dataprotection.pfx` writes a self-signed certificate that `setup-webapp` accepts, after
asking for its password. It refuses to overwrite a file.

## On the host

| Path | Holds |
|---|---|
| `/etc/todoapp/appsettings.Production.json` | The app's settings other than secrets, from `todoapp_settings` in `playbooks/group_vars/all.yml`; each release links to it |
| `/etc/todoapp/database-admin.env` | The `sa` password and connection string, for migrations (root only) |
| `/etc/todoapp/database.env` | The app's connection string (root only; systemd passes it to the app) |
| `/etc/todoapp/certificate.env`, `dataprotection.pfx` | The certificate's password (root only) and the PFX (readable by the app) |
| `/etc/todoapp/tls/fullchain.pem`, `privkey.pem` | The HTTPS certificate and its key (readable by the app), copied from `/etc/letsencrypt` by `/usr/local/sbin/todoapp-install-certificate` |
| `/etc/todoapp/dapr/components` | The Dapr sidecar's components (only the sidecar's user can read them) |
| `/run/todoapp-dapr` | The sidecar's sockets, which only the app's group can open |
| `/var/lib/todoapp/acme` | The web root certbot writes ACME challenges to, which the app serves |
| `/opt/todoapp/releases/<tag>` | Each installed release; `/opt/todoapp/current` links to the one running |
| `/opt/dotnet/<version>` | The ASP.NET Core runtime |
| `/opt/dapr/<version>` | The Dapr sidecar, `daprd` |

Environment variables override the JSON file, so the two secrets stay in the `.env` files, which systemd passes to
the app. To change another setting, such as `TrustedProxies` or `RateLimits` from the
[root README](../README.md#environment-variables), add it to `todoapp_settings` and run `setup-webapp` again.

`journalctl -u todoapp` shows the app's logs, and `journalctl -u todoapp-dapr` the sidecar's.

## Pinned versions

`playbooks/group_vars/all.yml` pins SQL Server and its tools, Docker, the ASP.NET Core runtime, `daprd`, and the
signing keys of their repositories by checksum. `pyproject.toml`, `uv.lock` and `.python-version` pin Python and the
Python packages.
The few packages that come from Ubuntu's own archive, such as `unzip` and `libicu74`, follow Ubuntu 24.04's updates.

## Checks

The root `make check` formats and lints the Python with ruff, runs its tests with pytest, and lints and syntax-checks
the playbooks with ansible-lint and `ansible-playbook --syntax-check`.
