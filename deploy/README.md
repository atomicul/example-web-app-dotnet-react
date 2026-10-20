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

The first run sets two passwords, taken from environment variables when they are set and generated otherwise:

- The `todoapp` login's password, from `TODOAPP_DATABASE_PASSWORD`. It stores it nowhere. A generated one it **shows
  once** at the end of the run, in the connection string an app on the same host connects with, as a line ready for
  the file `setup-webapp --env-file` takes.
- The `sa` password, from `TODOAPP_SA_PASSWORD`, kept for later migrations in `/etc/todoapp/database-admin.env`, which
  only root can read.

A password of your own must be at least 8 letters and digits, with a lowercase letter, an uppercase letter and a
digit, so that SQL Server accepts it and a connection string needs no quoting. Later runs ignore both variables: the
login and SQL Server keep the passwords they were created with.

SQL Server listens on `127.0.0.1` only. It runs as the Express edition, which is free in production; set
`mssql_edition` in `src/todoapp_deploy/playbooks/group_vars/all.yml` before the first run to use a licensed one.

### `setup-webapp`

Installs the ASP.NET Core runtime and the release's zip, and runs the app as the `todoapp` systemd service on port
`80`, then waits for `/health` to answer `Healthy`. Beside it, it runs the app's Dapr sidecar as the `todoapp-dapr`
service (see [Dapr sidecar](#dapr-sidecar)). It serves plain HTTP until `setup-tls` gives the host a
certificate, and from then on HTTPS on port `443` as well. To terminate HTTPS in a reverse proxy instead, skip
`setup-tls` and set the API's `TrustedProxies` (see the [root README](../README.md#rate-limiting)).

`--env-file FILE` replaces the deployment's environment variables on the host with a file in systemd's
[`EnvironmentFile`](https://www.freedesktop.org/software/systemd/man/latest/systemd.exec.html#EnvironmentFile=)
format, which holds the app's secrets, such as its connection string: the line `setup-database` showed when it set the
database up on the same host, or one to any other SQL Server.

```sh
ConnectionStrings__Default="Server=127.0.0.1,1433;Database=TodoApp;User ID=todoapp;Password=...;TrustServerCertificate=True"
```

Without it, the host keeps the variables it has. Only root can read them on the host; systemd passes them to the app.
They override the app's settings, including the ones the deploy sets itself (see `--settings` below), so set none of
those, nor `ASPNETCORE_ENVIRONMENT` or `DOTNET_ROOT`. Without a connection string, the app answers `/health` with
`503`, and `journalctl -u todoapp` shows why.

On a host without a Data Protection certificate, it asks for the PFX certificate that encrypts the keys protecting
sessions: type the path of a PFX, e.g. one made with [`generate-certificate`](#generate-certificate), then its
password. Press Enter instead to store the keys unencrypted; later runs ask again until the host has a certificate.

`--certificate PFX` gives the certificate without the prompt, with its password from `TODOAPP_CERTIFICATE_PASSWORD`, or
else asked for. Secrets only ever come from environment variables, never from arguments. The password is checked
against the PFX before anything on the host changes. Passing the host's PFX again changes nothing; passing another one
replaces it, which logs every user out, as losing it does: keep the PFX and its password.

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
win, and it holds no secrets: those go in the file `--env-file` takes.

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
password in `TODOAPP_CERTIFICATE_PASSWORD`, or else one it asks for. Press Enter at that prompt to generate a password,
which it **shows once**. It refuses to overwrite a file.
`uv run check-certificate dataprotection.pfx` checks that a PFX opens with its password, taken the same way.

## On the host

| Path | Holds | Created by | Required by |
|---|---|---|---|
| `/etc/todoapp/appsettings.Production.json` | The app's settings other than secrets: the deployment's, with `todoapp_settings` from `src/todoapp_deploy/playbooks/group_vars/all.yml` on top; each release links to it | `setup-webapp`, rewritten by `setup-tls` | The app |
| `/etc/todoapp/deployment-settings.json` | The deployment's settings (root only) | `setup-webapp --settings` | `setup-webapp` and `setup-tls`, when they write the app's settings; optional |
| `/etc/todoapp/database-admin.env` | The `sa` password and connection string, for migrations (root only) | `setup-database`, on its first run | `setup-database`, on later runs |
| `/etc/todoapp/deployment.env` | The deployment's environment variables, such as the app's connection string (root only; systemd passes them to the app) | `setup-webapp --env-file` | The app, which answers `/health` with `503` without a connection string |
| `/etc/todoapp/certificate.env`, `dataprotection.pfx` | The certificate's password (root only) and the PFX (readable by the app) | `setup-webapp`, when given a PFX | The app, to encrypt its keys; optional |
| `/etc/todoapp/tls/fullchain.pem`, `privkey.pem` | The HTTPS certificate and its key (readable by the app) | `setup-tls`, through `/usr/local/sbin/todoapp-install-certificate`, which copies each renewal from `/etc/letsencrypt` | The app, which serves HTTPS when `setup-webapp` or `setup-tls` find them; optional |
| `/etc/todoapp/dapr/components` | The Dapr sidecar's components (only the sidecar's user can read them) | `setup-webapp --dapr-components` | The sidecar; optional |
| `/etc/systemd/system/todoapp.service`, `todoapp-dapr.service` | The app's and the sidecar's services | `setup-webapp` | `setup-tls`, which fails without the app's |
| `/run/todoapp-dapr` | The sidecar's sockets, which only the app's group can open | `setup-webapp`, and systemd on each boot | The app, to reach the sidecar |
| `/var/lib/todoapp/acme` | The web root certbot writes ACME challenges to, which the app serves | `setup-tls` | certbot, to get and renew the certificate |
| `/var/opt/mssql` | SQL Server's data | `setup-database` | The app, when its connection string points at this host |
| `/opt/todoapp/releases/<tag>` | Each installed release; `/opt/todoapp/current` links to the one running | `setup-webapp` | The app's service |
| `/opt/dotnet/<version>` | The ASP.NET Core runtime | `setup-webapp` | The app's service |
| `/opt/dapr/<version>` | The Dapr sidecar, `daprd` | `setup-webapp` | The sidecar's service |

Secrets stay in the `.env` files, which systemd passes to the app as environment variables, overriding the JSON file.
To change another setting, such as `TrustedProxies` or `RateLimits` from the
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
