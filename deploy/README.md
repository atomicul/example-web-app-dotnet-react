# Deploy

Sets up the todo app and its database on a single Ubuntu host with Ansible. uv is the only requirement: it installs
the pinned Python, Ansible and every other dependency from `uv.lock`.

## Requirements

- On this machine: uv. The scripts deploy the releases of the GitHub repository they come from: the one uvx installed
  them from (see [From another repository](#from-another-repository)), or else the `origin` remote of the clone they
  run in. A fork thus deploys its own GitHub releases and `ghcr.io` images, which must be public.
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

### From another repository

The package carries its playbooks, so another repository, such as one holding your infrastructure, runs the scripts
with uvx and no clone of this one. Install them from the tag of the release they deploy, so the playbooks match it:

```sh
uvx --from "git+https://github.com/OWNER/REPO@v1.2.3#subdirectory=deploy" setup-webapp ubuntu@203.0.113.10 v1.2.3
```

`setup-webapp` saves a generated PFX in the directory it runs from: add `*.pfx` to that repository's `.gitignore`.

### `setup-database`

Installs SQL Server and Docker, applies the release's migrations with the `migrate` command of its Docker image, and
creates the `todoapp` login the app connects with, which can read and write the app's tables but not change them.

The first run generates two passwords:

- The `todoapp` login's password, which it **shows once** at the end of the run, in the connection string an app on
  the same host connects with, and stores nowhere. `setup-webapp` asks for that connection string.
- The `sa` password, kept for later migrations in `/etc/todoapp/database-admin.env`, which only root can read.

SQL Server listens on `127.0.0.1` only. It runs as the Express edition, which is free in production; set
`mssql_edition` in `src/todoapp_deploy/playbooks/group_vars/all.yml` before the first run to use a licensed one.

### `setup-webapp`

Installs the ASP.NET Core runtime and the release's zip, and runs the app as the `todoapp` systemd service on port
`80`, then waits for `/health` to answer `Healthy`. Beside it, it runs the app's Dapr sidecar as the `todoapp-dapr`
service (see [Dapr sidecar](#dapr-sidecar)). It serves plain HTTP until `setup-tls` gives the host a
certificate, and from then on HTTPS on port `443` as well. To terminate HTTPS in a reverse proxy instead, skip
`setup-tls` and set the API's `TrustedProxies` (see the [root README](../README.md#rate-limiting)).

It only asks for what the host doesn't have yet:

- The connection string the app connects to its database with: the one `setup-database` showed when it set the
  database up on the same host, or one to any other SQL Server.
- The PFX certificate that encrypts the app's Data Protection keys: press Enter to generate one, saved locally as its
  backup, or type the path of an existing PFX. Either way, it then asks for the PFX's password. For a new PFX, press
  Enter to generate a password, which it **shows once**.

To change either of them on a host that has them, pass `--change-connection-string` or `--change-certificate`.
Replacing the certificate logs every user out, and losing it does too: keep the PFX and its password.

To answer the certificate's prompts ahead of time, pass the PFX's path as `--certificate PFX` and export its password
as `TODOAPP_CERTIFICATE_PASSWORD`, e.g. for a PFX made with [`generate-certificate`](#generate-certificate). Secrets
only ever come from environment variables, never from arguments. On a host that has a certificate, they are used only
with `--change-certificate`; without it, the run fails if `--certificate` names a different PFX than the host's, so
the certificate is never replaced by accident. Passing the same PFX and password again changes nothing on the host,
even with `--change-certificate`.

`--dapr-components DIR` replaces the sidecar's components on the host with the `*.yaml` and `*.yml` files of a local
directory, and removes the ones it doesn't have. Without it, the host keeps the components it has.

`--settings FILE` replaces the deployment's app settings on the host with a JSON file in the format of the API's
`appsettings.json`, e.g. its proxies and rate limits:

```json
{
  "TrustedProxies": { "Networks": ["10.0.0.0/8"] },
  "RateLimits": { "Api": { "PermitLimit": 500 } }
}
```

Without it, the host keeps the settings it has. The file only overrides this deployment's values: a setting every
environment needs belongs, with its default, in `backend/TodoApi/appsettings.json`, which each release ships. Its
entries go under the ones the deploy sets itself (Kestrel's endpoints, Data Protection, ACME and Dapr), which
win, and it holds no secrets: those stay in the `.env` files below.

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
to it, are yours to decide and to keep, e.g. in a private directory next to the PFX backup. A feature that uses one
needs both its component, which `--dapr-components` installs, and the setting that names the component, which turns
the feature on and which `--settings` passes:

| Component | Setting | Turns on |
|---|---|---|
| An output binding, e.g. named `emails` | `Email:Queue`, set to the binding's name | Queueing emails on it, in the format the [root README](../README.md#email) describes; without the setting, the app logs them |

Setting a provider up is manual. For AWS, for example:

1. Create the SQS queue and whatever consumes it, e.g. a Lambda that sends each message with SES.
2. Let the host send to the queue, preferably with an instance profile: the sidecar then needs no keys.
3. Write `emails.yaml` as an [SQS binding](https://docs.dapr.io/reference/components-reference/supported-bindings/sqs/)
   named `emails`. Any secret it needs can come from a secret store component, such as
   [SSM Parameter Store](https://docs.dapr.io/reference/components-reference/supported-secret-stores/aws-parameter-store/),
   in the same directory.
4. Set `Email:Queue` to the binding's name in your settings file, e.g. `{ "Email": { "Queue": "emails" } }`.
5. Run `uv run setup-webapp user@host v1.2.3 --dapr-components path/to/components --settings path/to/appsettings.json`.

`journalctl -u todoapp-dapr` shows whether the sidecar loaded the components.

### `generate-certificate`

`uv run generate-certificate dataprotection.pfx` writes a self-signed certificate that `setup-webapp` accepts, with the
password in `TODOAPP_CERTIFICATE_PASSWORD`, or else one it asks for. It refuses to overwrite a file.

## On the host

| Path | Holds |
|---|---|
| `/etc/todoapp/appsettings.Production.json` | The app's settings other than secrets: the deployment's, with `todoapp_settings` from `src/todoapp_deploy/playbooks/group_vars/all.yml` on top; each release links to it |
| `/etc/todoapp/deployment-settings.json` | The deployment's settings, from `setup-webapp --settings` (root only) |
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
[root README](../README.md#environment-variables), set it in the file `setup-webapp --settings` takes.

`journalctl -u todoapp` shows the app's logs, and `journalctl -u todoapp-dapr` the sidecar's.

## Pinned versions

`src/todoapp_deploy/playbooks/group_vars/all.yml` pins SQL Server and its tools, Docker, the ASP.NET Core runtime,
`daprd`, and the signing keys of their repositories by checksum. `pyproject.toml`, `uv.lock` and `.python-version` pin
Python and the Python packages.
The few packages that come from Ubuntu's own archive, such as `unzip` and `libicu74`, follow Ubuntu 24.04's updates.

## Checks

The root `make check` formats and lints the Python with ruff, runs its tests with pytest, and lints and syntax-checks
the playbooks with ansible-lint and `ansible-playbook --syntax-check`.
