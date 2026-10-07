# DATABASE INSPECT (provider: Postgres)

This command is intended to be used as a quick-and-easy way to inspect the inferred database schema based on the current database state. It is not intended to be a full-blown documentation generator; therefore, the output is limited to basic markdown tables that show the column names, data types, nullability, and primary key status for the specified tables. The markdown output can be copied and pasted into any markdown viewer or editor for further formatting or documentation purposes.

**Note**: The following is based on querying the database system tables/views; it may not be 100% accurate. Always refer to the actual database for the source of truth.

## PUBLIC.UNKNOWN - Exists: No

## PUBLIC.GENDER - Exists: Yes

- Schema: public
- Name: gender
- Qualified Name: "public"."gender"
- Table or View: Table
- Reference Data: Yes

### Columns

| Column     | Type                     | Null | Default | PK  | Identity | Computed | Unique | JSON |
|------------|--------------------------|------|---------|-----|----------|----------|--------|------|
| gender_id  | INTEGER                  | No   |         | Yes | Yes      | No       | No     | No   |
| code       | CHARACTER VARYING(50)    | No   |         | No  | No       | No       | Yes    | No   |
| text       | CHARACTER VARYING(256)   | No   |         | No  | No       | No       | No     | No   |
| created_by | CHARACTER VARYING(50)    | Yes  |         | No  | No       | No       | No     | No   |
| created_on | TIMESTAMP WITH TIME ZONE | Yes  |         | No  | No       | No       | No     | No   |
| updated_by | CHARACTER VARYING(50)    | Yes  |         | No  | No       | No       | No     | No   |
| updated_on | TIMESTAMP WITH TIME ZONE | Yes  |         | No  | No       | No       | No     | No   |
| xmin       | XID                      | No   |         | No  | No       | Yes      | No     | No   |

## PUBLIC.CONTACT - Exists: Yes

- Schema: public
- Name: contact
- Qualified Name: "public"."contact"
- Table or View: Table
- Reference Data: No

### Columns

| Column            | Type                     | Null | Default | PK  | Identity | Computed | Unique | JSON |
|-------------------|--------------------------|------|---------|-----|----------|----------|--------|------|
| contact_id        | INTEGER                  | No   |         | Yes | Yes      | No       | No     | No   |
| name              | CHARACTER VARYING(200)   | No   |         | No  | No       | No       | No     | No   |
| phone             | CHARACTER VARYING(15)    | Yes  |         | No  | No       | No       | No     | No   |
| date_of_birth     | DATE                     | Yes  |         | No  | No       | No       | No     | No   |
| contact_type_id   | INTEGER                  | No   | 1       | No  | No       | No       | No     | No   |
| gender_id         | INTEGER                  | Yes  |         | No  | No       | No       | No     | No   |
| notes             | TEXT                     | Yes  |         | No  | No       | No       | No     | No   |
| created_by        | CHARACTER VARYING(50)    | Yes  |         | No  | No       | No       | No     | No   |
| created_on        | TIMESTAMP WITH TIME ZONE | Yes  |         | No  | No       | No       | No     | No   |
| updated_by        | CHARACTER VARYING(50)    | Yes  |         | No  | No       | No       | No     | No   |
| updated_on        | TIMESTAMP WITH TIME ZONE | Yes  |         | No  | No       | No       | No     | No   |
| contact_type_code | CHARACTER VARYING(50)    | Yes  |         | No  | No       | No       | No     | No   |
| xmin              | XID                      | No   |         | No  | No       | Yes      | No     | No   |

## PUBLIC.EXTRA_TYPES - Exists: Yes

- Schema: public
- Name: extra_types
- Qualified Name: "public"."extra_types"
- Table or View: Table
- Reference Data: No

### Columns

| Column           | Type                 | Null | Default | PK  | Identity | Computed | Unique | JSON |
|------------------|----------------------|------|---------|-----|----------|----------|--------|------|
| extra_types_id   | INTEGER              | No   |         | Yes | No       | No       | No     | No   |
| ip_address       | INET                 | Yes  |         | No  | No       | No       | No     | No   |
| network          | CIDR                 | Yes  |         | No  | No       | No       | No     | No   |
| mac_address      | MACADDR              | Yes  |         | No  | No       | No       | No     | No   |
| mac_address8     | MACADDR8             | Yes  |         | No  | No       | No       | No     | No   |
| search_vector    | TSVECTOR             | Yes  |         | No  | No       | No       | No     | No   |
| search_query     | TSQUERY              | Yes  |         | No  | No       | No       | No     | No   |
| pt               | POINT                | Yes  |         | No  | No       | No       | No     | No   |
| ln               | LINE                 | Yes  |         | No  | No       | No       | No     | No   |
| seg              | LSEG                 | Yes  |         | No  | No       | No       | No     | No   |
| bx               | BOX                  | Yes  |         | No  | No       | No       | No     | No   |
| pth              | PATH                 | Yes  |         | No  | No       | No       | No     | No   |
| poly             | POLYGON              | Yes  |         | No  | No       | No       | No     | No   |
| circ             | CIRCLE               | Yes  |         | No  | No       | No       | No     | No   |
| bit_flag         | BIT(1)               | Yes  |         | No  | No       | No       | No     | No   |
| bit_mask         | BIT(8)               | Yes  |         | No  | No       | No       | No     | No   |
| bit_varying      | BIT VARYING(16)      | Yes  |         | No  | No       | No       | No     | No   |
| tags             | text[]               | Yes  |         | No  | No       | No       | No     | No   |
| numbers          | integer[]            | Yes  |         | No  | No       | No       | No     | No   |
| current_mood     | mood                 | Yes  |         | No  | No       | No       | No     | No   |
| moods            | mood[]               | Yes  |         | No  | No       | No       | No     | No   |
| int_range        | INT4RANGE            | Yes  |         | No  | No       | No       | No     | No   |
| ts_range         | TSTZRANGE            | Yes  |         | No  | No       | No       | No     | No   |
| date_range       | DATERANGE            | Yes  |         | No  | No       | No       | No     | No   |
| int_multirange   | INT4MULTIRANGE       | Yes  |         | No  | No       | No       | No     | No   |
| attributes       | hstore               | Yes  |         | No  | No       | No       | No     | No   |
| label            | ltree                | Yes  |         | No  | No       | No       | No     | No   |
| ci_text          | citext               | Yes  |         | No  | No       | No       | No     | No   |
| geom             | geometry(Point,4326) | Yes  |         | No  | No       | No       | No     | No   |
| geom_any         | geometry             | Yes  |         | No  | No       | No       | No     | No   |
| geog             | geography            | Yes  |         | No  | No       | No       | No     | No   |
| embedding        | vector(3)            | Yes  |         | No  | No       | No       | No     | No   |
| half_embedding   | halfvec(3)           | Yes  |         | No  | No       | No       | No     | No   |
| sparse_embedding | sparsevec(3)         | Yes  |         | No  | No       | No       | No     | No   |
| xmin             | XID                  | No   |         | No  | No       | Yes      | No     | No   |

