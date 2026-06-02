# DATABASE INSPECT (provider: MySQL)

This command is intended to be used as a quick-and-easy way to inspect the inferred database schema based on the current database state. It is not intended to be a full-blown documentation generator; therefore, the output is limited to basic markdown tables that show the column names, data types, nullability, and primary key status for the specified tables. The markdown output can be copied and pasted into any markdown viewer or editor for further formatting or documentation purposes.

**Note**: The following is based on querying the database system tables/views; it may not be 100% accurate. Always refer to the actual database for the source of truth.

## UNKNOWN - Exists: No

## GENDER - Exists: Yes

- Schema: 
- Name: gender
- Qualified Name: `gender`
- Table or View: Table
- Reference Data: Yes

### Columns

| Column     | Type         | Null | Default | PK  | Identity | Computed | Unique |
|------------|--------------|------|---------|-----|----------|----------|--------|
| gender_id  | INT          | No   |         | Yes | Yes      | No       | No     |
| code       | VARCHAR(50)  | No   |         | No  | No       | No       | Yes    |
| text       | VARCHAR(256) | No   |         | No  | No       | No       | No     |
| created_by | VARCHAR(50)  | Yes  |         | No  | No       | No       | No     |
| created_on | DATETIME     | Yes  |         | No  | No       | No       | No     |
| updated_by | VARCHAR(50)  | Yes  |         | No  | No       | No       | No     |
| updated_on | DATETIME     | Yes  |         | No  | No       | No       | No     |

## CONTACT - Exists: Yes

- Schema: 
- Name: contact
- Qualified Name: `contact`
- Table or View: Table
- Reference Data: No

### Columns

| Column            | Type         | Null | Default | PK  | Identity | Computed | Unique |
|-------------------|--------------|------|---------|-----|----------|----------|--------|
| contact_id        | INT          | No   |         | Yes | Yes      | No       | No     |
| name              | VARCHAR(200) | No   |         | No  | No       | No       | No     |
| phone             | VARCHAR(15)  | Yes  |         | No  | No       | No       | No     |
| date_of_birth     | DATE         | Yes  |         | No  | No       | No       | No     |
| contact_type_id   | INT          | No   | 1       | No  | No       | No       | No     |
| gender_id         | INT          | Yes  |         | No  | No       | No       | No     |
| notes             | TEXT         | Yes  |         | No  | No       | No       | No     |
| created_by        | VARCHAR(50)  | Yes  |         | No  | No       | No       | No     |
| created_on        | DATETIME     | Yes  |         | No  | No       | No       | No     |
| updated_by        | VARCHAR(50)  | Yes  |         | No  | No       | No       | No     |
| updated_on        | DATETIME     | Yes  |         | No  | No       | No       | No     |
| contact_type_code | VARCHAR(50)  | Yes  |         | No  | No       | No       | No     |

