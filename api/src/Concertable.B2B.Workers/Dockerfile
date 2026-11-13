FROM mcr.microsoft.com/dotnet/aspnet:10.0.15-noble@sha256:bfd912c83295ce66d4b706ebb8541b9f14259b0e76fbb88b2751f58ce2399d2e AS patched-runtime

FROM mcr.microsoft.com/azure-functions/dotnet-isolated:4-dotnet-isolated10.0@sha256:6990da543e1dc1c7bb8d8d32c702c87a525db7b8ea621e645b479c822ed72c6d

# The Functions base still ships .NET 10.0.12. Replace that runtime while retaining its host and other runtime families.
RUN rm -rf /usr/share/dotnet/shared/Microsoft.NETCore.App/10.0.* \
    /usr/share/dotnet/shared/Microsoft.AspNetCore.App/10.0.* \
    /usr/share/dotnet/host/fxr/10.0.*
COPY --from=patched-runtime /usr/share/dotnet/ /usr/share/dotnet/
RUN test -d /usr/share/dotnet/shared/Microsoft.NETCore.App/10.0.15 \
    && test -d /usr/share/dotnet/shared/Microsoft.AspNetCore.App/10.0.15 \
    && dotnet --list-runtimes

RUN apt-get update \
    && apt-get install -y --no-install-recommends --only-upgrade libssl3t64 openssl linux-libc-dev \
    && dpkg --compare-versions "$(dpkg-query -W -f='${Version}' linux-libc-dev)" ge 6.8.0-149.149 \
    && rm -rf /var/lib/apt/lists/*

ENV AzureWebJobsScriptRoot=/home/site/wwwroot \
    AzureFunctionsJobHost__Logging__Console__IsEnabled=true

WORKDIR /home/site/wwwroot

COPY . /home/site/wwwroot
