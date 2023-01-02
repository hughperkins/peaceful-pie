ARG REPO=mcr.microsoft.com/dotnet/aspnet
FROM $REPO:7.0.1-jammy-amd64

ENV \
    # Unset ASPNETCORE_URLS from aspnet base image
    ASPNETCORE_URLS= \
    # Do not generate certificate
    DOTNET_GENERATE_ASPNET_CERTIFICATE=false \
    # Do not show first run text
    DOTNET_NOLOGO=true \
    # SDK version
    DOTNET_SDK_VERSION=7.0.101 \
    # Enable correct mode for dotnet watch (only mode supported in a container)
    DOTNET_USE_POLLING_FILE_WATCHER=true \
    # Skip extraction of XML docs - generally not useful within an image/container - helps performance
    NUGET_XMLDOC_MODE=skip \
    # PowerShell telemetry for docker image usage
    POWERSHELL_DISTRIBUTION_CHANNEL=PSDocker-DotnetSDK-Ubuntu-22.04

RUN apt-get update \
    && apt-get install -y --no-install-recommends \
        curl \
        git \
        wget \
        gnupg \
        vim \
        libgtk-3-0 \
        libarchive-dev \
    && rm -rf /var/lib/apt/lists/*

# Install .NET SDK
RUN curl -fSL --output dotnet.tar.gz https://dotnetcli.azureedge.net/dotnet/Sdk/$DOTNET_SDK_VERSION/dotnet-sdk-$DOTNET_SDK_VERSION-linux-x64.tar.gz \
    && dotnet_sha512='cf289ad0e661c38dcda7f415b3078a224e8347528448429d62c0f354ee951f4e7bef9cceaf3db02fb52b5dd7be987b7a4327ca33fb9239b667dc1c41c678095c' \
    && echo "$dotnet_sha512  dotnet.tar.gz" | sha512sum -c - \
    && mkdir -p /usr/share/dotnet \
    && tar -oxzf dotnet.tar.gz -C /usr/share/dotnet ./packs ./sdk ./sdk-manifests ./templates ./LICENSE.txt ./ThirdPartyNotices.txt \
    && rm dotnet.tar.gz \
    # Trigger first run experience by running arbitrary cmd
    && dotnet help

RUN curl -fSL --output UnitySetup-2021.3.16f1 'https://download.unity3d.com/download_unity/4016570cf34f/UnitySetup-2021.3.16f1?_ga=2.256438923.1493529091.1672006725-1442771051.1669639379' \
    && chmod +x UnitySetup-2021.3.16f1 \
    && echo y |./UnitySetup-2021.3.16f1 --unattended --install-location /opt/Unity > /dev/null
