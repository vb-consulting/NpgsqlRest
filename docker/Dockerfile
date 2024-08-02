FROM ubuntu:22.04 AS builder
WORKDIR /app
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget ca-certificates && \
    wget https://github.com/vb-consulting/NpgsqlRest/releases/download/v2.9.0-client-v1.5.0/npgsqlrest-linux64 -O npgsqlrest && \
    wget https://github.com/vb-consulting/NpgsqlRest/releases/download/v2.9.0-client-v1.5.0/appsettings.json -O appsettings.json && \
    chmod +x npgsqlrest

FROM ubuntu:22.04
WORKDIR /app
COPY --from=builder /app/npgsqlrest /usr/local/bin/npgsqlrest
COPY --from=builder /app/appsettings.json /app/appsettings.json

RUN apt-get update && \
    apt-get install -y --no-install-recommends libssl3 && \
    rm -rf /var/lib/apt/lists/* && \
    apt-get clean autoclean && \
    apt-get autoremove --yes && \
    rm -rf /var/lib/{apt,dpkg,cache,log}/

ENV PATH="/usr/local/bin:${PATH}"

ENTRYPOINT ["npgsqlrest"]