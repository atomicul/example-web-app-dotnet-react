# syntax=docker/dockerfile:1

FROM node:24-slim AS frontend
WORKDIR /src
RUN corepack enable
COPY frontend/package.json frontend/pnpm-lock.yaml ./
RUN pnpm install --frozen-lockfile
COPY frontend/ ./
RUN pnpm build

FROM mcr.microsoft.com/dotnet/sdk:10.0.402 AS backend
ARG VERSION
WORKDIR /src
COPY global.json dotnet-tools.json ./
COPY backend/Directory.Build.props backend/Directory.Packages.props backend/
COPY backend/TodoApi/TodoApi.csproj backend/TodoApi/
RUN dotnet tool restore && dotnet restore backend/TodoApi
COPY backend/TodoApi/ backend/TodoApi/
RUN dotnet publish backend/TodoApi -c Release -o /app --no-restore -p:OpenApiGenerateDocuments=false ${VERSION:+-p:Version=$VERSION}
RUN dotnet ef migrations bundle --project backend/TodoApi --configuration Release --no-build --output /migrate

FROM mcr.microsoft.com/dotnet/aspnet:10.0.13
ENV ASPNETCORE_ENVIRONMENT=Production
WORKDIR /app
COPY --from=backend /app ./
COPY --from=backend /migrate /usr/local/bin/migrate
COPY --from=frontend /src/dist wwwroot/
USER $APP_UID
EXPOSE 8080
CMD ["dotnet", "TodoApi.dll"]
