FROM ubuntu:22.04 AS builder
WORKDIR /app
RUN apt-get update && \
    apt-get install -y --no-install-recommends wget ca-certificates && \
    wget https://github.com/vb-consulting/NpgsqlRest/releases/download/v2.8.5-client-v1.3.0/npgsqlrest-linux64 -O npgsqlrest && \
    chmod +x npgsqlrest

FROM ubuntu:22.04
WORKDIR /app
COPY --from=builder /app/npgsqlrest /usr/local/bin/npgsqlrest

RUN apt-get update && \
    apt-get install -y --no-install-recommends libssl3 && \
    rm -rf /var/lib/apt/lists/* && \
    apt-get clean autoclean && \
    apt-get autoremove --yes && \
    rm -rf /var/lib/{apt,dpkg,cache,log}/

ENTRYPOINT ["/usr/local/bin/npgsqlrest"]