# DATABASE INSPECT (provider: SqlServer)

This command is intended to be used as a quick-and-easy way to inspect the inferred database schema based on the current database state. It is not intended to be a full-blown documentation generator; therefore, the output is limited to basic markdown tables that show the column names, data types, nullability, and primary key status for the specified tables. The markdown output can be copied and pasted into any markdown viewer or editor for further formatting or documentation purposes.

**Note**: The following is based on querying the database system tables/views; it may not be 100% accurate. Always refer to the actual database for the source of truth.

## TEST.UNKNOWN - Exists: No

## TEST.GENDER - Exists: Yes

- Schema: Test
- Name: Gender
- Qualified Name: [Test].[Gender]
- Table or View: Table
- Reference Data: Yes

### Columns

| Column    | Type           | Null | Default | PK  | Identity | Computed | Unique |
|-----------|----------------|------|---------|-----|----------|----------|--------|
| GenderId  | INT            | No   |         | Yes | Yes      | No       | No     |
| Code      | NVARCHAR(50)   | No   |         | No  | No       | No       | Yes    |
| Text      | VARCHAR(256)   | No   |         | No  | No       | No       | No     |
| CreatedBy | NVARCHAR(250)  | Yes  |         | No  | No       | No       | No     |
| CreatedOn | DATETIMEOFFSET | Yes  |         | No  | No       | No       | No     |
| UpdatedBy | NVARCHAR(250)  | Yes  |         | No  | No       | No       | No     |
| UpdatedOn | DATETIMEOFFSET | Yes  |         | No  | No       | No       | No     |

## TEST.CONTACT - Exists: Yes

- Schema: Test
- Name: Contact
- Qualified Name: [Test].[Contact]
- Table or View: Table
- Reference Data: No

### Columns

| Column          | Type          | Null | Default | PK  | Identity | Computed | Unique |
|-----------------|---------------|------|---------|-----|----------|----------|--------|
| ContactId       | INT           | No   |         | Yes | No       | No       | No     |
| Name            | NVARCHAR(200) | No   |         | No  | No       | No       | No     |
| Phone           | VARCHAR(15)   | Yes  |         | No  | No       | No       | No     |
| DateOfBirth     | DATE          | Yes  |         | No  | No       | No       | No     |
| ContactTypeId   | INT           | No   | ((1))   | No  | No       | No       | No     |
| GenderId        | INT           | Yes  |         | No  | No       | No       | No     |
| TenantId        | NVARCHAR(50)  | Yes  |         | No  | No       | No       | No     |
| Notes           | NVARCHAR(MAX) | Yes  |         | No  | No       | No       | No     |
| ContactTypeCode | NVARCHAR(50)  | Yes  |         | No  | No       | No       | No     |

