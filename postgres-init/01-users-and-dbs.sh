#!/bin/bash
set -e

# Create keycloak user if not exists
psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
    DO \$\$
    BEGIN
      IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '$KC_DB_USER') THEN
        CREATE ROLE "$KC_DB_USER" LOGIN PASSWORD '$KC_DB_PASS';
      END IF;
    END \$\$;
EOSQL

# Create keycloak database if not exists
if ! psql -U "$POSTGRES_USER" -lqt | cut -d \| -f 1 | grep -qw "$KC_DB_NAME"; then
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -c "CREATE DATABASE \"$KC_DB_NAME\" OWNER \"$KC_DB_USER\";"
    echo "Created database $KC_DB_NAME"
else
    echo "Database $KC_DB_NAME already exists"
fi

# Create app user and database (if variables are set)
if [ -n "$APP_DB_USER" ]; then
    psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" <<-EOSQL
        DO \$\$
        BEGIN
          IF NOT EXISTS (SELECT FROM pg_roles WHERE rolname = '$APP_DB_USER') THEN
            CREATE ROLE "$APP_DB_USER" LOGIN PASSWORD '$APP_DB_PASS';
          END IF;
        END \$\$;
EOSQL

    if ! psql -U "$POSTGRES_USER" -lqt | cut -d \| -f 1 | grep -qw "$APP_DB_NAME"; then
        psql -v ON_ERROR_STOP=1 --username "$POSTGRES_USER" --dbname "$POSTGRES_DB" -c "CREATE DATABASE \"$APP_DB_NAME\" OWNER \"$APP_DB_USER\";"
        echo "Created database $APP_DB_NAME"
    else
        echo "Database $APP_DB_NAME already exists"
    fi
fi

echo "Database initialization complete!"
