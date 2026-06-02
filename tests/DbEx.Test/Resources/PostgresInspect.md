# DATABASE INSPECT (provider: Postgres)

This command is intended to be used as a quick-and-easy way to inspect the inferred database schema based on the current database state. It is not intended to be a full-blown documentation generator; therefore, the output is limited to basic markdown tables that show the column names, data types, nullability, and primary key status for the specified tables. The markdown output can be copied and pasted into any markdown viewer or editor for further formatting or documentation purposes.

**Note**: This following is based on querying the database system tables/views; it may not be 100% accurate. Always refer to the actual database for the source of truth.

## PUBLIC.UNKNOWN - Exists: No

## PUBLIC.GENDER - Exists: Yes

- Schema: public
- Name: gender
- Qualified Name: "public"."gender"
- Table or View: Table
- Reference Data: Yes

### Columns

| Column     | Type                     | Null | Default | PK  | Identity | Computed | Unique |
|------------|--------------------------|------|---------|-----|----------|----------|--------|
| gender_id  | INTEGER                  | No   |         | Yes | Yes      | No       | No     |
| code       | CHARACTER VARYING(50)    | No   |         | No  | No       | No       | Yes    |
| text       | CHARACTER VARYING(256)   | No   |         | No  | No       | No       | No     |
| created_by | CHARACTER VARYING(50)    | Yes  |         | No  | No       | No       | No     |
| created_on | TIMESTAMP WITH TIME ZONE | Yes  |         | No  | No       | No       | No     |
| updated_by | CHARACTER VARYING(50)    | Yes  |         | No  | No       | No       | No     |
| updated_on | TIMESTAMP WITH TIME ZONE | Yes  |         | No  | No       | No       | No     |
| xmin       | XID                      | No   |         | No  | No       | Yes      | No     |

## PUBLIC.CONTACT - Exists: Yes

- Schema: public
- Name: contact
- Qualified Name: "public"."contact"
- Table or View: Table
- Reference Data: No

### Columns

| Column            | Type                     | Null | Default | PK  | Identity | Computed | Unique |
|-------------------|--------------------------|------|---------|-----|----------|----------|--------|
| contact_id        | INTEGER                  | No   |         | Yes | Yes      | No       | No     |
| name              | CHARACTER VARYING(200)   | No   |         | No  | No       | No       | No     |
| phone             | CHARACTER VARYING(15)    | Yes  |         | No  | No       | No       | No     |
| date_of_birth     | DATE                     | Yes  |         | No  | No       | No       | No     |
| contact_type_id   | INTEGER                  | No   | 1       | No  | No       | No       | No     |
| gender_id         | INTEGER                  | Yes  |         | No  | No       | No       | No     |
| notes             | TEXT                     | Yes  |         | No  | No       | No       | No     |
| created_by        | CHARACTER VARYING(50)    | Yes  |         | No  | No       | No       | No     |
| created_on        | TIMESTAMP WITH TIME ZONE | Yes  |         | No  | No       | No       | No     |
| updated_by        | CHARACTER VARYING(50)    | Yes  |         | No  | No       | No       | No     |
| updated_on        | TIMESTAMP WITH TIME ZONE | Yes  |         | No  | No       | No       | No     |
| contact_type_code | CHARACTER VARYING(50)    | Yes  |         | No  | No       | No       | No     |
| xmin              | XID                      | No   |         | No  | No       | Yes      | No     |

