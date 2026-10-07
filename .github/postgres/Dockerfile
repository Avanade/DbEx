# PostgreSQL test image with the PostGIS and pgvector extensions (used to test the extension data types).
FROM postgres:17
RUN apt-get update \
    && apt-get install -y --no-install-recommends postgresql-17-postgis-3 postgresql-17-pgvector \
    && rm -rf /var/lib/apt/lists/*